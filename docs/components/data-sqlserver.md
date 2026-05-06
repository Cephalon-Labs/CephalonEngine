# Cephalon.Data.SqlServer

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.SqlServer` is the SQL Server provider-native CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC execution/runtime catalog family also fits a relational provider with durable LSN checkpoints, change-table polling, and module-preserving ownership truth without any SQL Server-specific registry in `Cephalon.Engine`.

## What it owns

- contributes configured provider-native SQL Server CDC captures through `SqlServerCdcCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "sqlserver"` and `mode = "change-table"`
- publishes capability metadata `data.sqlserver`, `data.relational-store`, and `data.cdc.sqlserver` introspectable at runtime through the manifest
- publishes the provider-native CDC execution graph `sqlserver-cdc-capture-flow`, hosted execution `sqlserver-cdc-capture-pump`, and execution runtime `sqlserver-cdc-capture-pump` when SQL Server CDC captures are configured
- runs a provider-native hosted service that polls configured SQL Server CDC change tables, stages outbox publications, persists SQL Server checkpoint tokens only after outbox stage success, and reports runtime posture through the shared `ICdcCaptureRuntimeReporter` surface
- stores durable SQL Server CDC checkpoints in a Cephalon-managed checkpoint table, defaulting to `dbo.cephalon_cdc_checkpoints`
- preserves authored capture ownership through `CdcCaptureDescriptor.SourceModuleId` while surfacing `metadata.contributorModuleId = "sqlserver-data"` when the provider pack contributes the descriptor on behalf of another module

## Main surfaces

- `Configuration/SqlServerDataOptions.cs`
- `Configuration/SqlServerCdcCaptureOptions.cs`
- `Modules/SqlServerDataModule.cs`
- `Registration/SqlServerDataEngineBuilderExtensions.cs`
- `Services/SqlServerCdcCaptureHostedService.cs`
- `Services/SqlServerCdcCheckpointToken.cs`
- `Services/SqlServerCdcExecutionRuntimeContributor.cs`
- `Services/SqlServerCdcTransport.cs`
- `Services/SqlServerDataRuntimeIds.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral CDC descriptor catalog, capture-side execution binding, shared runtime-state catalog, additive execution-runtime catalog, and optional external runtime reporting seam. `Cephalon.Data.SqlServer` adds the SQL Server-specific change-table reader, durable checkpoint store, and provider-native hosted execution loop needed to project truthful SQL Server CDC behavior into those shared surfaces.

That keeps the engine honest. The shared `data-cdc-capture-pump` still exists as the generic in-process runtime, but it simply ignores captures whose effective owner resolves to `sqlserver-cdc-capture-pump`. The SQL Server pack becomes the second concrete provider-native CDC runner after MongoDB, and the first relational runner on the same ownership/topology model.

The slice stays intentionally scoped. `Cephalon.Data.SqlServer` does not claim general-purpose `IReadStore` or `IWriteStore` dispatch, DbContext integration, SQL Server outbox storage, or SQL Server inbox storage. If a host wants relational read/write persistence today, `Cephalon.Data.EntityFramework` remains the honest baseline. `Cephalon.Data.SqlServer` only claims provider-native CDC capture plus durable checkpoint truth.

## Registration

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddSqlServerData(
    connectionString: "Server=localhost;Database=app;Integrated Security=true;TrustServerCertificate=true",
    databaseName: "app",
    configure: options =>
    {
        options.CdcCaptures.Add(new SqlServerCdcCaptureOptions
        {
            Id = "orders-cdc",
            DisplayName = "Orders CDC",
            SourceModuleId = "orders",
            CaptureInstance = "dbo_orders",
            TableSchema = "dbo",
            TableName = "orders",
            OutboxId = "tenant-event-outbox",
            ChannelId = "orders",
            MessageType = "OrdersChanged"
        });
    });
