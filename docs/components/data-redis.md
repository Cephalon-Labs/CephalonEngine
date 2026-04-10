# Cephalon.Data.Redis

`Cephalon.Data.Redis` is the Redis key-value store companion pack for Cephalon, proving that the companion-pack pattern established by `Cephalon.Data.MongoDB` extends cleanly to key-value stores without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `IConnectionMultiplexer` from a StackExchange.Redis connection string, using `TryAdd` semantics so a host-owned multiplexer is never displaced
- registers a scoped `IOutbox` backed by Redis Hash and Sorted Set structures when `RegisterOutbox` is enabled; key names honour the configurable `KeyPrefix`
- registers a scoped `IEventDispatchStore` over those same Redis structures when `RegisterOutbox` is enabled, so staged Redis events can be read and durable dispatch outcomes can be written back through the runtime-neutral eventing contract
- registers a scoped `IInbox` backed by a Redis Set when `RegisterInbox` is enabled; key names honour the configurable `KeyPrefix`
- ensures that outbox staging is idempotent by using a Redis transaction conditioned on the Hash key not existing (`Condition.KeyNotExists`) — if the hash already exists, the transaction silently no-ops
- exposes operator-facing outbox and inbox descriptors through `/engine/outboxes`, `/engine/inboxes`, and `/engine/snapshot` when the respective path is enabled
- projects the outbox descriptor through the `event-driven-integration` technology surface as `outbox-producers` with `provider: "redis"` and `mode: "sorted-set"` when that technology is active
- projects the inbox descriptor through the same technology surface as `inbox-stores` when the technology is active
- publishes capability metadata `data.redis`, `data.key-value-store`, and optionally `data.outbox.redis` and `data.inbox.redis` introspectable at runtime through the manifest

## Main surfaces

- `Configuration/RedisDataOptions.cs`
- `Modules/RedisDataModule.cs`
- `Registration/RedisDataEngineBuilderExtensions.cs`
- `Services/RedisOutbox.cs`
- `Services/RedisEventDispatchStore.cs`
- `Services/RedisInbox.cs`
- `Services/RedisOutboxRuntimeSurfaceContributor.cs`
- `Services/RedisInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.Redis` adds the Redis-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages without a relational store, and it now also exposes the same staged outbox through `IEventDispatchStore` so consumer-managed or adapter-owned dispatch loops can read pending items and persist durable dispatch outcomes without Redis-specific host glue.

The slice is intentionally narrow and honest: it proves the companion-pack pattern works for key-value stores, ships an idempotent outbox using Hash + Sorted Set structures, an idempotency-guarded inbox using a Redis Set, and exposes the same runtime introspection surfaces as the MongoDB and Entity Framework providers. `IReadStore` and `IWriteStore` are not backed directly by Redis in this slice — query and command handlers should depend on Redis directly through `IConnectionMultiplexer`. Full CQRS query/command dispatch on top of Redis remains a later slice.

## Registration

```csharp
engine.AddRedisData("localhost:6379");
```

To enable the outbox and inbox paths:

```csharp
engine.AddRedisData(
    connectionString: "localhost:6379",
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.KeyPrefix = "myapp:";  // optional — prefix all Cephalon keys
    });
```

The `connectionString` parameter accepts any StackExchange.Redis connection string, including options such as `abortConnect=false`, `password=...`, and `ssl=true`.

For configuration-driven hosts, prefer the options overload and let the pack resolve either
`ConnectionStringName` from the root `ConnectionStrings` section or `ConnectionString` directly:

```csharp
engine.AddRedisData(options =>
{
    configuration.GetSection(RedisDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "Redis";
});
```

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "Engine": {
    "Data": {
      "Redis": {
        "ConnectionStringName": "Redis",
        "KeyPrefix": "myapp:"
      }
    }
  }
}
```

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the pack
fails fast during service resolution. If neither is set, Redis falls back to `localhost:6379`.

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionStringName` | `string?` | `null` | Root `ConnectionStrings` key to resolve for Redis |
| `ConnectionString` | `string?` | `null` | Inline StackExchange.Redis connection string or configuration |
| `KeyPrefix` | `string` | `"cephalon:"` | Prefix applied to all Cephalon-managed Redis keys |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by a Redis Hash and Sorted Set |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by a Redis Set |

## Outbox key schema

The outbox uses two Redis data structures per staged message.

### Message Hash — `{KeyPrefix}outbox:msg:{messageId}`

| Field | Type | Notes |
|-------|------|-------|
| `MessageId` | string | Stable logical message identifier; Hash existence is the idempotency guard |
| `ChannelId` | string | Logical channel or destination identifier |
| `MessageType` | string | Logical message type identifier |
| `Payload` | string | Serialized payload that should be delivered later |
| `ContentType` | string | Payload content type; empty string when not provided |
| `CorrelationId` | string | Correlation identifier; empty string when not provided |
| `TenantId` | string | Tenant identifier; empty string when not provided |
| `OccurredAtUtc` | string | ISO 8601 UTC timestamp of the domain event |
| `CreatedAtUtc` | string | ISO 8601 UTC timestamp when the message was staged |
| `DispatchAttemptCount` | string | Dispatch attempt counter; starts at 0 |
| `HeadersJson` | string | `System.Text.Json`-serialized message headers dictionary |
| `MetadataJson` | string | `System.Text.Json`-serialized message metadata dictionary |

### Pending Sorted Set — `{KeyPrefix}outbox:pending`

Members are `messageId` values. The score is `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` at the time of staging, which provides a natural dispatch-ordering window for an external relay.

**Idempotency**: `EnqueueAsync` runs a Redis transaction conditioned on `KeyNotExists(hashKey)`. If the hash already exists, the transaction does not execute — no error is raised. Calling `EnqueueAsync` twice with the same `MessageId` is safe.

## Inbox key schema

### Receipts Set — `{KeyPrefix}inbox:receipts`

The set members are raw `messageId` strings.

**Idempotency**: `SADD` (Redis set add) is naturally idempotent — adding an existing member is a no-op. `MarkProcessedAsync` always calls `SADD`; calling it twice for the same id is safe.

## Runtime capabilities

When `RedisDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.redis` | Always |
| `data.key-value-store` | Always |
| `data.outbox.redis` | `RegisterOutbox = true` |
| `data.inbox.redis` | `RegisterInbox = true` |

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `redis-outbox` | `redis` |
| `inbox-stores` | `redis-inbox` | `redis` |

## Not shipped in this slice

This pack intentionally does not claim:

- `IReadStore` / `IWriteStore` dispatch backed by Redis — query and command handlers should use `IConnectionMultiplexer` directly
- transaction-scoped outbox staging with Lua script atomic test-and-set (the current implementation uses a Redis transaction with `KeyNotExists` condition, which is safe against the single-entry idempotency concern but is not a WATCH-based optimistic lock)
- pack-owned dispatch loops or broker-specific retry scheduling beyond the runtime-neutral `IEventDispatchStore` bridge
- TTL or key-expiry management for outbox or inbox entries
- Pub/Sub integration
- Redis Cluster sharding guidance

These remain explicit later slices to keep the initial provider claim honest.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.Redis](event-sourcing-redis.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
