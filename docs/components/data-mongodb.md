# Cephalon.Data.MongoDB

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.MongoDB` is the MongoDB document-store companion pack for Cephalon. It proves that the companion-pack pattern established by `Cephalon.Data.EntityFramework` extends cleanly to non-relational providers without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`, and it now also proves the first concrete provider-native CDC runner on top of the shared `Cephalon.Data` execution/runtime catalog family.

## What it owns

- registers a singleton `IMongoClient` from a connection string and a singleton `IMongoDatabase` from the configured database name, using `TryAdd` semantics so a host-owned client is never displaced
- registers a scoped `IOutbox` backed by the `outbox_messages` collection when `RegisterOutbox` is enabled; the collection name honors the optional `CollectionPrefix`
- registers a scoped `IEventDispatchStore` over that same outbox collection when `RegisterOutbox` is enabled, so staged MongoDB events can be read and durable dispatch outcomes can be written back through the runtime-neutral eventing contract
- registers a scoped `IInbox` backed by the `inbox_receipts` collection when `RegisterInbox` is enabled; the collection name honors the optional `CollectionPrefix`
- ensures that outbox staging is idempotent by maintaining a unique index on `MessageId` and swallowing the MongoDB duplicate-key error (code 11000) on a repeated `StageAsync` call for the same message
- contributes configured provider-native MongoDB change-stream captures through `MongoDbChangeStreamCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "mongodb"` and `mode = "change-stream"`
- publishes capability metadata `data.mongodb`, `data.document-store`, and optionally `data.outbox.mongodb`, `data.inbox.mongodb`, and `data.cdc.mongodb` introspectable at runtime through the manifest
- projects the outbox descriptor through the `event-driven-integration` technology surface as `outbox-producers` with `provider: "mongodb"` and `mode: "document-collection"` when that technology is active
- projects the inbox descriptor through the same technology surface as `inbox-stores` when that technology is active
- publishes the provider-native CDC execution graph `mongodb-change-stream-capture-flow`, hosted execution `mongodb-change-stream-capture-pump`, and execution runtime `mongodb-change-stream-capture-pump` when change-stream captures are configured
- runs a provider-native hosted service that watches configured collections, stages outbox publications, persists resume-token checkpoints only after stage success, and reports runtime posture through the shared `ICdcCaptureRuntimeReporter` surface
- stores durable change-stream resume tokens in the `{CollectionPrefix}cdc_change_stream_checkpoints` collection
- preserves authored capture ownership through `CdcCaptureDescriptor.SourceModuleId` while surfacing `metadata.contributorModuleId = "mongodb-data"` when the provider pack contributes the descriptor on behalf of another module

## Main surfaces

- `Configuration/MongoDbDataOptions.cs`
- `Configuration/MongoDbChangeStreamCaptureOptions.cs`
- `Modules/MongoDbDataModule.cs`
- `Registration/MongoDbDataEngineBuilderExtensions.cs`
- `Services/MongoDbChangeStreamCaptureHostedService.cs`
- `Services/MongoDbChangeStreamCheckpointEntry.cs`
- `Services/MongoDbChangeStreamExecutionRuntimeContributor.cs`
- `Services/MongoDbDataRuntimeIds.cs`
- `Services/MongoDbOutboxEntry.cs`
- `Services/MongoDbOutbox.cs`
- `Services/MongoDbEventDispatchStore.cs`
- `Services/MongoDbOutboxRuntimeSurfaceContributor.cs`
- `Services/MongoDbInboxEntry.cs`
- `Services/MongoDbInbox.cs`
- `Services/MongoDbInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface, the shared CDC runtime-state catalog, capture-side execution binding, and the additive execution-runtime contract. `Cephalon.Data.MongoDB` adds MongoDB-backed outbox and inbox persistence so event-driven workloads can stage and track messages without switching to a relational store, and it also exposes the same staged outbox through `IEventDispatchStore` so consumer-managed or adapter-owned dispatch loops can read pending items and persist durable dispatch outcomes without MongoDB-specific host glue.

The same pack now also proves the first provider-native CDC runner on top of those shared contracts. Instead of inventing a MongoDB-only registry, the pack contributes normal `CdcCaptureDescriptor` entries, binds them to the provider-native execution runtime `mongodb-change-stream-capture-pump`, stages captured changes through the linked `IOutbox`, persists resume-token checkpoints only after stage success, and reports freshness, lag, publication posture, checkpoints, and failures back through the shared `/engine/cdc-captures/runtime*` surface. The shared `data-cdc-capture-pump` remains additive and simply ignores captures whose effective owner resolves to the MongoDB runtime.

The slice stays intentionally honest: it proves the companion-pack pattern for document-oriented stores, ships an idempotent outbox and inbox, and now also ships one collection-scoped provider-native CDC path. `IReadStore` and `IWriteStore` are still not backed directly by MongoDB in this slice, and broader out-of-process CDC reporting or higher-level multi-capture orchestration still remain later work.

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
        options.CollectionPrefix = "app_";  // optional - prefix all Cephalon collections
    });
```

