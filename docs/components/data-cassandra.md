# Cephalon.Data.Cassandra

`Cephalon.Data.Cassandra` is the Apache Cassandra wide-column store companion pack for Cephalon, following the same companion-pack pattern established by `Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`, and `Cephalon.Data.Neo4j` without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `ICluster` built from one or more contact-point hosts and a port via `Cluster.Builder()...Build()`, using `TryAdd` semantics so a host-owned cluster is never displaced
- registers a scoped `IOutbox` backed by the `{TablePrefix}outbox_messages` Cassandra table when `RegisterOutbox` is enabled; the session is opened lazily on first use
- registers a scoped `IInbox` backed by the `{TablePrefix}inbox_receipts` Cassandra table when `RegisterInbox` is enabled; the session is opened lazily on first use
- ensures outbox and inbox staging are idempotent through Cassandra Lightweight Transaction (LWT) `INSERT IF NOT EXISTS`; an `[applied]=false` result means the row already exists and is silently treated as success
- exposes operator-facing outbox and inbox descriptors when the respective path is enabled
- projects the outbox descriptor through the `event-driven-integration` technology surface as `outbox-producers` with `provider: "cassandra"` and `mode: "wide-column-lwt"` when that technology is active
- projects the inbox descriptor through the same technology surface as `inbox-stores` when the technology is active
- publishes capability metadata `data.cassandra`, `data.wide-column-store`, and optionally `data.outbox.cassandra` and `data.inbox.cassandra` introspectable at runtime through the manifest

## Main surfaces

- `Configuration/CassandraDataOptions.cs`
- `Modules/CassandraDataModule.cs`
- `Registration/CassandraDataEngineBuilderExtensions.cs`
- `Services/CassandraOutbox.cs`
- `Services/CassandraOutboxRuntimeSurfaceContributor.cs`
- `Services/CassandraInbox.cs`
- `Services/CassandraInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.Cassandra` adds the Cassandra-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages using a wide-column store.

The slice is intentionally narrow: it proves the companion-pack pattern extends cleanly to Cassandra, ships an idempotent outbox and inbox, and exposes the same runtime introspection surfaces as the other provider packs. `IReadStore` and `IWriteStore` are not backed directly by Cassandra in this slice.

## Registration

```csharp
engine.AddCassandraData(
    contactPoints: "localhost",
    keyspace: "myapp");
```

To enable the outbox and inbox paths:

```csharp
engine.AddCassandraData(
    contactPoints: "localhost",
    keyspace: "myapp",
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.TablePrefix = "app_";  // optional — prefix all Cephalon tables
    });
```

Multi-node clusters:

