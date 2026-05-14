# Cephalon.EventSourcing.Nats

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.Nats` is the NATS JetStream KV event-store provider for Cephalon, implementing `IEventStore` against a NATS JetStream KV bucket using zero-padded keys for lexicographically safe ordering.

## What it owns

- implements `IEventStore` backed by a NATS JetStream KV bucket
- implements `ISnapshotStore` backed by the same NATS JetStream KV bucket for provider-durable latest snapshots
- contributes a sanitized `IEventStoreContributor` entry projected through `IEventStoreCatalog` and the `event-sourcing` runtime surface
- stores each domain event as a KV entry with key format `{streamId}/{streamVersion:D20}` — zero-padded 20-digit version numbers ensure lexicographic ordering matches numeric ordering, enabling correct key-sorted replay without separate index queries
- enforces optimistic concurrency via a pre-append `GetVersionAsync` check and `CreateAsync` which throws `NatsKVCreateException` if the key already exists (caught and rethrown as `EventStreamConcurrencyException`)
- stores latest snapshots under `snapshots/{streamId}/{stateTypeKey}`, where `stateTypeKey` is the base64url-encoded `{AssemblyName}:{FullTypeName}` state identity
- uses NATS KV entry revisions as the snapshot compare-and-set guard so stale snapshot rewinds are rejected
- creates or updates the KV bucket on first operation via `CreateOrUpdateStoreAsync`
- reads streams by collecting all keys matching the `{streamId}/` prefix with parsed version >= `fromVersion`, sorting lexicographically (which equals numeric order due to zero-padding), and fetching each entry
- serializes event payloads through the shared Cephalon event-type registry
- reconstructs domain events through stable registry names, with descriptor aliases for legacy `AssemblyQualifiedName` rows
- `NatsConnection` does NOT connect on construction — connection is deferred to first use, so DI resolution does not require a live NATS server

## Main surfaces

- `NatsEventEntry.cs` — plain-object representation of a single event stored as a KV entry
- `NatsEventSourcingConfiguration.cs` — shared key-format helpers for event-sourcing NATS persistence
- `NatsEventStore.cs` — `IEventStore` implementation
- `Hosting/NatsEventSourcingServiceCollectionExtensions.cs` — `AddCephalonNatsEventSourcing` registration
- `Services/NatsEventStoreContributor.cs` — sanitized runtime catalog contribution
- `Services/NatsSnapshotStore.cs` — provider-durable `ISnapshotStore` implementation

## How it fits

`Cephalon.EventSourcing.Nats` plugs into the `IEventStore` contract owned by `Cephalon.EventSourcing`. Application code depends only on `IEventStore` — swapping providers requires only a registration change. NATS JetStream KV is a durable, ordered, replicated key-value store that fits the event-sourcing append-and-replay pattern cleanly.

## Registration

```csharp
services.AddCephalonNatsEventSourcing(
    url: "nats://localhost:4222",
    bucketName: "cephalon-events");
```

The method registers `IEventStore`, `ISnapshotStore`, and the shared event-type registry. The host still registers concrete event payloads through `AddCephalonEventType<TEvent>(...)` or `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`.

## KV entry schema

| Field | Notes |
|-------|-------|
| Key | `{streamId}/{streamVersion:D20}` — zero-padded version for lexicographic ordering |
| Value | JSON-serialized `NatsEventEntry` |

`NatsEventEntry` payload fields:

| Field | Type | Notes |
|-------|------|-------|
| `StreamId` | string | Stream identifier |
| `StreamVersion` | long | Event version within the stream |
| `EventType` | string | Stable Cephalon event-type registry name |
| `Payload` | string | Serialized event body produced by the registered event-type descriptor |
| `OccurredAtUtc` | DateTime | UTC timestamp when the domain event occurred |
| `AppendedAtUtc` | DateTime | UTC timestamp when the event was appended |

## Optimistic concurrency

`AppendAsync` reads the current stream version before appending events. If the actual version does not match `expectedVersion`, an `EventStreamConcurrencyException` is thrown before any events are written. For each event, `CreateAsync` is used — if the key already exists (concurrent writer raced ahead), `NatsKVCreateException` is caught and rethrown as `EventStreamConcurrencyException` with the re-read actual version.

## Provider-durable snapshots

`NatsSnapshotStore` stores one latest snapshot per stream/state type in the same configured NATS JetStream KV bucket as the event stream:

| Field | Notes |
|-------|-------|
| Key | `snapshots/{streamId}/{stateTypeKey}` |
| `stateTypeKey` | Base64url-encoded `{AssemblyName}:{FullTypeName}` so CLR names stay within the NATS KV key character set |
| Value | JSON-serialized snapshot entry containing stream id, state type, stream version, serialized state payload, and save timestamp |

Saving a snapshot reads the current KV entry revision and uses `UpdateAsync(..., revision)` for compare-and-set updates. New snapshots use `CreateAsync(...)`, and older versions cannot replace a newer stored version. Runtime metadata reports:

- `snapshotStorage = NATS JetStream KV latest-snapshot`
- `snapshotBucketName = {bucketName}`
- `snapshotKeyPrefix = snapshots/`
- `snapshotLifecycle = provider-durable`
- `snapshotConcurrency = revision-compare-and-set`

The live provider integration lane proves append/read, snapshot save/load, snapshot-assisted managed replay, registered projection rebuild, final snapshot saveback, stale snapshot rejection, optimistic concurrency rejection, and provider descriptor readback against a real JetStream-enabled NATS runtime.

## Not shipped in this slice

This provider intentionally does not claim:

- NATS JetStream streams (non-KV) for event publishing
- provider-owned projection/read-model maintenance beyond the shared core replay worker invoking registered `IProjection<IDomainEvent>` services
- stream archival, retention, or compaction
- NATS consumer groups or competing consumers
- change-data-capture or NATS monitoring integration
- hosted background replay/projection runners

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Nats](data-nats.md)
- [Cephalon.EventSourcing.Qdrant](event-sourcing-qdrant.md)
- [Architecture](../architecture.md)
