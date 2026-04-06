# Cephalon.Data.MongoDB

`Cephalon.Data.MongoDB` is the MongoDB document-store companion pack for Cephalon, following the same companion-pack pattern as `Cephalon.Data.EntityFramework`.

## What it owns

- registers a singleton `IMongoClient` and `IMongoDatabase` from a connection string and database name
- implements `IReadStore` and `IWriteStore` for command/query handler dispatch against MongoDB
- exposes an opt-in MongoDB-backed `IOutbox` that persists staged messages in the `outbox_messages` collection with idempotent staging via a unique index on `MessageId`
- exposes an opt-in MongoDB-backed `IInbox` that tracks processed inbound messages in the `inbox_receipts` collection
- publishes capability metadata (`data.mongodb`, `data.document-store`, `data.outbox.mongodb`) introspectable at runtime
- publishes an operator-facing outbox descriptor through `/engine/outboxes` and `/engine/snapshot` when `event-driven-integration` is active
- projects that outbox through the `event-driven-integration` technology surface as `outbox-producers` with `provider: "mongodb"`

## Main surfaces

- `Configuration/MongoDbDataOptions.cs`
- `Modules/MongoDbDataModule.cs`
- `Registration/MongoDbDataEngineBuilderExtensions.cs`
- `Services/MongoDbOutbox.cs`
- `Services/MongoDbInbox.cs`
- `Services/MongoDbOutboxRuntimeContributor.cs`
- `Services/MongoDbInboxRuntimeSurfaceContributor.cs`

## Registration

```csharp
engine.AddMongoDbData(
    connectionString: "mongodb://localhost:27017",
    databaseName: "myapp");
```

## Outbox collection schema (`outbox_messages`)

| Field | Type | Notes |
|-------|------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `MessageId` | string | Unique idempotency key (GUID) |
| `EventType` | string | Fully qualified event type name |
| `Payload` | string (JSON) | Serialized event body |
| `CorrelationId` | string? | Optional causality tracking |
| `TenantId` | string? | Optional multi-tenancy |
| `CreatedAtUtc` | DateTime | Staged timestamp |
| `DispatchedAtUtc` | DateTime? | Null until dispatched |
| `DispatchAttemptCount` | int | Retry tracking |
| `NextAttemptAtUtc` | DateTime? | Delayed retry support |

A unique index on `MessageId` prevents duplicate staging (idempotent).

## Configuration

Settings bind from `Engine:Data:MongoDB`:

```json
{
  "Engine": {
    "Data": {
      "MongoDB": {
        "ConnectionString": "mongodb://localhost:27017",
        "DatabaseName": "myapp",
        "CollectionPrefix": ""
      }
    }
  }
}
```

## Status

✅ Shipped — Phase 10 (ENG-054) · companion-pack pattern proven for document-oriented stores
