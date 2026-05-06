# Cephalon.Data.Postgres

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.Postgres` is the PostgreSQL provider-native CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC execution/runtime catalog family also fits logical-replication streaming with slot-backed durable progress, publication/table ownership validation, and module-preserving capture ownership truth without a PostgreSQL-specific registry in `Cephalon.Engine`.

## What it owns

- contributes configured provider-native PostgreSQL logical-replication captures through `PostgresLogicalReplicationCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "postgresql"` and `mode = "logical-replication"`
- publishes capability metadata `data.postgresql`, `data.relational-store`, and `data.cdc.postgresql` introspectable at runtime through the manifest
- publishes the provider-native CDC execution graph `postgresql-logical-replication-capture-flow`, hosted execution `postgresql-logical-replication-capture-pump`, and execution runtime `postgresql-logical-replication-capture-pump` when PostgreSQL CDC captures are configured
- runs a provider-native hosted service that validates publication/table ownership, classifies logical-replication slot lifecycle posture, optionally recreates inactive invalidated slots when configured, streams one bounded logical-replication batch per configured capture, stages outbox publications, confirms replication-slot progress only after outbox stage success, and reports runtime posture through the shared `ICdcCaptureRuntimeReporter` surface
- keeps durable PostgreSQL CDC checkpoints on the logical replication slot itself, using confirmed flush position as the provider-native acknowledgement boundary instead of a Cephalon-managed checkpoint table
- preserves authored capture ownership through `CdcCaptureDescriptor.SourceModuleId` while surfacing `metadata.contributorModuleId = "postgres-data"` when the provider pack contributes the descriptor on behalf of another module

## Main surfaces

- `Configuration/PostgresDataOptions.cs`
- `Configuration/PostgresLogicalReplicationCaptureOptions.cs`
- `Modules/PostgresDataModule.cs`
- `Registration/PostgresDataEngineBuilderExtensions.cs`
- `Services/IPostgresLogicalReplicationTransport.cs`
- `Services/PostgresLogicalReplicationCaptureHostedService.cs`
- `Services/PostgresLogicalReplicationCheckpointToken.cs`
- `Services/PostgresLogicalReplicationExecutionRuntimeContributor.cs`
- `Services/PostgresLogicalReplicationTransport.cs`
- `Services/PostgresDataRuntimeIds.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral CDC descriptor catalog, capture-side execution binding, shared runtime-state catalog, additive execution-runtime catalog, external runtime reporting seam, and the shared `/engine/cdc-*` plus `snapshot` truth model. `Cephalon.Data.Postgres` adds the PostgreSQL-specific logical-replication reader, slot/publication validation, bounded transaction batching, and provider-native hosted execution loop needed to project truthful PostgreSQL behavior into those shared surfaces.

That keeps the engine honest. The shared `data-cdc-capture-pump` still exists as the generic in-process runtime, but it simply ignores captures whose effective owner resolves to `postgresql-logical-replication-capture-pump`. The PostgreSQL pack becomes the third concrete provider-native CDC runner after MongoDB and SQL Server, and the first slot-backed logical-streaming relational runner on the same ownership/topology model.

The slice stays intentionally scoped. `Cephalon.Data.Postgres` does not claim general-purpose `IReadStore` or `IWriteStore` dispatch, EF Core integration, PostgreSQL-backed outbox or inbox storage, publication management beyond validating one declared publication/table mapping, or multi-table orchestration beyond one configured capture per publication/table/slot path. If a host wants relational read/write persistence today, `Cephalon.Data.EntityFramework` remains the honest baseline. `Cephalon.Data.Postgres` only claims provider-native CDC capture plus durable slot-backed progress truth.

## Registration

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddPostgresData(
    connectionString: "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres",
    databaseName: "app",
    configure: options =>
    {
        options.CdcCaptures.Add(new PostgresLogicalReplicationCaptureOptions
        {
            Id = "orders-cdc",
            DisplayName = "Orders CDC",
            SourceModuleId = "orders",
            PublicationName = "cephalon_orders_publication",
            SlotName = "cephalon_orders_slot",
            TableSchema = "public",
            TableName = "orders",
            OutboxId = "tenant-event-outbox",
            ChannelId = "orders",
            MessageType = "OrdersChanged"
        });
    });