```

For configuration-driven hosts, prefer the options overload and bind from `Engine:Data:SqlServer`:

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddSqlServerData(options =>
{
    configuration.GetSection(SqlServerDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "SqlServer";
    options.DatabaseName = "app";
});
```

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=app;Integrated Security=true;TrustServerCertificate=true"
  },
  "Engine": {
    "Data": {
      "SqlServer": {
        "ConnectionStringName": "SqlServer",
        "DatabaseName": "app",
        "CheckpointTableSchema": "dbo",
        "CheckpointTableName": "cephalon_cdc_checkpoints",
        "CdcCaptures": [
          {
            "Id": "orders-cdc",
            "DisplayName": "Orders CDC",
            "SourceModuleId": "orders",
            "CaptureInstance": "dbo_orders",
            "TableSchema": "dbo",
            "TableName": "orders",
            "OutboxId": "tenant-event-outbox",
            "ChannelId": "orders",
            "MessageType": "OrdersChanged",
            "InitialPosition": "latest-available",
            "MaxChangesPerPoll": 128,
            "PollingIntervalSeconds": 5
          }
        ]
      }
    }
  }
}
```

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the pack fails fast during service resolution. If neither is set, the SQL Server provider cannot start.

## Configuration options (`Engine:Data:SqlServer`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionStringName` | `string?` | `null` | Root `ConnectionStrings` key to resolve for SQL Server |
| `ConnectionString` | `string?` | `null` | Inline SQL Server connection string |
| `DatabaseName` | `string` | required | Operator-facing database name for the configured CDC captures |
| `CheckpointTableSchema` | `string` | `"dbo"` | Schema that stores Cephalon-managed CDC checkpoints |
| `CheckpointTableName` | `string` | `"cephalon_cdc_checkpoints"` | Table that stores Cephalon-managed CDC checkpoints |
| `CdcCaptures` | `SqlServerCdcCaptureOptions[]` | `[]` | Contribute provider-native SQL Server CDC captures |

## SQL Server CDC capture options (`Engine:Data:SqlServer:CdcCaptures[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable CDC capture id |
| `DisplayName` | `string` | `Id` | Operator-facing capture name |
| `Description` | `string` | generated | Human-readable capture description |
| `SourceModuleId` | `string` | required | Module id that owns the capture surface |
| `SourceId` | `string` | `{provider}:{database}/{schema}.{table}` | Logical upstream source id when it should differ from the watched table path |
| `CaptureInstance` | `string` | required | SQL Server CDC capture-instance name |
| `TableSchema` | `string` | `"dbo"` | Schema name of the tracked table |
| `TableName` | `string` | required | Table name of the tracked table |
| `OutboxId` | `string` | required | Linked outbox id that receives publications |
| `ChannelId` | `string` | required | Logical outbox channel id for emitted publications |
| `MessageType` | `string` | required | Logical message type for emitted publications |
| `EventFormat` | `string` | `"sqlserver-cdc-change-table-event"` | Operator-facing event format projected on the descriptor |
| `InitialPosition` | `string` | `"latest-available"` | Initial position when no durable checkpoint exists yet; supported values are `latest-available` and `earliest-available` |
| `MaxChangesPerPoll` | `int` | `128` | Maximum number of captured changes to stage during one polling iteration |
| `PollingIntervalSeconds` | `int` | `5` | Polling interval in seconds for one provider-native iteration |
| `ResourceIds` | `string[]` | `["{database}.{schema}.{table}"]` | Explicit logical resources observed by the capture |
| `Tags` | `string[]` | `["cdc", "sqlserver", "provider-native"]` | Operator-facing tags on the descriptor |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing metadata merged onto the descriptor |

## Provider-native CDC runtime

When `CdcCaptures` are configured:

- each capture is published through `/engine/cdc-captures*` with `provider = "sqlserver"`, `mode = "change-table"`, and an `executionBinding` whose authored and requested runtime id is `sqlserver-cdc-capture-pump`
- the execution runtime is published through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` with `executionOwnership = host-managed`, `executionTopology = provider-native`, and `acknowledgementMode = provider-native`
- the same runtime publishes through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` under `sqlserver-cdc-capture-flow` plus `sqlserver-cdc-capture-pump`
- the hosted runner polls one SQL Server CDC change table per configured capture, stages one outbox message per captured row, and only persists the latest SQL checkpoint after the linked outbox accepted the batch
- each staged outbox message uses deterministic id `{cdcCaptureId}:{startLsn}-{sequenceValue}-{operation}`, content type `application/vnd.cephalon.sqlserver.cdc+json`, headers for `provider`, `cdcCaptureId`, `databaseName`, `schemaName`, `tableName`, `captureInstance`, and `operation`, plus metadata for `sourceId`, `eventFormat`, and `checkpointToken`
- the runtime-state surface keeps typed freshness, lag, pending-publication posture, checkpoint, change id, last operation type, checkpoint store, and failure-kind metadata on the same `/engine/cdc-captures/runtime*` catalog instead of inventing a SQL Server-specific monitor
- if a provider pack contributes a descriptor on behalf of another module, the authored `SourceModuleId` remains authoritative and `metadata.contributorModuleId` keeps the contributing pack explicit

### Checkpoint table schema (`cephalon_cdc_checkpoints`)

The default table name is `{CheckpointTableSchema}.{CheckpointTableName}`.

| Column | SQL type | Notes |
|--------|----------|-------|
| `CdcCaptureId` | `nvarchar(128)` | Stable CDC capture id; one checkpoint row per capture |
| `CheckpointStartLsn` | `varbinary(10)` | Latest durable `__$start_lsn` |
| `CheckpointSequenceValue` | `varbinary(10)` | Latest durable `__$seqval` |
| `CheckpointOperation` | `int` | Latest durable `__$operation` |
| `CheckpointToken` | `nvarchar(256)` | Serialized checkpoint token in `startLsn|seqval|operation` form |
| `UpdatedAtUtc` | `datetimeoffset(7)` | UTC timestamp of the last durable checkpoint write |

## Runtime capabilities

When `SqlServerDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.sqlserver` | Always |
| `data.relational-store` | Always |
| `data.cdc.sqlserver` | `CdcCaptures.Count > 0` |

## Live integration testing

The default repository test lane does not require a developer-managed SQL Server instance or Docker. SQL Server live CDC coverage lives in `tests/Cephalon.Tests.CdcIntegration` behind the shared external-service gate documented in that test project's README. The default run discovers `SqlServerCdc_StagesOutboxAndPersistsCheckpointAgainstLiveDatabase` but skips it until a developer or CI job opts into either `CEPHALON_CDC_SQLSERVER_CONNECTION_STRING` for a pre-provisioned service or `CEPHALON_CDC_TESTCONTAINERS=1` for a disposable Testcontainers-backed SQL Server service.

The live test creates an isolated database, enables database and table CDC for `dbo.orders`, inserts a real row, waits for the provider-native hosted runner to stage the linked outbox message, verifies runtime-state and execution-runtime aggregation, and asserts the Cephalon-managed SQL Server checkpoint table contains the same checkpoint token reported through the shared CDC state catalog.

## Not shipped in this slice

This pack intentionally still does not claim:

- `IReadStore` / `IWriteStore` dispatch backed directly by SQL Server
- Entity Framework or `DbContext` integration
- SQL Server-backed `IOutbox`, `IInbox`, or event-dispatch storage
- SQL Agent, Debezium, or out-of-process SQL Server CDC execution ownership
- net-new control plane for CDC monitoring outside the shared `/engine/cdc-*` surfaces
- low-code generation or multi-table orchestration beyond one configured capture per SQL Server CDC capture instance

These remain later slices so the current provider claim stays truthful.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
