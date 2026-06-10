# Cephalon.EventSourcing.Cassandra

> **Maturity:** `M1` · **Ownership:** provider-managed · **Family:** `event-sourcing` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.EventSourcing.Cassandra` is the Apache Cassandra event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.MongoDB`, `Cephalon.EventSourcing.Redis`, and `Cephalon.EventSourcing.Neo4j`. It delivers the `IEventStore` contract against a Cassandra wide-column table with a composite primary key on `(stream_id, stream_version)`, with optimistic concurrency detection via LWT `INSERT IF NOT EXISTS`.

## What it owns

- a Cassandra-backed implementation of `IEventStore` registered through `AddCephalonCassandraEventSourcing()`
- the `CassandraEventEntry` record model for persisted domain event rows
- `CassandraEventSourcingConfiguration` that holds the CQL DDL template for the event-streams table with composite primary key and clustering order
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- a fallback concurrency guard via LWT `INSERT IF NOT EXISTS` — if a concurrent writer commits the same version between the version read and the insert, Cassandra returns `[applied]=false` and the provider re-reads the actual version before rethrowing `EventStreamConcurrencyException`
- stream replay through `ReadStreamAsync` returning events ordered by `stream_version` ascending as an `IAsyncEnumerable<IDomainEvent>`
- `GetVersionAsync` returning `-1` for a stream that does not exist yet
- `System.Text.Json` serialization for event payloads using the concrete event CLR type
- event type round-tripping through `AssemblyQualifiedName` — stored as written by the CLR and resolved back via `Type.GetType()` on read
- lazy session initialization — `ICluster` is stored at construction time; `ISession` is opened on first operation using a `SemaphoreSlim(1,1)` guard

## Main surfaces

- `CassandraEventEntry.cs`
- `CassandraEventStore.cs`
- `CassandraEventSourcingConfiguration.cs`
- `Hosting/CassandraEventSourcingServiceCollectionExtensions.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore` contract, the `IDomainEvent` marker, and `EventStreamConcurrencyException`. `Cephalon.EventSourcing.Cassandra` supplies the Cassandra wide-column implementation of that contract so event-sourced aggregates can keep the same `IEventStore` injection point while swapping the backing store without changing application code.

## Registration

```csharp
builder.Services.AddCephalonCassandraEventSourcing(
    contactPoints: "localhost",
    keyspace: "myapp");
```

The `tableName` parameter defaults to `"cephalon_event_streams"` and can be overridden:

```csharp
builder.Services.AddCephalonCassandraEventSourcing(
    contactPoints: "localhost",
    keyspace: "myapp",
    tableName: "domain_events");
```

The method registers `ICluster` and `IEventStore` using `TryAdd` semantics — a host that already registered a shared `ICluster` keeps its own instance.

## Event-streams table schema

| Column | CQL type | Notes |
|--------|----------|-------|
| `stream_id` | `text` | Logical aggregate / stream identifier (partition key) |
| `stream_version` | `bigint` | Per-stream monotonic version (clustering key, ASC order) |
| `event_type` | `text` | `AssemblyQualifiedName` of the concrete event CLR type |
| `payload` | `text` | `System.Text.Json`-serialized event body using the concrete type |
| `occurred_at_utc` | `timestamp` | `IDomainEvent.OccurredAtUtc` as stored by the domain event |
| `appended_at_utc` | `timestamp` | UTC wall-clock time of the append operation |

**Primary key**: `PRIMARY KEY (stream_id, stream_version)` — `stream_id` is the partition key; `stream_version` is the clustering key with `CLUSTERING ORDER BY (stream_version ASC)`.

This key structure means:
- all events for a given `stream_id` are co-located in the same Cassandra partition
- per-stream range queries (`WHERE stream_id = ? AND stream_version >= ?`) are served from a single partition without a full table scan
- uniqueness on `(stream_id, stream_version)` is enforced at the Cassandra storage level, enabling LWT conflict detection

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `1` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before any insert |
| Concurrent writer commits same version (race after version read) | LWT `INSERT IF NOT EXISTS` returns `[applied]=false`; provider re-reads actual version and throws `EventStreamConcurrencyException` |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown — events must declare the version the provider will assign |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` executes:

```cql
SELECT stream_id, stream_version, event_type, payload, occurred_at_utc, appended_at_utc
FROM {tableName}
WHERE stream_id = ? AND stream_version >= ?
ORDER BY stream_version ASC
```

It returns an `IAsyncEnumerable<IDomainEvent>`, yielding events one by one as the `RowSet` is iterated. The CLR type is resolved from `event_type` via `Type.GetType(throwOnError: false)` — a missing type throws `InvalidOperationException` with a message that names the unresolvable type and the stream.

## Table bootstrap

`CassandraEventStore` uses a `bool _tableCreated` flag checked before every operation. On first access it executes the `CREATE TABLE IF NOT EXISTS` DDL from `CassandraEventSourcingConfiguration.CreateTableCql`. Subsequent calls skip the DDL via the flag. This avoids startup cost if the store is never accessed in a given process lifetime, and avoids repeated DDL round-trips in steady state.

## Lazy session note

`ICluster` is stored at construction time. **No socket is opened until the first call to `GetSessionAsync()`**, which is invoked lazily at the start of every operation. `ICluster`, `IEventStore`, and all DI-resolved services can therefore be composed in tests and at startup without requiring a live Cassandra node.

## Known limitation

The version-read (`MAX(stream_version)`) followed by LWT `INSERT IF NOT EXISTS` is not fully atomic. Two concurrent writers that both observe the same `expectedVersion` will both pass the pre-insert concurrency check and proceed to issue their LWT INSERT statements. The second writer's LWT will receive `[applied]=false` because the clustering key `(stream_id, stream_version)` is already occupied by the first writer's event. The provider catches this, re-reads the current version, and throws `EventStreamConcurrencyException`. The optimistic-concurrency contract is therefore maintained, but it is enforced at the LWT layer rather than through a single atomic compare-and-swap.

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence (`ISnapshotStore` is not implemented)
- projection rebuild orchestration
- archival or retention management
- background stream replay workers
- multi-datacenter consistency level configuration
- token-aware load balancing
- transport or event-bus integration
- per-event `CorrelationId` or `TenantId` column population (fields absent from the table schema in this slice)

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Cassandra](data-cassandra.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.EventSourcing.Redis](event-sourcing-redis.md)
- [Cephalon.EventSourcing.Neo4j](event-sourcing-neo4j.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