```

For configuration-driven hosts, prefer the options overload and bind from `Engine:Data:Postgres`:

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddPostgresData(options =>
{
    configuration.GetSection(PostgresDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "Postgres";
    options.DatabaseName = "app";
});
```

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres"
  },
  "Engine": {
    "Data": {
      "Postgres": {
        "ConnectionStringName": "Postgres",
        "DatabaseName": "app",
        "CdcCaptures": [
          {
            "Id": "orders-cdc",
            "DisplayName": "Orders CDC",
            "SourceModuleId": "orders",
            "PublicationName": "cephalon_orders_publication",
            "SlotName": "cephalon_orders_slot",
            "TableSchema": "public",
            "TableName": "orders",
            "OutboxId": "tenant-event-outbox",
            "ChannelId": "orders",
            "MessageType": "OrdersChanged",
            "InitialPosition": "slot-consistent-point",
            "CreateSlotIfMissing": true,
            "RecreateSlotIfInvalidated": false,
            "MaxChangesPerRead": 128,
            "MaxAwaitTimeSeconds": 5,
            "PollingIntervalSeconds": 5
          }
        ]
      }
    }
  }
}
```

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the pack fails fast during service resolution. If neither is set, the PostgreSQL provider cannot start.

## Configuration options (`Engine:Data:Postgres`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionStringName` | `string?` | `null` | Root `ConnectionStrings` key to resolve for PostgreSQL |
| `ConnectionString` | `string?` | `null` | Inline PostgreSQL connection string |
| `DatabaseName` | `string` | required | Operator-facing database name for the configured logical-replication captures |
| `CdcCaptures` | `PostgresLogicalReplicationCaptureOptions[]` | `[]` | Contribute provider-native PostgreSQL logical-replication captures |

## PostgreSQL capture options (`Engine:Data:Postgres:CdcCaptures[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable CDC capture id |
| `DisplayName` | `string` | `Id` | Operator-facing capture name |
| `Description` | `string` | generated | Human-readable capture description |
| `SourceModuleId` | `string` | required | Module id that owns the capture surface |
| `SourceId` | `string` | `{provider}:{database}/{schema}.{table}` | Logical upstream source id when it should differ from the watched table path |
| `PublicationName` | `string` | required | PostgreSQL publication that must publish the tracked table |
| `SlotName` | `string` | required | PostgreSQL logical replication slot used for durable progress |
| `TableSchema` | `string` | `"public"` | Schema name of the tracked table |
| `TableName` | `string` | required | Table name of the tracked table |
| `OutboxId` | `string` | required | Linked outbox id that receives publications |
| `ChannelId` | `string` | required | Logical outbox channel id for emitted publications |
| `MessageType` | `string` | required | Logical message type for emitted publications |
| `EventFormat` | `string` | `"postgresql-logical-replication-event"` | Operator-facing event format projected on the descriptor |
| `InitialPosition` | `string` | `"slot-consistent-point"` | Initial position when the slot must be created; supported values are `slot-consistent-point` and `latest-available` |
| `CreateSlotIfMissing` | `bool` | `true` | Creates the logical replication slot when it does not exist yet |
| `RecreateSlotIfInvalidated` | `bool` | `false` | Drops and recreates the logical replication slot when PostgreSQL reports that the existing slot is invalidated and inactive |
| `MaxChangesPerRead` | `int` | `128` | Maximum number of captured changes to stage during one provider-native iteration |
| `MaxAwaitTimeSeconds` | `int` | `5` | Maximum number of seconds to await committed WAL messages during one provider-native iteration |
| `PollingIntervalSeconds` | `int` | `5` | Polling interval in seconds for one provider-native loop iteration |
| `ResourceIds` | `string[]` | `["{database}.{schema}.{table}"]` | Explicit logical resources observed by the capture |
| `Tags` | `string[]` | `["cdc", "postgresql", "provider-native"]` | Operator-facing tags on the descriptor |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing metadata merged onto the descriptor |

