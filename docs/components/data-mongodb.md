# Cephalon.Data.MongoDB

`Cephalon.Data.MongoDB` is the MongoDB document-store companion pack for Cephalon, proving that the companion-pack pattern established by `Cephalon.Data.EntityFramework` extends cleanly to non-relational providers without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `IMongoClient` from a connection string and a singleton `IMongoDatabase` from the configured database name, using `TryAdd` semantics so a host-owned client is never displaced
- registers a scoped `IOutbox` backed by the `outbox_messages` collection when `RegisterOutbox` is enabled; the collection name honours the optional `CollectionPrefix`
- registers a scoped `IInbox` backed by the `inbox_receipts` collection when `RegisterInbox` is enabled; the collection name honours the optional `CollectionPrefix`
- ensures that outbox staging is idempotent by maintaining a unique index on `MessageId` and swallowing the MongoDB duplicate-key error (code 11000) on a repeated `StageAsync` call for the same message
- exposes operator-facing outbox and inbox descriptors through `/engine/outboxes`, `/engine/inboxes`, and `/engine/snapshot` when the respective path is enabled
- projects the outbox descriptor through the `event-driven-integration` technology surface as `outbox-producers` with `provider: "mongodb"` and `mode: "document-collection"` when that technology is active
- projects the inbox descriptor through the same technology surface as `inbox-stores` when the technology is active
- publishes capability metadata `data.mongodb`, `data.document-store`, and optionally `data.outbox.mongodb` and `data.inbox.mongodb` introspectable at runtime through the manifest

## Main surfaces

- `Configuration/MongoDbDataOptions.cs`
- `Modules/MongoDbDataModule.cs`
- `Registration/MongoDbDataEngineBuilderExtensions.cs`
- `Services/MongoDbOutboxEntry.cs`
- `Services/MongoDbOutbox.cs`
- `Services/MongoDbOutboxRuntimeSurfaceContributor.cs`
- `Services/MongoDbInboxEntry.cs`
- `Services/MongoDbInbox.cs`
- `Services/MongoDbInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.MongoDB` adds the MongoDB-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages without switching to a relational store.

The slice is intentionally narrow and honest: it proves the companion-pack pattern works for document-oriented stores, ships an idempotent outbox and an idempotency-guarded inbox, and exposes the same runtime introspection surfaces as the Entity Framework provider. `IReadStore` and `IWriteStore` are not backed directly by MongoDB in this slice — query and command handlers should depend on MongoDB directly through `IMongoDatabase` or typed collection injection. Full CQRS query/command dispatch on top of MongoDB collections remains a later slice.

## Registration

```csharp
engine.AddMongoDbData(
    connectionString: "mongodb://localhost:27017",
    databaseName: "myapp");
```

To enable the outbox and inbox paths:

```csharp
engine.AddMongoDbData(
    connectionString: "mongodb://localhost:27017",
    databaseName: "myapp",
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.CollectionPrefix = "app_";  // optional — prefix all Cephalon collections
    });
```

## Configuration options (`Engine:Data:MongoDB`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | `string` | `"mongodb://localhost:27017"` | MongoDB connection string |
| `DatabaseName` | `string` | `"cephalon"` | Target database name |
| `CollectionPrefix` | `string` | `""` | Optional prefix for all Cephalon-managed collections |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by the `outbox_messages` collection |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by the `inbox_receipts` collection |

## Outbox collection schema (`outbox_messages`)

The collection name is `{CollectionPrefix}outbox_messages`.

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `MessageId` | string | Unique idempotency key (GUID); unique index prevents duplicate staging |
| `EventType` | string | Fully qualified CLR event type name |
| `Payload` | string | `System.Text.Json`-serialized event body |
| `CorrelationId` | string? | Optional causality tracking, propagated from `IBehaviorContext.CorrelationId` |
| `TenantId` | string? | Optional multi-tenancy discriminator |
| `CreatedAtUtc` | DateTime | UTC timestamp when the message was staged |
| `DispatchedAtUtc` | DateTime? | `null` until the message is marked dispatched |
| `DispatchAttemptCount` | int | Incremented on each dispatch attempt; starts at 0 |
| `NextAttemptAtUtc` | DateTime? | Populated by a dispatch adapter for delayed-retry intent |

**Idempotency**: A unique index on `MessageId` is created on first use. Calling `StageAsync` with a `MessageId` that already exists silently returns — the duplicate-key exception (error code 11000) is caught and swallowed.

## Inbox collection schema (`inbox_receipts`)

The collection name is `{CollectionPrefix}inbox_receipts`.

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `MessageId` | string | Processed message id; unique index enforces exactly-once semantics |
| `ProcessedAtUtc` | DateTime | UTC timestamp when the message was first processed |

**Idempotency**: A unique index on `MessageId` is created on first use. `MarkProcessedAsync` swallows the duplicate-key error — calling it twice for the same id is safe.

## Runtime capabilities

When `MongoDbDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.mongodb` | Always |
| `data.document-store` | Always |
| `data.outbox.mongodb` | `RegisterOutbox = true` |
| `data.inbox.mongodb` | `RegisterInbox = true` |

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `mongodb-outbox` | `mongodb` |
| `inbox-stores` | `mongodb-inbox` | `mongodb` |

## Not shipped in this slice

This pack intentionally does not claim:

- `IReadStore` / `IWriteStore` dispatch backed by MongoDB — query and command handlers should use `IMongoDatabase` directly
- transaction-scoped outbox staging (no MongoDB multi-document transaction is opened; each `StageAsync` is a single `InsertOneAsync`)
- adapter-owned dispatch loops or broker retry scheduling
- projection rebuild orchestration
- change-stream subscription support

These remain explicit later slices to keep the initial provider claim honest.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
