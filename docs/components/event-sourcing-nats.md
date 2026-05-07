# Cephalon.EventSourcing.Nats

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.Nats` is the NATS JetStream KV event-store provider for Cephalon, implementing `IEventStore` against a NATS JetStream KV bucket using zero-padded keys for lexicographically safe ordering.

## What it owns

- implements `IEventStore` backed by a NATS JetStream KV bucket
- contributes a sanitized `IEventStoreContributor` entry projected through `IEventStoreCatalog` and the `event-sourcing` runtime surface
- stores each domain event as a KV entry with key format `{streamId}/{streamVersion:D20}` — zero-padded 20-digit version numbers ensure lexicographic ordering matches numeric ordering, enabling correct key-sorted replay without separate index queries
- enforces optimistic concurrency via a pre-append `GetVersionAsync` check and `CreateAsync` which throws `NatsKVCreateException` if the key already exists (caught and rethrown as `EventStreamConcurrencyException`)
- creates or updates the KV bucket on first operation via `CreateOrUpdateStoreAsync`
- reads streams by collecting all keys matching the `{streamId}/` prefix with parsed version >= `fromVersion`, sorting lexicographically (which equals numeric order due to zero-padding), and fetching each entry
- serializes event payloads through the shared Cephalon event-type registry
- reconstructs domain events through stable registry names, with descriptor aliases for legacy `AssemblyQualifiedName` rows
- `NatsConnection` does NOT connect on construction — connection is deferred to first use, so DI resolution does not require a live NATS server

## Main surfaces

- `NatsEventEntry.cs` — plain-object representation of a single event stored as a KV entry
- `NatsEventStore.cs` — `IEventStore` implementation
- `Hosting/NatsEventSourcingServiceCollectionExtensions.cs` — `AddCephalonNatsEventSourcing` registration
- `Services/NatsEventStoreContributor.cs` — sanitized runtime catalog contribution

## How it fits

`Cephalon.EventSourcing.Nats` plugs into the `IEventStore` contract owned by `Cephalon.EventSourcing`. Application code depends only on `IEventStore` — swapping providers requires only a registration change. NATS JetStream KV is a durable, ordered, replicated key-value store that fits the event-sourcing append-and-replay pattern cleanly.

## Registration

```csharp
services.AddCephalonNatsEventSourcing(
    url: "nats://localhost:4222",
    bucketName: "cephalon-events");
```

The method registers `IEventStore` and the shared event-type registry. The host still registers concrete event payloads through `AddCephalonEventType<TEvent>(...)` or `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`.

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

## Not shipped in this slice

This provider intentionally does not claim:

- NATS JetStream streams (non-KV) for event publishing
- event projections or read-model maintenance
- snapshotting or stream archival
- NATS consumer groups or competing consumers
- change-data-capture or NATS monitoring integration

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Nats](data-nats.md)
- [Cephalon.EventSourcing.Qdrant](event-sourcing-qdrant.md)
- [Architecture](../architecture.md)
