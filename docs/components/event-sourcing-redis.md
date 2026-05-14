# Cephalon.EventSourcing.Redis

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.Redis` is the Redis Streams event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.MongoDB`. It delivers the `IEventStore` contract against Redis Streams (`XADD`/`XRANGE`) instead of a document collection or relational table.

## What it owns

- a Redis Streams-backed implementation of `IEventStore` registered through `AddCephalonRedisEventSourcing()`
- a Redis Hash-backed implementation of `ISnapshotStore` for provider-durable latest snapshots
- a sanitized `IEventStoreContributor` entry projected through `IEventStoreCatalog` and the `event-sourcing` runtime surface
- stream key naming via `RedisEventSourcingConfiguration.StreamKey(keyPrefix, streamId)` → `{keyPrefix}stream:{streamId}`
- snapshot key naming via `{keyPrefix}snapshot:{streamId}:{stateType}`, where `stateType` follows the shared `{AssemblyName}:{FullTypeName}` snapshot identity used by the other durable providers
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- `AppendAsync` validates that each event's `StreamId` matches the target stream and that declared `StreamVersion` values are sequential from the expected version before issuing any `XADD` commands
- event payload serialization through the shared Cephalon event-type registry
- event type round-tripping through stable registry names, with descriptor aliases for legacy `AssemblyQualifiedName` rows
- stream replay through `ReadStreamAsync` returning events filtered by `StreamVersion >= fromVersion` in ascending stream-entry order
- `GetVersionAsync` returning `-1` for a stream that does not exist yet, by reading the last Redis Stream entry in descending order
- latest-snapshot save/load through a Redis Hash that rejects stale snapshot rewinds with an atomic Lua compare-and-set script
- `snapshotLifecycle = provider-durable`, `snapshotStorage = Redis Hash latest-snapshot`, and `snapshotKeyPrefix = {keyPrefix}snapshot:` metadata so the core `event-sourcing` runtime surface can report Redis as a provider-durable snapshot provider
- is covered by the opt-in Redis live-provider canary in `tests/Cephalon.Tests.ProviderIntegration`, which runs against a real Redis runtime through either Testcontainers or `CEPHALON_PROVIDER_REDIS_CONNECTION_STRING`

## Main surfaces

- `RedisEventStore.cs`
- `RedisEventSourcingConfiguration.cs`
- `Hosting/RedisEventSourcingServiceCollectionExtensions.cs`
- `Services/RedisEventStoreContributor.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore`, `ISnapshotStore`, `IEventStreamReplayWorker`, `IDomainEvent`, and replay/reporting contracts. `Cephalon.EventSourcing.Redis` supplies the Redis Streams implementation for event persistence and the Redis Hash implementation for latest snapshots so event-sourced aggregates can keep the same provider-neutral `IEventStore`, `ISnapshotStore`, and replay-worker injection points while using Redis as the backing store.

The slice is intentionally narrow: it proves append, read, optimistic concurrency, provider-durable latest-snapshot persistence, and managed replay handoff against Redis. Projection rebuild still runs through the shared core replay worker and registered `IProjection<IDomainEvent>` services; this provider does not own named projection orchestration, archival, retention, distributed replay, or hosted replay workers.

## Registration

```csharp
builder.Services.AddCephalonRedisEventSourcing(
    configuration: "localhost:6379");
```

The `keyPrefix` parameter defaults to `"cephalon:"` and can be overridden:

```csharp
builder.Services.AddCephalonRedisEventSourcing(
    configuration: "localhost:6379",
    keyPrefix: "myapp:");
```

The method registers `IConnectionMultiplexer`, `IEventStore`, `ISnapshotStore`, and the shared event-type registry using additive semantics — a host that already registered a shared `IConnectionMultiplexer` keeps its own instance. The host still registers concrete event payloads through `AddCephalonEventType<TEvent>(...)` or `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`.

## Redis stream entry fields

Each domain event is stored as one Redis Stream entry under the key `{keyPrefix}stream:{streamId}`.

