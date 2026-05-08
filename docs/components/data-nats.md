# Cephalon.Data.Nats

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.Nats` is the NATS JetStream ledger-store companion pack for Cephalon, proving that the companion-pack pattern extends cleanly to NATS-based messaging infrastructure without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `INatsConnection` using `TryAdd` semantics so a host-owned connection is never displaced; `NatsConnection` does NOT connect on construction — the connection is deferred to the first operation, which allows DI resolution tests without a live NATS server
- registers a scoped `IOutbox` backed by a NATS JetStream KV bucket when `RegisterOutbox` is enabled
- registers a scoped `IEventDispatchStore` backed by the same NATS JetStream KV bucket when `RegisterOutbox` is enabled so staged entries can participate in consumer-managed or adapter-managed dispatch flows
- registers a scoped `IInbox` backed by a NATS JetStream KV bucket when `RegisterInbox` is enabled
- ensures idempotent staging for both outbox and inbox via `CreateAsync` — if the key already exists, `NatsKVCreateException` is caught and swallowed as a safe no-op
- creates or updates the KV bucket on first use via `CreateOrUpdateStoreAsync`
- keeps one mutable KV value per staged outbox message so dispatch attempts, `dispatchedAtUtc`, and `nextAttemptAtUtc` can be persisted without introducing a second store
- exposes operator-facing outbox and inbox descriptors through the engine runtime surfaces
- projects the outbox descriptor through the `event-driven-integration` technology surface as outbox producers when that technology is active
- projects the inbox descriptor through the same technology surface as inbox stores when the technology is active
- publishes capability metadata `data.nats`, `data.ledger-store`, and optionally `data.outbox.nats` and `data.inbox.nats` introspectable at runtime through the manifest
- redacts inline URI user-info and query secrets in capability metadata; `uri` contains only scheme, host, port, and path, while `uriCredentialsConfigured` and `secretProjection = redacted` preserve operator posture without leaking credentials

## Main surfaces

- `Configuration/NatsDataOptions.cs`
- `Modules/NatsDataModule.cs`
- `Registration/NatsDataEngineBuilderExtensions.cs`
- `Services/NatsOutbox.cs`
- `Services/NatsOutboxRecord.cs`
- `Services/NatsEventDispatchStore.cs`
- `Services/NatsOutboxRuntimeSurfaceContributor.cs`
- `Services/NatsInbox.cs`
- `Services/NatsInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.Nats` adds the NATS-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages against a NATS JetStream KV store without switching to a relational or document-oriented provider.

NATS JetStream KV is modeled as a ledger store: entries are durable, ordered, and replayed by key. This makes it a natural fit for the outbox pattern where message staging must survive process restarts. The current Cephalon slice now goes one step further and keeps the staged outbox payload as a mutable record, which lets `IEventDispatchStore` read pending items and write back durable dispatch outcomes without inventing a second sidecar store. That is still a consumer-managed baseline rather than a broker-owned execution model: Cephalon is using JetStream KV as a durable state bucket for staged publication, not claiming NATS-native retry or subscription ownership.

## Registration

```csharp
engine.AddNatsData("nats://localhost:4222");
```

To enable the outbox and inbox paths:

```csharp
engine.AddNatsData(
    uri: "nats://localhost:4222",
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.BucketPrefix = "myapp";  // optional — prefix all Cephalon KV bucket names
    });
```

To resolve the server URI from the root `Uris` section:

```csharp
engine.AddNatsData(options =>
{
    options.UriName = "Messaging";
    options.RegisterOutbox = true;
    options.RegisterInbox = true;
    options.BucketPrefix = "myapp";
});
```

```json
{
  "Uris": {
    "Messaging": "nats://localhost:4222"
  },
  "Engine": {
    "Data": {
      "Nats": {
        "UriName": "Messaging",
        "BucketPrefix": "myapp"
      }
    }
  }
}
```

`UriName` and `Uri` are mutually exclusive. If both are set, the pack throws during service
registration. Leaving both unset falls back to `nats://localhost:4222`.

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UriName` | `string?` | `null` | Root `Uris` key to resolve for NATS |
| `Uri` | `string?` | `null` | Inline NATS server URI |
| `BucketPrefix` | `string` | `"cephalon"` | Prefix applied to all managed JetStream KV bucket names |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by a NATS JetStream KV bucket |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by a NATS JetStream KV bucket |

## Idempotency semantics

Both `IOutbox.EnqueueAsync` and `IInbox.MarkProcessedAsync` use `NatsKVStore.CreateAsync`:

- `CreateAsync` atomically creates a new KV entry for the message ID as the key.
- If the key already exists, NATS throws `NatsKVCreateException`, which is caught and swallowed — making repeated calls with the same message ID safe no-ops.

`HasProcessedAsync` uses `TryGetEntryAsync` — returns `true` if the key exists and has no error.

## Runtime capabilities

When `NatsDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.nats` | Always |
| `data.ledger-store` | Always |
| `data.outbox.nats` | `RegisterOutbox = true` |
| `data.inbox.nats` | `RegisterInbox = true` |

The provider capability sanitizes inline URI metadata. A URI such as
`nats://user:password@localhost:4222?token=secret` is reported as
`nats://localhost:4222` with `uriCredentialsConfigured = true` and
`secretProjection = redacted`.

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `nats-outbox` | `nats` |
| `inbox-stores` | `nats-inbox` | `nats` |

When an eventing runtime asks for `IEventDispatchStore`, the `nats-outbox` descriptor can now resolve to `consumer-managed` and the runtime can read/write durable dispatch state directly from the same KV bucket.

## Provider integration proof

`LiveDataProviderIntegrationTests.NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream` runs behind `ExternalProviderServiceFact(ExternalProviderServiceProvider.Nats)`. The lane is skipped unless `CEPHALON_PROVIDER_EXTERNAL_SERVICES` or `CEPHALON_PROVIDER_INTEGRATION` is enabled and `CEPHALON_PROVIDER_NATS_URI` is supplied. The test composes `Cephalon.Engine`, `Cephalon.Eventing`, and this pack, then proves real outbox/inbox writes, `IEventDispatchStore` pending/success transitions, runtime manifest capabilities, and `event-driven-integration` runtime surfaces against a live NATS service with JetStream KV enabled.

## Not shipped in this slice

This pack intentionally does not claim:

- NATS JetStream consumer or publish-subscribe primitives
- NATS Core (non-JetStream) messaging
- `IReadStore` / `IWriteStore` dispatch backed by NATS — query handlers should use `INatsConnection` directly
- transaction-scoped outbox staging spanning multiple NATS operations
- broker-owned retry scheduling or NATS-native delivery ownership beyond the shared `IEventDispatchStore` baseline
- NATS service API or micro-service primitives

These remain explicit later slices to keep the initial provider claim honest.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.Nats](event-sourcing-nats.md)
- [Cephalon.Data.Qdrant](data-qdrant.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
