# Cephalon.EventSourcing.MongoDB

> **Maturity:** `M1` · **Ownership:** provider-managed · **Family:** `event-sourcing` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.EventSourcing.MongoDB` is the MongoDB event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.EntityFramework`. It delivers the `IEventStore` contract against a MongoDB collection instead of a relational table, with optimistic concurrency enforced by a compound unique index on `(StreamId, StreamVersion)`.

## What it owns

- a MongoDB-backed implementation of `IEventStore` registered through `AddCephalonMongoDbEventSourcing()`
- the `MongoDbEventEntry` document model for append-only event stream documents
- `MongoDbEventSourcingConfiguration` that creates the compound unique index on `(StreamId, StreamVersion)` using lazy double-check semantics so indexes are created on first use, not at startup
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- a fallback concurrency guard via `InsertManyAsync` — if a concurrent writer commits the same version between the version read and the insert, MongoDB raises error code 11000 and the provider re-reads the actual version before rethrowing `EventStreamConcurrencyException`
- stream replay through `ReadStreamAsync` returning events ordered by `StreamVersion` ascending
- `GetVersionAsync` returning `-1` for a stream that does not exist yet
- `System.Text.Json` serialization for event payloads using the concrete event CLR type
- event type round-tripping through `AssemblyQualifiedName` — the type name is stored as written by the CLR and resolved back via `Type.GetType()` on read

## Main surfaces

- `MongoDbEventEntry.cs`
- `MongoDbEventStore.cs`
- `MongoDbEventSourcingConfiguration.cs`
- `Hosting/MongoDbEventSourcingServiceCollectionExtensions.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore` contract, the `IDomainEvent` marker, and `EventStreamConcurrencyException`. `Cephalon.EventSourcing.MongoDB` supplies the MongoDB document-store implementation of that contract so event-sourced aggregates can keep the same `IEventStore` injection point while swapping the backing store without changing application code.

The slice is intentionally narrow: it proves append, read, and optimistic concurrency against a MongoDB collection and nothing more. Snapshot persistence, projection rebuild, archival, and background replay workers are honest later additions.

## Registration

```csharp
builder.Services.AddCephalonMongoDbEventSourcing(
    connectionString: "mongodb://localhost:27017",
    databaseName: "myapp");
```

The `collectionName` parameter defaults to `"event_streams"` and can be overridden:

```csharp
builder.Services.AddCephalonMongoDbEventSourcing(
    connectionString: connectionString,
    databaseName: "myapp",
    collectionName: "domain_events");
```

The method registers `IMongoClient`, `IMongoDatabase`, and the typed `IMongoCollection<MongoDbEventEntry>` using `TryAdd` semantics — a host that already registered a shared `IMongoClient` keeps its own instance.

## Event stream collection schema (`event_streams`)

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `StreamId` | string | Logical aggregate / stream identifier |
| `StreamVersion` | long | Per-stream monotonic version (1-based; stream starts at version 1) |
| `EventType` | string | `AssemblyQualifiedName` of the concrete event CLR type |
| `Payload` | string | `System.Text.Json`-serialized event body using the concrete type |
| `OccurredAtUtc` | DateTime | `IDomainEvent.OccurredAtUtc` as stored by the domain event |
| `AppendedAtUtc` | DateTime | UTC wall-clock time of the `InsertManyAsync` call |
| `CorrelationId` | string? | Optional; not populated in this slice |
| `TenantId` | string? | Optional; not populated in this slice |

**Index**: compound unique index on `(StreamId ascending, StreamVersion ascending)`. This index is the primary concurrency guard and makes per-stream range queries efficient without a full collection scan.

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `1` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before insert |
| Concurrent writer commits same version (race after version read) | `InsertManyAsync` raises error code 11000; provider re-reads actual version and throws `EventStreamConcurrencyException` |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown — events must declare the version the provider will assign |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` filters the collection with:

```
StreamId == streamId AND StreamVersion >= fromVersion
```

and sorts by `StreamVersion` ascending. It returns an `IAsyncEnumerable<IDomainEvent>`, yielding events one by one as the cursor advances. The CLR type is resolved from `EventType` via `Type.GetType(throwOnError: false)` — a missing type throws `InvalidOperationException` with a message that names the unresolvable type and the stream.

## Index laziness

`MongoDbEventStore` uses a `volatile bool _indexesCreated` flag checked before every operation. On first access it calls `MongoDbEventSourcingConfiguration.EnsureIndexesAsync()`, which calls `CreateOneAsync` with `CreateIndexOptions { Background = false }`. Subsequent calls skip the check via the volatile read. This avoids startup cost if the store is never accessed in a given process lifetime.

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence (`ISnapshotStore` is not implemented)
- projection rebuild orchestration
- archival or retention management
- background stream replay workers
- change-stream subscription support
- transport or event-bus integration
- multi-tenancy discriminator population (`TenantId` field is present but not filled)

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.EventSourcing.EntityFramework](event-sourcing-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