To enable the provider-native MongoDB CDC path, pair the pack with `AddData()` so the shared CDC catalogs and reporters exist:

```csharp
engine.AddData();
engine.AddMongoDbData(options =>
{
    configuration.GetSection(MongoDbDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "MongoDB";
    options.DatabaseName = "myapp";
    options.RegisterOutbox = true;
    options.CollectionPrefix = "app_";

    options.ChangeStreamCaptures.Add(new MongoDbChangeStreamCaptureOptions
    {
        Id = "catalog-items-cdc",
        DisplayName = "Catalog Items CDC",
        SourceModuleId = "catalog",
        CollectionName = "catalog_items",
        OutboxId = "mongodb-outbox",
        ChannelId = "catalog.items",
        MessageType = "CatalogItemChanged"
    });
});
```

If the same host also needs the shared `Cephalon.Data` in-process CDC pump for other non-MongoDB captures, enable it separately through `AddData(options => options.EnableCdcExecution = true)`. The shared pump still skips any capture whose effective owner is `mongodb-change-stream-capture-pump`.

For configuration-driven hosts, prefer the options overload and let the pack resolve either `ConnectionStringName` from the root `ConnectionStrings` section or `ConnectionString` directly:

```csharp
engine.AddData();
engine.AddMongoDbData(options =>
{
    configuration.GetSection(MongoDbDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "MongoDB";
    options.DatabaseName = "myapp";
});
```

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/?replicaSet=rs0"
  },
  "Engine": {
    "Data": {
      "MongoDB": {
        "ConnectionStringName": "MongoDB",
        "DatabaseName": "myapp",
        "CollectionPrefix": "app_",
        "RegisterOutbox": true,
        "ChangeStreamCaptures": [
          {
            "Id": "catalog-items-cdc",
            "DisplayName": "Catalog Items CDC",
            "SourceModuleId": "catalog",
            "CollectionName": "catalog_items",
            "OutboxId": "mongodb-outbox",
            "ChannelId": "catalog.items",
            "MessageType": "CatalogItemChanged",
            "FullDocumentMode": "update-lookup"
          }
        ]
      }
    }
  }
}
```

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the pack fails fast during service resolution. If neither is set, MongoDB falls back to `mongodb://localhost:27017`.

## Configuration options (`Engine:Data:MongoDB`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionStringName` | `string?` | `null` | Root `ConnectionStrings` key to resolve for MongoDB |
| `ConnectionString` | `string?` | `null` | Inline MongoDB connection string |
| `DatabaseName` | `string` | `"cephalon"` | Target database name |
| `CollectionPrefix` | `string` | `""` | Optional prefix for all Cephalon-managed collections |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by the `outbox_messages` collection |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by the `inbox_receipts` collection |
| `ChangeStreamCaptures` | `MongoDbChangeStreamCaptureOptions[]` | `[]` | Contribute provider-native MongoDB change-stream CDC captures |

## Change-stream capture options (`Engine:Data:MongoDB:ChangeStreamCaptures[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable CDC capture id |
| `DisplayName` | `string` | `Id` | Operator-facing capture name |
| `Description` | `string` | generated | Human-readable capture description |
| `SourceModuleId` | `string` | required | Module id that owns the capture surface |
| `SourceId` | `string` | `{provider}:{database}/{collection}` | Logical upstream source id when it should differ from the watched collection path |
| `DatabaseName` | `string?` | pack-level `DatabaseName` | Database name to watch for this capture |
| `CollectionName` | `string` | required | MongoDB collection to watch |
| `OutboxId` | `string` | `"mongodb-outbox"` | Linked outbox id that receives publications |
| `ChannelId` | `string` | required | Logical outbox channel id for emitted publications |
| `MessageType` | `string` | required | Logical message type for emitted publications |
| `EventFormat` | `string` | `"mongodb-change-stream-event"` | Operator-facing event format projected on the descriptor |
| `FullDocumentMode` | `string` | `"update-lookup"` | MongoDB full-document mode: `default`, `update-lookup`, `when-available`, or `required` |
| `MaxAwaitTimeSeconds` | `int` | `5` | Maximum await time for one change-stream batch |
| `BatchSize` | `int?` | `null` | Optional batch-size hint for one change-stream batch |
| `ResourceIds` | `string[]` | `["{database}.{collection}"]` | Explicit logical resources observed by the capture |
| `Tags` | `string[]` | `["cdc", "mongodb", "provider-native"]` | Operator-facing tags on the descriptor |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing metadata merged onto the descriptor |

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

**Idempotency**: A unique index on `MessageId` is created on first use. Calling `StageAsync` with a `MessageId` that already exists silently returns: the duplicate-key exception (error code 11000) is caught and swallowed.

## Inbox collection schema (`inbox_receipts`)

The collection name is `{CollectionPrefix}inbox_receipts`.

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `MessageId` | string | Processed message id; unique index enforces exactly-once semantics |
| `ProcessedAtUtc` | DateTime | UTC timestamp when the message was first processed |

**Idempotency**: A unique index on `MessageId` is created on first use. `MarkProcessedAsync` swallows the duplicate-key error: calling it twice for the same id is safe.

