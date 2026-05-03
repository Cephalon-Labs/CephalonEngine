# Cephalon.EventSourcing.Neo4j

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.Neo4j` is the Neo4j event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.MongoDB` and `Cephalon.EventSourcing.Redis`. It delivers the `IEventStore` contract against Neo4j graph nodes instead of a document collection or stream, with optimistic concurrency enforced by a compound node key constraint on `(streamId, streamVersion)`.

## What it owns

- a Neo4j-backed implementation of `IEventStore` registered through `AddCephalonNeo4jEventSourcing()`
- the `Neo4jEventEntry` POCO representing the fields stored on each `:Event` node
- `Neo4jEventSourcingConfiguration` that holds the constraint name constant and the `CREATE CONSTRAINT` Cypher statement
- a node key constraint on `(streamId, streamVersion)` bootstrapped lazily on first use via `volatile bool _constraintCreated` — `IS NODE KEY` enforces both uniqueness and existence
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- a fallback concurrency guard via the node key constraint — if a concurrent writer commits the same version between the version read and the `CREATE`, Neo4j raises `Neo.ClientError.Schema.ConstraintValidationFailed` and the provider re-reads the actual version before rethrowing `EventStreamConcurrencyException`
- stream replay through `ReadStreamAsync` returning events ordered by `streamVersion` ascending
- `GetVersionAsync` returning `-1` for a stream that does not exist yet (via `coalesce(max(e.streamVersion), -1)`)
- `System.Text.Json` serialization for event payloads using the concrete event CLR type
- event type round-tripping through `AssemblyQualifiedName` — the type name is stored as written by the CLR and resolved back via `Type.GetType()` on read

## Main surfaces

- `Neo4jEventEntry.cs`
- `Neo4jEventStore.cs`
- `Neo4jEventSourcingConfiguration.cs`
- `Hosting/Neo4jEventSourcingServiceCollectionExtensions.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore` contract, the `IDomainEvent` marker, and `EventStreamConcurrencyException`. `Cephalon.EventSourcing.Neo4j` supplies the Neo4j graph-node implementation of that contract so event-sourced aggregates can keep the same `IEventStore` injection point while using a graph database as the backing store.

The slice is intentionally narrow: it proves append, read, and optimistic concurrency against Neo4j graph nodes and nothing more. Snapshot persistence, projection rebuild, archival, and background replay workers are honest later additions.

## Registration

```csharp
builder.Services.AddCephalonNeo4jEventSourcing(
    uri: "bolt://localhost:7687",
    username: "neo4j",
    password: "secret");
```

The `eventLabel` parameter defaults to `"Event"` and can be overridden:

```csharp
builder.Services.AddCephalonNeo4jEventSourcing(
    uri: "bolt://localhost:7687",
    username: "neo4j",
    password: "secret",
    eventLabel: "DomainEvent");
```

The method registers `IDriver` using `TryAdd` semantics — a host that already registered a shared `IDriver` keeps its own instance. `IEventStore` is registered as scoped.

## Event node schema (`:Event`)

| Property | Type | Notes |
|----------|------|-------|
| `streamId` | string | Logical aggregate / stream identifier |
| `streamVersion` | long | Per-stream monotonic version (1-based; stream starts at version 1) |
| `eventType` | string | `AssemblyQualifiedName` of the concrete event CLR type |
| `payload` | string | `System.Text.Json`-serialized event body using the concrete type |
| `occurredAtUtc` | string | ISO 8601 UTC representation of `IDomainEvent.OccurredAtUtc` |
| `appendedAtUtc` | string | ISO 8601 UTC wall-clock time of the `CREATE` call |

**Constraint**: `IS NODE KEY` on `(streamId, streamVersion)` — enforces both uniqueness and non-nullability of both properties. This is the primary concurrency guard and makes per-stream range queries efficient.

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` (via `coalesce(max(e.streamVersion), -1)`) |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `1` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before insert |
| Concurrent writer commits same version (race after version read) | `CREATE` raises `Neo.ClientError.Schema.ConstraintValidationFailed`; provider re-reads actual version and throws `EventStreamConcurrencyException` |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown — events must declare the version the provider will assign |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

**Known limitation**: the version read (`GetVersionAsync`) and the batch `CREATE` statements are not executed in a single atomic transaction by the current implementation. Two concurrent writers that both read the same `expectedVersion` will both proceed to issue `CREATE` nodes. The node key constraint enforces the conflict — the second writer's transaction is aborted by Neo4j with a constraint violation error, which the provider catches and rethrows as `EventStreamConcurrencyException`.

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` executes:

```cypher
MATCH (e:Event {streamId: $streamId})
WHERE e.streamVersion >= $fromVersion
RETURN e.streamId AS streamId, e.streamVersion AS streamVersion,
       e.eventType AS eventType, e.payload AS payload,
       e.occurredAtUtc AS occurredAtUtc, e.appendedAtUtc AS appendedAtUtc
ORDER BY e.streamVersion ASC
```

and returns an `IAsyncEnumerable<IDomainEvent>`, yielding events one by one as the result list is iterated. The CLR type is resolved from `eventType` via `Type.GetType(throwOnError: false)` — a missing type throws `InvalidOperationException` with a message that names the unresolvable type and the stream.

## Constraint bootstrap

`Neo4jEventStore` uses a `volatile bool _constraintCreated` flag checked before every operation. On first access it runs `Neo4jEventSourcingConfiguration.CreateConstraintCypher` via `CREATE CONSTRAINT ... IF NOT EXISTS`, which is idempotent and safe to call multiple times. Subsequent calls skip the bootstrap via the volatile read. This avoids startup cost if the store is never accessed in a given process lifetime.

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence (`ISnapshotStore` is not implemented)
- projection rebuild orchestration
- archival or retention management
- background stream replay workers
- Neo4j change-data-capture or streams subscription
- transport or event-bus integration
- multi-tenancy discriminator population
- relationship modeling between event nodes

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Neo4j](data-neo4j.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.EventSourcing.Redis](event-sourcing-redis.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