| Field | Type | Notes |
|-------|------|-------|
| `StreamVersion` | string (long) | Per-stream monotonic version assigned by the append logic |
| `EventType` | string | Stable Cephalon event-type registry name |
| `Payload` | string | Serialized event body produced by the registered event-type descriptor |
| `OccurredAtUtc` | string | ISO 8601 UTC timestamp from `IDomainEvent.OccurredAtUtc` |
| `AppendedAtUtc` | string | ISO 8601 UTC wall-clock time of the `XADD` batch |

The Redis Stream entry ID (auto-generated by `XADD *`) is a monotonic timestamp-sequence pair managed by Redis and is not used for stream versioning. Stream version is tracked explicitly in the `StreamVersion` field.

## Redis snapshot hash fields

Each latest snapshot is stored as one Redis Hash under `{keyPrefix}snapshot:{streamId}:{stateType}`. The provider stores one latest snapshot per stream/state-type pair.

| Field | Type | Notes |
|-------|------|-------|
| `StreamId` | string | The normalized logical stream identifier |
| `StateType` | string | Stable `{AssemblyName}:{FullTypeName}` state type key |
| `StreamVersion` | string (long) | The stream version represented by the snapshot |
| `Payload` | string | JSON serialized aggregate state |
| `SavedAtUtc` | string | ISO 8601 UTC wall-clock time when the snapshot was saved |

Snapshot saves use an atomic Lua script: if the existing hash already represents a newer version, the save fails with `InvalidOperationException`; otherwise the hash is inserted or replaced in one Redis script execution. This hardens the snapshot lifecycle without changing the documented Redis Stream append concurrency limitation below.

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `0` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before any `XADD` |
| Concurrent writer commits same version (race after version read) | The second writer's pre-insert check catches the mismatch on the next `AppendAsync` call; the race window between the version read and the `XADD` calls is a known limitation — see below |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown — events must declare the version the provider will assign |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

**Known concurrency limitation**: unlike the MongoDB provider which has an atomic unique index on `(StreamId, StreamVersion)` as a secondary guard, the Redis provider performs an optimistic pre-check but has no atomic test-and-set. A narrow concurrent race between the `GetVersionAsync` read and the `XADD` commands can allow two writers to commit conflicting versions to the same stream. This is an explicit, documented tradeoff for this slice. Hardening with a Lua script or Redis transactions (MULTI/EXEC) is an honest follow-up slice.

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` issues a `XRANGE {streamKey} - +` command to read all entries, then filters client-side by `StreamVersion >= fromVersion`. It returns an `IAsyncEnumerable<IDomainEvent>`, yielding events in the order returned by `XRANGE` (ascending Redis Stream entry ID order, which corresponds to append order). The event payload is resolved through `IEventTypeRegistry` by `EventType`; a missing descriptor throws `InvalidOperationException` with a message that names the unregistered type name and the stream. Descriptors include legacy `AssemblyQualifiedName` aliases by default so older entries can still be read after hosts register the concrete event type.

## Live provider validation

`tests/Cephalon.Tests.ProviderIntegration` contains `RedisProviderIntegrationTests.RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis`. The test is discovered by default but skipped unless `CEPHALON_PROVIDER_EXTERNAL_SERVICES=1` is set and either `CEPHALON_PROVIDER_TESTCONTAINERS=1` or `CEPHALON_PROVIDER_REDIS_CONNECTION_STRING` is provided.

When enabled, the canary proves the Redis provider against a live Redis runtime: `IEventStoreContributor` / `IEventStoreCatalog` descriptor projection, append from `expectedVersion: -1`, ordered replay through `ReadStreamAsync`, `GetVersionAsync` returning the latest zero-based stream version, raw Redis Stream length, provider-durable `ISnapshotStore` save/load through Redis Hashes, snapshot-assisted managed replay through `IEventStreamReplayWorker`, registered projection rebuild, final snapshot saveback, stale snapshot rewind rejection, runtime-surface readback for `providerDurableSnapshotProviders = redis`, and `EventStreamConcurrencyException` before a mismatched append writes another entry.

## Not shipped in this slice

This provider intentionally does not claim:

- projection rebuild orchestration
- archival or retention management
- background stream replay workers
- Redis Pub/Sub or Consumer Group integration
- atomic Redis Stream append concurrency via Lua scripting or WATCH/MULTI/EXEC
- multi-tenancy discriminator population
- transport or event-bus integration

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Redis](data-redis.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