```csharp
engine.AddCassandraData(
    contactPoints: "node1,node2,node3",
    keyspace: "myapp");
```

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ContactPoints` | `string` | `"localhost"` | Comma-separated Cassandra contact-point host addresses |
| `Port` | `int` | `9042` | Cassandra native transport port |
| `Keyspace` | `string` | `"cephalon"` | Target Cassandra keyspace |
| `TablePrefix` | `string` | `"cephalon_"` | Optional prefix for all Cephalon-managed table names |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by the `{TablePrefix}outbox_messages` table |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by the `{TablePrefix}inbox_receipts` table |

## Outbox table schema (`{TablePrefix}outbox_messages`)

The table name is `{TablePrefix}outbox_messages` (default: `cephalon_outbox_messages`).

| Column | CQL type | Notes |
|--------|----------|-------|
| `message_id` | `text` | Primary key — idempotency key (GUID); uniqueness enforced by Cassandra partition key semantics |
| `channel_id` | `text` | Channel the message targets |
| `message_type` | `text` | Fully qualified CLR message type name |
| `payload` | `text` | `System.Text.Json`-serialized message body |
| `content_type` | `text` | MIME type of the payload |
| `correlation_id` | `text` | Optional causality tracking identifier |
| `tenant_id` | `text` | Optional multi-tenancy discriminator |
| `occurred_at_utc` | `timestamp` | UTC time at which the domain event or message occurred |
| `created_at_utc` | `timestamp` | UTC wall-clock time when the row was staged |
| `dispatch_attempt_count` | `int` | Incremented on each dispatch attempt; starts at 0 |
| `headers_json` | `text` | `System.Text.Json`-serialized headers dictionary |
| `metadata_json` | `text` | `System.Text.Json`-serialized metadata dictionary |

**Idempotency**: `EnqueueAsync` issues `INSERT ... IF NOT EXISTS`. If `[applied]=false` (row already exists for that `message_id`), the duplicate is silently ignored — calling `EnqueueAsync` twice for the same message id is safe.

## Inbox table schema (`{TablePrefix}inbox_receipts`)

The table name is `{TablePrefix}inbox_receipts` (default: `cephalon_inbox_receipts`).

| Column | CQL type | Notes |
|--------|----------|-------|
| `message_id` | `text` | Primary key — processed message id |
| `channel_id` | `text` | Channel the message arrived on |
| `message_type` | `text` | Fully qualified CLR message type name |
| `correlation_id` | `text` | Optional causality tracking identifier |
| `tenant_id` | `text` | Optional multi-tenancy discriminator |
| `received_at_utc` | `timestamp` | UTC time at which the message was received |
| `processed_at_utc` | `timestamp` | UTC wall-clock time when the row was written |

**Idempotency**: `MarkProcessedAsync` issues `INSERT ... IF NOT EXISTS`. If `[applied]=false` (row already exists), the call is a no-op — calling `MarkProcessedAsync` twice for the same id is safe.

## Idempotency semantics

Both `CassandraOutbox` and `CassandraInbox` use Cassandra Lightweight Transactions (LWT):

```cql
INSERT INTO {table} (...) VALUES (...) IF NOT EXISTS
```

When Cassandra executes an LWT statement it returns a result row containing a boolean column named `[applied]`:

- `[applied] = true` — the row was inserted; this is the happy path.
- `[applied] = false` — a row with the same primary key already exists; the insert was rejected. The Cephalon providers treat this as a silent success — the operation is idempotent and the caller does not need to distinguish between a first write and a repeated write.

## Lazy session note

`ICluster` is created by `Cluster.Builder()...Build()` during DI registration. **No socket is opened at this point.** The underlying TCP connection to the Cassandra node(s) is deferred until the first call to `cluster.ConnectAsync(keyspace)`, which happens inside `CassandraOutbox` or `CassandraInbox` on first use. This means `ICluster`, `IOutbox`, and `IInbox` can all be resolved from the DI container in tests or at startup without requiring a live Cassandra cluster.

A `SemaphoreSlim(1,1)` guards the lazy session initialization so that concurrent first-calls do not race to open multiple sessions.

## Runtime capabilities

When `CassandraDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.cassandra` | Always |
| `data.wide-column-store` | Always |
| `data.outbox.cassandra` | `RegisterOutbox = true` |
| `data.inbox.cassandra` | `RegisterInbox = true` |

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `cassandra-outbox` | `cassandra` |
| `inbox-stores` | `cassandra-inbox` | `cassandra` |

## Not shipped in this slice

This pack intentionally does not claim:

- batch dispatch or broker retry scheduling
- TTL-based expiry of outbox or inbox rows
- multi-datacenter topology configuration (replication factor, consistency level overrides)
- token-aware load balancing policy configuration
- `IReadStore` / `IWriteStore` dispatch backed by Cassandra — query and command handlers should use `ISession` directly
- transaction-scoped outbox staging (Cassandra does not support multi-row ACID transactions in the general case)

These remain explicit later slices.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.Cassandra](event-sourcing-cassandra.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.Redis](data-redis.md)
- [Cephalon.Data.Neo4j](data-neo4j.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