## Provider-native CDC runtime

When `CdcCaptures` are configured:

- each capture is published through `/engine/cdc-captures*` with `provider = "postgresql"`, `mode = "logical-replication"`, and an `executionBinding` whose authored and requested runtime id is `postgresql-logical-replication-capture-pump`
- the execution runtime is published through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` with `executionOwnership = host-managed`, `executionTopology = provider-native`, and `acknowledgementMode = provider-native`
- the same runtime publishes through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` under `postgresql-logical-replication-capture-flow` plus `postgresql-logical-replication-capture-pump`
- the hosted runner validates that the declared publication publishes the declared table, validates slot type/plugin/database ownership, optionally creates the logical replication slot, can drop and recreate an inactive invalidated slot when `RecreateSlotIfInvalidated = true`, reads one bounded committed pgoutput batch per iteration, stages one outbox message per captured change, and only confirms slot progress after the linked outbox accepted the batch
- each staged outbox message uses deterministic id `{commitLsn}:{ordinal}`, content type `application/vnd.cephalon.postgresql.logical-replication+json`, headers for `provider`, `cdcCaptureId`, `databaseName`, `schemaName`, `tableName`, `publicationName`, `slotName`, and `operation`, plus metadata for `sourceId`, `eventFormat`, and `checkpointToken`
- the runtime-state surface keeps typed freshness, lag, pending-publication posture, checkpoint, change id, last operation type, reporter metadata, failure-kind metadata, and slot lifecycle metadata such as `slotLifecycleState`, `slotLifecycleAction`, `slotResumeMode`, `slotRestartLsn`, `slotConfirmedFlushLsn`, `slotWalStatus`, and `slotInvalidationReason` on the same `/engine/cdc-captures/runtime*` catalog instead of inventing a PostgreSQL-specific monitor
- capture failure metadata flows through the provider-local typed failure metadata contract implemented by `PostgresLogicalReplicationCaptureException`, so the hosted-service error projection no longer depends on duck-typed reflection over arbitrary exception properties
- durable checkpoint tokens are serialized as `slotName|commitLsn|transactionEndLsn`, runtime metadata keeps `replicationCheckpointSource = "slot-confirmed-flush-lsn"`, and restart or resume posture stays grounded in the slot's confirmed flush position instead of a Cephalon-managed checkpoint table

## Runtime capabilities

When `PostgresDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.postgresql` | Always |
| `data.relational-store` | Always |
| `data.cdc.postgresql` | `CdcCaptures.Count > 0` |

## Live integration testing

The default repository test lane does not require a developer-managed PostgreSQL instance or Docker. PostgreSQL live CDC tests belong in `tests/Cephalon.Tests.CdcIntegration` and should use the shared external-service gate documented in that test project's README. The gate keeps PostgreSQL tests skipped by default, then lets a developer or CI job opt into either `CEPHALON_CDC_POSTGRES_CONNECTION_STRING` for a pre-provisioned database or `CEPHALON_CDC_TESTCONTAINERS=1` for a disposable Testcontainers-backed service.

## Not shipped in this slice

This pack intentionally still does not claim:

- `IReadStore` / `IWriteStore` dispatch backed directly by PostgreSQL
- Entity Framework or `DbContext` integration
- PostgreSQL-backed `IOutbox`, `IInbox`, or event-dispatch storage
- publication or table creation/migration orchestration beyond validating one declared publication/table path and optionally recreating an inactive invalidated slot
- logical decoding plugins beyond the shipped `pgoutput` path
- multi-table slot orchestration, fan-out, or low-code generation beyond one configured capture per table path
- automatic slot failover, multi-consumer lease orchestration, or external edge execution ownership beyond the shared `/engine/cdc-*` topology surfaces

These remain later slices so the current provider claim stays truthful.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.SqlServer](data-sqlserver.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
