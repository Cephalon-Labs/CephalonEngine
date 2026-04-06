# Cephalon.EventSourcing.MongoDB

`Cephalon.EventSourcing.MongoDB` is the MongoDB event-store provider for Cephalon, following the same companion-pack pattern as `Cephalon.EventSourcing.EntityFramework`.

## What it owns

- implements `IEventStore` backed by MongoDB with per-stream monotonic versioning
- uses an `event_streams` collection with a compound unique index on `(StreamId, StreamVersion)` for optimistic concurrency
- throws `EventStreamConcurrencyException` when the expected stream version does not match the current stored version
- provides `MongoDbEventSourcingConfiguration` to initialize collection indexes at startup
- registers via `AddCephalonMongoDbEventSourcing()` on `IServiceCollection`

## Main surfaces

- `MongoDbEventEntry.cs`
- `MongoDbEventStore.cs`
- `MongoDbEventSourcingConfiguration.cs`
- `Hosting/MongoDbEventSourcingServiceCollectionExtensions.cs`

## Registration

```csharp
services.AddCephalonMongoDbEventSourcing(
    connectionString: "mongodb://localhost:27017",
    databaseName: "myapp");
```

## Event stream collection schema (`event_streams`)

| Field | Type | Notes |
|-------|------|-------|
| `_id` | ObjectId | Surrogate PK |
| `StreamId` | string | Logical stream identifier |
| `StreamVersion` | long | Per-stream monotonic version (0-based) |
| `EventType` | string | Fully qualified event type |
| `Payload` | string (JSON) | Serialized event body |
| `OccurredAtUtc` | DateTime | From `IDomainEvent.OccurredAtUtc` |
| `AppendedAtUtc` | DateTime | Server-side insert timestamp |
| `CorrelationId` | string? | Optional causality tracking |
| `TenantId` | string? | Optional multi-tenancy |

## Concurrency model

Optimistic concurrency is enforced via a compound unique index on `(StreamId, StreamVersion)`. Before appending, `MongoDbEventStore` reads the current `MAX(StreamVersion)` for the stream and compares against the caller's `expectedVersion`. A mismatch throws `EventStreamConcurrencyException` before the insert is attempted.

## Status

✅ Shipped — Phase 10 (ENG-054) · MongoDB event-store provider for `IEventStore`
