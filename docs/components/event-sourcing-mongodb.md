# Cephalon.EventSourcing.MongoDB

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.MongoDB` is the MongoDB event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.EntityFramework`. It delivers the `IEventStore` contract against a MongoDB collection instead of a relational table, with optimistic concurrency enforced by a compound unique index on `(StreamId, StreamVersion)`.

The package follows the repository-managed `MongoDB.Driver` version and keeps `SharpCompress` as an explicit direct dependency so NuGet audit and downstream consumers resolve the same non-vulnerable compression stack used by release validation.

## What it owns

- a MongoDB-backed implementation of `IEventStore` registered through `AddCephalonMongoDbEventSourcing()`
- a sanitized `IEventStoreContributor` entry projected through `IEventStoreCatalog` and the `event-sourcing` runtime surface
- the `MongoDbEventEntry` document model for append-only event stream documents
- a provider-durable `ISnapshotStore` implementation backed by a latest-snapshot MongoDB collection named `{collectionName}_snapshots`
- `MongoDbEventSourcingConfiguration` that creates the compound unique index on `(StreamId, StreamVersion)` for events and `(StreamId, StateType)` for snapshots using lazy double-check semantics so indexes are created on first use, not at startup
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- a fallback concurrency guard via `InsertManyAsync` — if a concurrent writer commits the same version between the version read and the insert, MongoDB raises error code 11000 and the provider re-reads the actual version before rethrowing `EventStreamConcurrencyException`
- stream replay through `ReadStreamAsync` returning events ordered by `StreamVersion` ascending
- `GetVersionAsync` returning `-1` for a stream that does not exist yet
- event payload serialization through the shared Cephalon event-type registry
- event type round-tripping through stable registry names, with descriptor aliases for legacy `AssemblyQualifiedName` rows
- `snapshotLifecycle = provider-durable` and `snapshotStorage = {collectionName}_snapshots` metadata so the core `event-sourcing` runtime surface can report MongoDB as a provider-durable snapshot provider

## Main surfaces

- `MongoDbEventEntry.cs`
- `MongoDbEventStore.cs`
- `MongoDbEventSourcingConfiguration.cs`
- `Hosting/MongoDbEventSourcingServiceCollectionExtensions.cs`
- `Services/MongoDbEventStoreContributor.cs`
- `Services/MongoDbSnapshotStore.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore` contract, the `IDomainEvent` marker, `ISnapshotStore`, the on-demand replay worker, and `EventStreamConcurrencyException`. `Cephalon.EventSourcing.MongoDB` supplies the MongoDB document-store implementation of the event-store and snapshot-store contracts so event-sourced aggregates can keep the same injection points while swapping the backing store without changing application code.

The slice is intentionally narrow: it proves append, read, optimistic concurrency, and durable latest-snapshot persistence against MongoDB collections. Projection rebuild still runs through the shared core replay worker and registered `IProjection<IDomainEvent>` services; this provider does not own named projection orchestration, archival, retention, distributed replay, or hosted replay workers.

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

The method registers `IMongoClient`, `IMongoDatabase`, the typed `IMongoCollection<MongoDbEventEntry>`, the provider snapshot collection, the MongoDB-backed `ISnapshotStore`, and the shared event-type registry using `TryAdd` semantics for shared infrastructure — a host that already registered a shared `IMongoClient` keeps its own instance. The host still registers concrete event payloads through `AddCephalonEventType<TEvent>(...)` or `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`.

## Event stream collection schema (`event_streams`)

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `StreamId` | string | Logical aggregate / stream identifier |
| `StreamVersion` | long | Per-stream monotonic version (zero-based; stream starts at version 0) |
| `EventType` | string | Stable Cephalon event-type registry name |
| `Payload` | string | Serialized event body produced by the registered event-type descriptor |
| `OccurredAtUtc` | DateTime | `IDomainEvent.OccurredAtUtc` as stored by the domain event |
| `AppendedAtUtc` | DateTime | UTC wall-clock time of the `InsertManyAsync` call |
| `CorrelationId` | string? | Optional; not populated in this slice |
| `TenantId` | string? | Optional; not populated in this slice |

**Index**: compound unique index on `(StreamId ascending, StreamVersion ascending)`. This index is the primary concurrency guard and makes per-stream range queries efficient without a full collection scan.

## Snapshot collection schema (`event_streams_snapshots`)

The snapshot collection name is derived from the event collection name. With the default `collectionName = "event_streams"`, snapshots are stored in `event_streams_snapshots`; with `collectionName = "domain_events"`, snapshots are stored in `domain_events_snapshots`.

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `StreamId` | string | Logical aggregate / stream identifier |
| `StateType` | string | Stable snapshot state type key using `AssemblyName:FullTypeName` |
| `StreamVersion` | long | Latest stream version represented by the snapshot |
| `Payload` | string | Serialized state payload produced by `System.Text.Json` |
| `SavedAtUtc` | DateTime | UTC wall-clock time when the snapshot was persisted |

**Index**: compound unique index on `(StreamId ascending, StateType ascending)`. `SaveSnapshotAsync` updates an existing snapshot only when the new version is at least the stored version; older replacement attempts fail with `InvalidOperationException` so a stale replay cannot rewind durable state.

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `0` |
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

and sorts by `StreamVersion` ascending. It returns an `IAsyncEnumerable<IDomainEvent>`, yielding events one by one as the cursor advances. The event payload is resolved through `IEventTypeRegistry` by `EventType`; a missing descriptor throws `InvalidOperationException` with a message that names the unregistered type name and the stream. Descriptors include legacy `AssemblyQualifiedName` aliases by default so older documents can still be read after hosts register the concrete event type.

## Index laziness

`MongoDbEventStore` and `MongoDbSnapshotStore` use a `volatile bool _indexesCreated` flag checked before every operation. On first access they call the matching `MongoDbEventSourcingConfiguration` index helper. Subsequent calls skip the check via the volatile read. This avoids startup cost if the store is never accessed in a given process lifetime.

## Not shipped in this slice

This provider intentionally does not claim:

- projection rebuild orchestration
- archival or retention management
- distributed replay execution
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
