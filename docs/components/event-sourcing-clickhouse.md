# Cephalon.EventSourcing.ClickHouse

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.ClickHouse` is the ClickHouse event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.MongoDB`, `Cephalon.EventSourcing.Redis`, `Cephalon.EventSourcing.Neo4j`, and `Cephalon.EventSourcing.Cassandra`. It delivers the `IEventStore` contract against a ClickHouse `MergeTree` table ordered by `(stream_id, stream_version)`, with application-layer optimistic concurrency via a pre-read version check.

## What it owns

- a ClickHouse-backed implementation of `IEventStore` registered through `AddCephalonClickHouseEventSourcing()`
- a sanitized `IEventStoreContributor` entry projected through `IEventStoreCatalog` and the `event-sourcing` runtime surface
- the `ClickHouseEventEntry` record model for persisted domain event rows
- `ClickHouseEventSourcingConfiguration` that holds the DDL template for the event-streams table using `MergeTree()` ordered by `(stream_id, stream_version)`
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- stream replay through `ReadStreamAsync` returning events ordered by `stream_version` ascending as an `IAsyncEnumerable<IDomainEvent>`
- `GetVersionAsync` returning `-1` for a stream that does not exist yet
- event payload serialization through the shared Cephalon event-type registry
- event type round-tripping through stable registry names, with descriptor aliases for legacy `AssemblyQualifiedName` rows
- lazy table bootstrap — `CREATE TABLE IF NOT EXISTS` is executed once per instance using a `SemaphoreSlim(1,1)` guard
- connection-per-operation pattern — a new `ClickHouseConnection` is created for each database operation; no long-lived connection object is held

## Main surfaces

- `ClickHouseEventEntry.cs`
- `ClickHouseEventStore.cs`
- `ClickHouseEventSourcingConfiguration.cs`
- `Hosting/ClickHouseEventSourcingServiceCollectionExtensions.cs`
- `Services/ClickHouseEventStoreContributor.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore` contract, the `IDomainEvent` marker, and `EventStreamConcurrencyException`. `Cephalon.EventSourcing.ClickHouse` supplies the ClickHouse columnar implementation of that contract so event-sourced aggregates can keep the same `IEventStore` injection point while using ClickHouse as the append-only analytics backing store.

## Registration

```csharp
builder.Services.AddCephalonClickHouseEventSourcing(
    host: "localhost",
    database: "myapp");
```

The `tableName` parameter defaults to `"cephalon_event_streams"` and can be overridden:

```csharp
builder.Services.AddCephalonClickHouseEventSourcing(
    host: "localhost",
    database: "myapp",
    tableName: "domain_events",
    username: "cephalon",
    password: "secret");
```

The method registers `IEventStore` and the shared event-type registry using `TryAdd` semantics — a host that already registered a shared `IEventStore` keeps its own instance. The host still registers concrete event payloads through `AddCephalonEventType<TEvent>(...)` or `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`.

## Event-streams table schema

| Column | ClickHouse type | Notes |
|--------|-----------------|-------|
| `stream_id` | `String` | Logical aggregate / stream identifier |
| `stream_version` | `Int64` | Per-stream monotonic version |
| `event_type` | `String` | Stable Cephalon event-type registry name |
| `payload` | `String` | Serialized event body produced by the registered event-type descriptor |
| `occurred_at_utc` | `DateTime64(3, 'UTC')` | `IDomainEvent.OccurredAtUtc` as stored by the domain event |
| `appended_at_utc` | `DateTime64(3, 'UTC')` | UTC wall-clock time of the append operation |

**Engine**: `MergeTree() ORDER BY (stream_id, stream_version)`

The `MergeTree` engine is used because domain events are immutable — no deduplication or replacement is needed. Unlike `ReplacingMergeTree`, `MergeTree` preserves every appended row permanently.

The `ORDER BY (stream_id, stream_version)` clause makes per-stream range queries efficient and stores events in version order within each logical stream. ClickHouse physically stores rows sorted by this key on disk.

**Important**: ClickHouse `MergeTree` does **not** enforce uniqueness on the `ORDER BY` key. Two concurrent writers can both INSERT with the same `(stream_id, stream_version)` without database-level rejection. See the [Concurrency semantics](#concurrency-semantics) section below.

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `1` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before any insert |
| Concurrent writer commits same version (race after version read) | Both INSERTs succeed at the database level — MergeTree does not reject duplicates. The pre-read check catches the conflict only if observed before the race window closes. |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown — events must declare the version the provider will assign |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

## Known limitation — optimistic concurrency

The version-read (`max(stream_version)`) followed by INSERT is **not atomic**. Two concurrent writers that both observe the same `expectedVersion` will both pass the pre-insert check and both INSERT successfully — ClickHouse `MergeTree` does NOT enforce uniqueness on `ORDER BY` key columns.

This means:
- `EventStreamConcurrencyException` is thrown only when the pre-read version check fails (catches most single-process conflicts)
- A narrow race window exists between the version read and the INSERT where a concurrent writer can slip through
- For strict concurrency control, wrap `AppendAsync` operations in application-layer distributed locking (e.g. Redis SETNX, database advisory locks, or a distributed lock service)

ClickHouse is optimized for high-throughput append-heavy analytics workloads. This provider is well-suited to scenarios where occasional duplicate detection at the application layer is acceptable, and to single-writer event stream scenarios where strict concurrency is not required.

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` executes:

```sql
SELECT stream_id, stream_version, event_type, payload, occurred_at_utc, appended_at_utc
FROM {tableName}
WHERE stream_id = '{streamId}' AND stream_version >= {fromVersion}
ORDER BY stream_version ASC
```

It returns an `IAsyncEnumerable<IDomainEvent>`, yielding events one by one as the reader is iterated. The event payload is resolved through `IEventTypeRegistry` by `event_type`; a missing descriptor throws `InvalidOperationException` with a message that names the unregistered type name and the stream. Descriptors include legacy `AssemblyQualifiedName` aliases by default so older rows can still be read after hosts register the concrete event type.

## Table bootstrap

`ClickHouseEventStore` uses a `bool _tableCreated` flag checked before every operation. On first access it executes the `CREATE TABLE IF NOT EXISTS` DDL from `ClickHouseEventSourcingConfiguration.CreateTableSql` using a `SemaphoreSlim(1,1)` guard. Subsequent calls skip the DDL via the flag. This avoids startup cost if the store is never accessed in a given process lifetime, and avoids repeated DDL round-trips in steady state.

## Lazy connection note

`ClickHouseEventStore` creates a new `ClickHouseConnection` per operation. **No socket is opened at construction time.** The TCP connection to the ClickHouse HTTP endpoint is deferred until `connection.OpenAsync()` is called for each operation. This means `IEventStore` can be composed in tests and at startup without requiring a live ClickHouse node.

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence (`ISnapshotStore` is not implemented)
- projection rebuild orchestration
- archival or retention management
- background stream replay workers
- database-level uniqueness constraint on `(stream_id, stream_version)` — use application-layer locking for strict concurrency
- Materialized Views for streaming projections
- per-event `CorrelationId` or `TenantId` column population (fields absent from the table schema in this slice)
- transport or event-bus integration

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.ClickHouse](data-clickhouse.md)
- [Cephalon.EventSourcing.Cassandra](event-sourcing-cassandra.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.EventSourcing.Redis](event-sourcing-redis.md)
- [Cephalon.EventSourcing.Neo4j](event-sourcing-neo4j.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
