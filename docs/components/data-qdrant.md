# Cephalon.Data.Qdrant

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.Qdrant` is the Qdrant vector-store companion pack for Cephalon, proving that the companion-pack pattern extends cleanly to vector databases without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `QdrantClient` using `TryAdd` semantics so a host-owned client is never displaced
- registers a scoped `IOutbox` backed by a Qdrant vector collection when `RegisterOutbox` is enabled; uses 1-dimensional dummy vectors (`[0.0f]`) so Qdrant serves as a durable payload store while its vector capabilities remain available for future semantic search use-cases
- registers a scoped `IEventDispatchStore` against the same outbox collection when `RegisterOutbox` is enabled so consumer-managed or adapter-managed dispatch can page pending staged items and persist durable dispatch outcomes without forcing a relational bridge
- registers a scoped `IInbox` backed by a Qdrant vector collection when `RegisterInbox` is enabled
- ensures idempotent staging for both outbox and inbox via point-ID existence checks before upserting — message IDs are mapped to point UUIDs via `Guid.TryParse` or SHA-256 hash (first 16 bytes)
- bootstraps the collection and a payload index on `message_id` on first use
- exposes operator-facing outbox and inbox descriptors through the engine runtime surfaces
- projects the outbox descriptor through the `event-driven-integration` technology surface as outbox producers when that technology is active
- projects the inbox descriptor through the same technology surface as inbox stores when the technology is active
- publishes capability metadata `data.qdrant`, `data.vector-store`, and optionally `data.outbox.qdrant` and `data.inbox.qdrant` introspectable at runtime through the manifest

## Main surfaces

- `Configuration/QdrantDataOptions.cs`
- `Modules/QdrantDataModule.cs`
- `Registration/QdrantDataEngineBuilderExtensions.cs`
- `Services/QdrantOutbox.cs`
- `Services/QdrantEventDispatchStore.cs`
- `Services/QdrantOutboxRuntimeSurfaceContributor.cs`
- `Services/QdrantInbox.cs`
- `Services/QdrantInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.Qdrant` adds the Qdrant-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages against a vector store without switching to a relational or document-oriented provider.

The 1-dimensional dummy vector approach is intentional: Qdrant is used here as a durable payload store. The vector dimension is purely structural — its actual value is irrelevant. The dispatch-store follow-through stays equally honest: it uses paged collection scans plus payload-field updates instead of claiming a broker-owned queue or specialized retry scheduler. That keeps the architecture truthful while leaving the door open for future semantic search capabilities using the same Qdrant collection.

## Registration

```csharp
engine.AddQdrantData("localhost");
```

To enable the outbox and inbox paths:

```csharp
engine.AddQdrantData(
    host: "localhost",
    port: 6334,
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.CollectionPrefix = "myapp_";  // optional — prefix all Cephalon collection names
        options.ApiKey = "secret";             // optional — for authenticated clusters
    });
```

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Host` | `string` | `"localhost"` | Qdrant server hostname |
| `Port` | `int` | `6334` | Qdrant gRPC port |
| `ApiKey` | `string?` | `null` | Optional API key for authenticated clusters |
| `CollectionPrefix` | `string` | `""` | Prefix applied to all managed collection names |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by a Qdrant vector collection |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by a Qdrant vector collection |

## Idempotency semantics

Both `IOutbox.EnqueueAsync` and `IInbox.MarkProcessedAsync` derive a point UUID from the message ID:

- If the message ID is a valid GUID string, it is used directly.
- Otherwise, a UUID is derived via SHA-256 of the string, taking the first 16 bytes.

Before upserting, the implementation calls `RetrieveAsync` to check if the point already exists. If it does, the operation returns without upserting — making repeated calls with the same id safe no-ops.

## Outbox point payload

When `RegisterOutbox` is enabled, each staged outbox point stores:

- `message_id`
- `channel_id`
- `message_type`
- `payload`
- `content_type`
- `correlation_id`
- `tenant_id`
- `occurred_at_utc`
- `created_at_utc`
- `dispatch_attempt_count`
- `headers_json`
- `metadata_json`

When the dispatch-store baseline is in use, the same point may later gain:

- `dispatched_at_utc` when dispatch succeeds or is skipped
- `next_attempt_at_utc` when dispatch is delayed for retry

## Runtime capabilities

When `QdrantDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.qdrant` | Always |
| `data.vector-store` | Always |
| `data.outbox.qdrant` | `RegisterOutbox = true` |
| `data.inbox.qdrant` | `RegisterInbox = true` |

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `qdrant-outbox` | `qdrant` |
| `inbox-stores` | `qdrant-inbox` | `qdrant` |

## Provider integration proof

`LiveDataProviderIntegrationTests.QdrantProvider_StagesOutboxInboxAndDispatchAgainstLiveService` runs behind `ExternalProviderServiceFact(ExternalProviderServiceProvider.Qdrant)`. The lane is skipped unless `CEPHALON_PROVIDER_EXTERNAL_SERVICES` or `CEPHALON_PROVIDER_INTEGRATION` is enabled and either `CEPHALON_PROVIDER_TESTCONTAINERS=1` or `CEPHALON_PROVIDER_QDRANT_HOST` is supplied; port and API key can be supplied through the matching variables for pre-provisioned services. Testcontainers mode starts a disposable `qdrant/qdrant:v1.12.5` service over gRPC. The test composes `Cephalon.Engine`, `Cephalon.Eventing`, and this pack, then proves real outbox/inbox writes, `IEventDispatchStore` pending/success transitions, runtime manifest capabilities, and `event-driven-integration` runtime surfaces against a live Qdrant gRPC service.

## Not shipped in this slice

This pack intentionally does not claim:

- semantic vector search or nearest-neighbor queries
- multi-vector or named-vector collections
- `IReadStore` / `IWriteStore` dispatch backed by Qdrant — query handlers should use `QdrantClient` directly
- transaction-scoped outbox staging spanning multiple Qdrant operations
- adapter-owned dispatch loops or broker retry scheduling beyond the shipped consumer-managed dispatch-store baseline
- change-data-capture or Qdrant streams integration

These remain explicit later slices to keep the initial provider claim honest.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.Qdrant](event-sourcing-qdrant.md)
- [Cephalon.Data.Nats](data-nats.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
