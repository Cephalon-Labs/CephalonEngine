# Cephalon.EventSourcing.Qdrant

`Cephalon.EventSourcing.Qdrant` is the Qdrant vector-store event-store provider for Cephalon, implementing `IEventStore` against a Qdrant vector collection using payload-only indexing with 1-dimensional dummy vectors.

## What it owns

- implements `IEventStore` backed by a Qdrant vector collection
- stores each domain event as a point with a 1-dimensional dummy vector (`[0.0f]`) and all event metadata in payload fields — vector capabilities remain available for future semantic search scenarios
- derives compound point IDs from `{streamId}:{streamVersion}` via SHA-256 hash (first 16 bytes) for structural uniqueness
- enforces optimistic concurrency via a pre-append `GetVersionAsync` check followed by `UpsertAsync`
- creates the collection and payload indexes on `stream_id` (keyword) and `stream_version` (integer) on first use
- reads streams via `ScrollAsync` with payload filter on `stream_id` and `stream_version >= fromVersion`, sorted in memory by `stream_version`
- serializes event payloads with `System.Text.Json`
- reconstructs domain events by resolving `Type.GetType(AssemblyQualifiedName)` and deserializing via `JsonSerializer`

## Main surfaces

- `QdrantEventEntry.cs` — plain-object representation of a single event stored in a Qdrant point
- `QdrantEventStore.cs` — `IEventStore` implementation
- `Hosting/QdrantEventSourcingServiceCollectionExtensions.cs` — `AddCephalonQdrantEventSourcing` registration

## How it fits

`Cephalon.EventSourcing.Qdrant` plugs into the `IEventStore` contract owned by `Cephalon.EventSourcing`. Application code depends only on `IEventStore` — swapping providers requires only a registration change. The Qdrant client connects lazily on first operation, so DI resolution does not require a live Qdrant server.

## Registration

```csharp
services.AddCephalonQdrantEventSourcing(
    host: "localhost",
    port: 6334,
    collectionName: "event-streams");
```

## Point schema (event-streams collection)

| Payload field | Type | Notes |
|---------------|------|-------|
| `stream_id` | keyword | Stream identifier; indexed for efficient scroll filtering |
| `stream_version` | integer | Monotonically increasing version; indexed for range filtering |
| `event_type` | keyword | Assembly-qualified CLR type name |
| `payload` | keyword | JSON-serialized event body |
| `occurred_at_utc` | keyword | ISO 8601 UTC timestamp when the domain event occurred |
| `appended_at_utc` | keyword | ISO 8601 UTC timestamp when the event was appended |

Point ID is derived from `SHA-256({streamId}:{streamVersion})[0..16]` as a UUID.

## Optimistic concurrency

`AppendAsync` reads the current stream version before upserting events. If the actual version does not match `expectedVersion`, an `EventStreamConcurrencyException` is thrown before any points are upserted. Concurrent writers that both observe the same `expectedVersion` will both proceed to upsert — the compound point-ID hash provides a structural safeguard but full atomicity is not guaranteed. This is consistent with the ClickHouse and OpenSearch provider approaches.

## Not shipped in this slice

This provider intentionally does not claim:

- semantic similarity search over event payloads
- event projections or read-model maintenance
- snapshotting or stream archival
- multi-collection event streams
- Qdrant gRPC streaming for incremental replay

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Qdrant](data-qdrant.md)
- [Cephalon.EventSourcing.Nats](event-sourcing-nats.md)
- [Architecture](../architecture.md)