## Provider-native CDC runtime

When `ChangeStreamCaptures` are configured:

- each capture is published through `/engine/cdc-captures*` with `provider = "mongodb"`, `mode = "change-stream"`, and an `executionBinding` whose authored and requested runtime id is `mongodb-change-stream-capture-pump`
- the execution runtime is published through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` with `executionOwnership = host-managed`, `executionTopology = provider-native`, and `acknowledgementMode = provider-native`
- the same runtime publishes through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` under `mongodb-change-stream-capture-flow` plus `mongodb-change-stream-capture-pump`
- the hosted runner watches one collection per configured capture, reports `Started` only after the provider-native change-stream cursor opens (`cursorState = "opened"`), serializes the raw MongoDB `BackingDocument` as relaxed Extended JSON, stages one outbox message per change, and only persists the latest resume token after the linked outbox accepted the batch
- each staged outbox message uses deterministic id `{cdcCaptureId}:{resumeTokenHash}`, content type `application/vnd.cephalon.mongodb.change-stream+json`, headers for `provider`, `cdcCaptureId`, `databaseName`, `collectionName`, and `operationType`, plus metadata for `sourceId`, `eventFormat`, and `resumeTokenHash`
- the runtime-state surface keeps typed freshness, current lag, pending-publication posture, checkpoint, change id, last operation type, checkpoint collection, and failure-kind metadata on the same `/engine/cdc-captures/runtime*` catalog instead of inventing a MongoDB-only monitor
- if a provider pack contributes a descriptor on behalf of another module, the authored `SourceModuleId` remains authoritative and `metadata.contributorModuleId` keeps the contributing pack explicit

The dedicated CDC integration lane under `tests/Cephalon.Tests.CdcIntegration` now proves this MongoDB change-stream path against a real disposable replica set using `EphemeralMongo7`. That lane validates outbox staging, provider-native runtime binding, runtime-state reporting, execution-runtime aggregation, and checkpoint persistence without requiring Docker or a developer-managed MongoDB instance.

The dedicated provider integration lane under `tests/Cephalon.Tests.ProviderIntegration` also proves the non-CDC MongoDB data-provider runtime path through `MongoDbProviderIntegrationTests.MongoDbProvider_StagesOutboxInboxAndDispatchAgainstDisposableReplicaSet`. The test starts a disposable single-node replica set, composes `Cephalon.Engine`, `Cephalon.Data.MongoDB`, and `Cephalon.Eventing`, and validates manifest capabilities, outbox and inbox descriptors, `event-driven-integration` runtime surfaces, idempotent outbox/inbox writes, dispatch-store success reporting, and durable dispatched/processed document state without requiring Docker or a developer-managed MongoDB instance.

### Checkpoint collection schema (`cdc_change_stream_checkpoints`)

The collection name is `{CollectionPrefix}cdc_change_stream_checkpoints`.

| Field | BSON type | Notes |
|-------|-----------|-------|
| `_id` | ObjectId | Auto-generated surrogate key |
| `CdcCaptureId` | string | Stable CDC capture id; one checkpoint record per capture |
| `ResumeTokenJson` | string | Relaxed Extended JSON copy of the latest durable resume token |
| `ChangeId` | string | SHA-256 hash derived from the persisted resume token |
| `UpdatedAtUtc` | DateTime | UTC timestamp of the last durable checkpoint write |

## Runtime capabilities

When `MongoDbDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.mongodb` | Always |
| `data.document-store` | Always |
| `data.outbox.mongodb` | `RegisterOutbox = true` |
| `data.inbox.mongodb` | `RegisterInbox = true` |
| `data.cdc.mongodb` | `ChangeStreamCaptures.Count > 0` |

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `mongodb-outbox` | `mongodb` |
| `inbox-stores` | `mongodb-inbox` | `mongodb` |

When provider-native change-stream captures are configured, the following additional runtime entries appear:

| Surface | Entry id | Notes |
|---------|----------|-------|
| `/engine/cdc-capture-runtimes` | `mongodb-change-stream-capture-pump` | Provider-native execution-runtime descriptor and aggregate summary |
| `/engine/hosted-executions` | `mongodb-change-stream-capture-pump` | Background-service execution entry |
| `/engine/execution-graphs` | `mongodb-change-stream-capture-flow` | Provider-native change-stream execution flow |

## Not shipped in this slice

This pack intentionally still does not claim:

- `IReadStore` / `IWriteStore` dispatch backed by MongoDB: query and command handlers should use `IMongoDatabase` directly
- transaction-scoped outbox staging (no MongoDB multi-document transaction is opened; each `StageAsync` is a single `InsertOneAsync`)
- pack-owned dispatch loops or broker-specific retry scheduling beyond the runtime-neutral `IEventDispatchStore` bridge
- projection rebuild orchestration
- out-of-process or externally reported MongoDB change-stream runtime observation beyond the in-process hosted service
- broader multi-collection orchestration or low-code capture generation beyond the current one-capture-per-configured-collection path

These remain explicit later slices so the current provider claim stays truthful.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
