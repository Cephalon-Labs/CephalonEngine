# Cephalon.Data.ClickHouse

`Cephalon.Data.ClickHouse` is the ClickHouse analytics store companion pack for Cephalon, following the same companion-pack pattern established by `Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`, `Cephalon.Data.Neo4j`, and `Cephalon.Data.Cassandra` without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a scoped `IOutbox` backed by a ClickHouse `ReplacingMergeTree` table when `RegisterOutbox` is enabled; a new `ClickHouseConnection` is created per operation (no long-lived connection object)
- registers a scoped `IInbox` backed by a ClickHouse `ReplacingMergeTree` table when `RegisterInbox` is enabled; uses `SELECT ... FINAL` to force deduplication at read time
- ensures outbox staging is eventually idempotent through ClickHouse `ReplacingMergeTree(created_at_utc)` with `ORDER BY (message_id)` — duplicate rows with the same `message_id` are merged asynchronously by ClickHouse background merge processes
- ensures inbox reads are idempotent via the `FINAL` modifier, which forces merge-time deduplication at query execution
- exposes operator-facing outbox and inbox descriptors when the respective path is enabled
- declares the ClickHouse outbox as `DispatchPolicy.PolicyId = unsupported` with `ExecutionMode = disabled` so `/engine/outboxes`, `event-dispatches`, and `/engine/snapshot` report staging-only truth instead of a generic "not configured" answer
- projects the outbox descriptor through the `event-driven-integration` technology surface as `outbox-producers` with `provider: "clickhouse"` and `mode: "replacing-merge-tree"` when that technology is active
- projects the inbox descriptor through the same technology surface as `inbox-stores` when the technology is active
- publishes capability metadata `data.clickhouse`, `data.analytics-store`, and optionally `data.outbox.clickhouse` and `data.inbox.clickhouse` introspectable at runtime through the manifest

## Main surfaces

- `Configuration/ClickHouseDataOptions.cs`
- `Modules/ClickHouseDataModule.cs`
- `Registration/ClickHouseDataEngineBuilderExtensions.cs`
- `Services/ClickHouseOutbox.cs`
- `Services/ClickHouseOutboxRuntimeSurfaceContributor.cs`
- `Services/ClickHouseInbox.cs`
- `Services/ClickHouseInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.ClickHouse` adds the ClickHouse-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages using an analytics-optimized columnar store.

The slice is intentionally narrow: it proves the companion-pack pattern extends cleanly to ClickHouse, ships an eventually-idempotent outbox and inbox suited to high-throughput analytics workloads, and exposes the same runtime introspection surfaces as the other provider packs. `IReadStore` and `IWriteStore` are not backed directly by ClickHouse in this slice, and the outbox remains explicitly staging-only until Cephalon has a truthful ClickHouse-native mutable dispatch-state design.

## Registration

```csharp
engine.AddClickHouseData(
    host: "localhost",
    database: "myapp");
```

To enable the outbox and inbox paths:

```csharp
engine.AddClickHouseData(
    host: "localhost",
    database: "myapp",
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.TablePrefix = "app_";  // optional — prefix all Cephalon tables
    });
```

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Host` | `string` | `"localhost"` | ClickHouse host address |
| `Port` | `int` | `8123` | ClickHouse HTTP port |
| `Database` | `string` | `"default"` | Target ClickHouse database |
| `Username` | `string` | `"default"` | ClickHouse username |
| `Password` | `string` | `""` | ClickHouse password |
| `TablePrefix` | `string` | `"cephalon_"` | Optional prefix for all Cephalon-managed table names |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by the `{TablePrefix}outbox_messages` table |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by the `{TablePrefix}inbox_receipts` table |

## Outbox table schema (`{TablePrefix}outbox_messages`)

The table name is `{TablePrefix}outbox_messages` (default: `cephalon_outbox_messages`).

| Column | ClickHouse type | Notes |
|--------|-----------------|-------|
| `message_id` | `String` | Idempotency key (GUID); ORDER BY key for ReplacingMergeTree deduplication |
| `channel_id` | `String` | Channel the message targets |
| `message_type` | `String` | Fully qualified CLR message type name |
| `payload` | `String` | `System.Text.Json`-serialized message body |
| `content_type` | `String` | MIME type of the payload |
| `correlation_id` | `String` | Optional causality tracking identifier |
| `tenant_id` | `String` | Optional multi-tenancy discriminator |
| `occurred_at_utc` | `DateTime64(3, 'UTC')` | UTC time at which the domain event or message occurred |
| `created_at_utc` | `DateTime64(3, 'UTC')` | UTC wall-clock time when the row was staged; used as the ReplacingMergeTree version column |
| `dispatch_attempt_count` | `Int32` | Incremented on each dispatch attempt; starts at 0 |
| `headers_json` | `String` | `System.Text.Json`-serialized headers dictionary |
| `metadata_json` | `String` | `System.Text.Json`-serialized metadata dictionary |

**Engine**: `ReplacingMergeTree(created_at_utc) ORDER BY (message_id)`

## Inbox table schema (`{TablePrefix}inbox_receipts`)

The table name is `{TablePrefix}inbox_receipts` (default: `cephalon_inbox_receipts`).

| Column | ClickHouse type | Notes |
|--------|-----------------|-------|
| `message_id` | `String` | Processed message id; ORDER BY key for ReplacingMergeTree deduplication |
| `channel_id` | `String` | Channel the message arrived on |
| `message_type` | `String` | Fully qualified CLR message type name |
| `correlation_id` | `String` | Optional causality tracking identifier |
| `tenant_id` | `String` | Optional multi-tenancy discriminator |
| `received_at_utc` | `DateTime64(3, 'UTC')` | UTC time at which the message was received |
| `processed_at_utc` | `DateTime64(3, 'UTC')` | UTC wall-clock time when the row was written; used as the ReplacingMergeTree version column |

**Engine**: `ReplacingMergeTree(processed_at_utc) ORDER BY (message_id)`

## Idempotency semantics

### Outbox eventual idempotency

`EnqueueAsync` inserts a row into the `ReplacingMergeTree` table. Duplicate rows with the same `message_id` are deduplicated asynchronously during ClickHouse background merge processes:

- **Immediate visibility**: Both the original and duplicate rows may be returned by `SELECT` queries until a merge cycle completes.
- **After merge**: Only the row with the latest `created_at_utc` value is retained.
- **Read-time deduplication**: Use `SELECT ... FINAL` to force merge-time deduplication at query execution, at the cost of slower reads.

This idempotency model is suitable for high-throughput analytics-oriented outbox workloads. It is **not suitable** for strict exactly-once guarantees in transactional workloads — use `Cephalon.Data.Cassandra` (LWT `INSERT IF NOT EXISTS`) or `Cephalon.Data.Redis` for that.

### Inbox FINAL-read deduplication

`HasProcessedAsync` issues `SELECT count() FINAL WHERE message_id = ?`. The `FINAL` modifier forces ClickHouse to deduplicate rows from the `ReplacingMergeTree` at read time, providing a consistent view even before background merges complete.

## Lazy connection note

`ClickHouseConnection` is an ADO.NET `DbConnection`. **No socket is opened when the connection is constructed.** The TCP connection to the ClickHouse HTTP endpoint is deferred until `connection.OpenAsync()` is called. A new connection is created per operation — there is no long-lived connection object held by the outbox, inbox, or event store.

This means `IOutbox`, `IInbox`, and `IEventStore` can all be resolved from the DI container in tests or at startup without requiring a live ClickHouse server.

## Runtime capabilities

When `ClickHouseDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.clickhouse` | Always |
| `data.analytics-store` | Always |
| `data.outbox.clickhouse` | `RegisterOutbox = true` |
| `data.inbox.clickhouse` | `RegisterInbox = true` |

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | Key metadata |
|---------|----------|--------------|
| `outbox-producers` | `clickhouse-outbox` | `provider = clickhouse`, `dispatchPolicyId = unsupported`, `dispatchExecutionMode = disabled` |
| `inbox-stores` | `clickhouse-inbox` | `provider = clickhouse` |

## Dispatch-policy stance

When `RegisterOutbox = true`, the outbox descriptor stays visible through `/engine/outboxes`, `event-dispatches`, and `snapshot.Outboxes`, but it now answers with an explicit unsupported policy instead of a generic disabled/default placeholder:

- `DispatchPolicy.PolicyId = unsupported`
- `DispatchPolicy.ExecutionMode = disabled`
- `dispatchStore = unsupported`
- `dispatchRuntime = unsupported`
- `dispatchPolicy.reason = append-only-analytics-store`

That answer is deliberate. The current ClickHouse `ReplacingMergeTree` baseline is good for durable staging/history and analytics-style replay, but it is not yet the truthful owner for mutable pending-dispatch state in the same way as the current Entity Framework, MongoDB, Redis, Elasticsearch, OpenSearch, Neo4j, Qdrant, NATS, or Cassandra follow-through slices.

## Not shipped in this slice

This pack intentionally does not claim:

- batch dispatch or broker retry scheduling
- provider-native `IEventDispatchStore` ownership for mutable pending-dispatch state
- TTL-based expiry of outbox or inbox rows
- Materialized Views for event stream projections
- Dictionary-based deduplication (alternative to FINAL reads)
- `IReadStore` / `IWriteStore` dispatch backed by ClickHouse — query handlers should use `ClickHouseConnection` directly
- transaction-scoped outbox staging (ClickHouse does not support multi-row ACID transactions)
- strict exactly-once delivery guarantees (use `Cephalon.Data.Cassandra` LWT for that)

These remain explicit later slices. Any future managed-dispatch follow-through for ClickHouse should start with a dedicated mutable pending-dispatch design rather than pretending the current staging table is already a truthful dispatch-state owner.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.ClickHouse](event-sourcing-clickhouse.md)
- [Cephalon.Data.Cassandra](data-cassandra.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.Redis](data-redis.md)
- [Cephalon.Data.Neo4j](data-neo4j.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
