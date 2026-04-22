# Cephalon.Data.MySql

`Cephalon.Data.MySql` is the MySQL provider-native CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC execution and runtime catalog family also fits a binlog-backed relational source with durable file-plus-position checkpoints, provider-owned execution, and module-preserving capture ownership truth without a MySQL-specific registry in `Cephalon.Engine`.

## What it owns

- contributes configured provider-native MySQL binlog captures through `MySqlBinlogCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "mysql"` and `mode = "binlog"`
- publishes capability metadata `data.mysql`, `data.relational-store`, and `data.cdc.mysql` introspectable at runtime through the manifest
- publishes the provider-native CDC execution graph `mysql-binlog-capture-flow`, hosted execution `mysql-binlog-capture-pump`, and execution runtime `mysql-binlog-capture-pump` when MySQL binlog captures are configured
- runs a provider-native hosted service that resolves start position from the Cephalon-managed checkpoint table or from the configured initial binlog posture, reads one bounded binlog row-event batch, stages outbox publications, persists the next durable binlog checkpoint only after outbox stage success, and reports runtime posture through the shared `ICdcCaptureRuntimeReporter` surface
- stores durable MySQL binlog checkpoints in a Cephalon-managed checkpoint table, defaulting to `cephalon_cdc_checkpoints`
- preserves authored capture ownership through `CdcCaptureDescriptor.SourceModuleId` while surfacing `metadata.contributorModuleId = "mysql-data"` when the provider pack contributes the descriptor on behalf of another module

## Main surfaces

- `Configuration/MySqlDataOptions.cs`
- `Configuration/MySqlBinlogCaptureOptions.cs`
- `Modules/MySqlDataModule.cs`
- `Registration/MySqlDataEngineBuilderExtensions.cs`
- `Services/IMySqlBinlogTransport.cs`
- `Services/MySqlBinlogCaptureHostedService.cs`
- `Services/MySqlBinlogCheckpointToken.cs`
- `Services/MySqlBinlogExecutionRuntimeContributor.cs`
- `Services/MySqlBinlogTransport.cs`
- `Services/MySqlDataRuntimeIds.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral CDC descriptor catalog, capture-side execution binding, shared runtime-state catalog, additive execution-runtime catalog, external runtime reporting seam, and the shared `/engine/cdc-*`, `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` truth model. `Cephalon.Data.MySql` adds the MySQL-specific binlog reader, file-plus-position checkpoint store, and provider-native hosted execution loop needed to project truthful MySQL behavior into those shared surfaces.

That keeps the engine honest. The shared `data-cdc-capture-pump` still exists as the generic in-process runtime, but it simply ignores captures whose effective owner resolves to `mysql-binlog-capture-pump`. The MySQL pack becomes the fourth concrete provider-native CDC runner after MongoDB, SQL Server, and PostgreSQL, and the first explicit MySQL binlog proof on the same ownership and topology model.

The slice stays intentionally scoped. `Cephalon.Data.MySql` does not claim general-purpose `IReadStore` or `IWriteStore` dispatch, Entity Framework integration, MySQL-backed outbox or inbox storage, GTID-based orchestration, or MySQL-specific lifecycle coordination beyond the shared runtime story. If a host wants relational read or write persistence today, `Cephalon.Data.EntityFramework` remains the honest baseline. `Cephalon.Data.MySql` only claims provider-native CDC capture plus durable binlog checkpoint truth.

## Registration

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddMySqlData(
    connectionString: "Server=localhost;Port=3306;Database=app;User ID=root;Password=mysql",
    databaseName: "app",
    configure: options =>
    {
        options.CdcCaptures.Add(new MySqlBinlogCaptureOptions
        {
            Id = "orders-cdc",
            DisplayName = "Orders CDC",
            SourceModuleId = "orders",
            TableSchema = "app",
            TableName = "orders",
            ServerId = 700001,
            OutboxId = "tenant-event-outbox",
            ChannelId = "orders",
            MessageType = "OrdersChanged"
        });
    });
```

For configuration-driven hosts, prefer the options overload and bind from `Engine:Data:MySql`:

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddMySqlData(options =>
{
    configuration.GetSection(MySqlDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "MySql";
    options.DatabaseName = "app";
});
```

```json
{
  "ConnectionStrings": {
    "MySql": "Server=localhost;Port=3306;Database=app;User ID=root;Password=mysql"
  },
  "Engine": {
    "Data": {
      "MySql": {
        "ConnectionStringName": "MySql",
        "DatabaseName": "app",
        "CheckpointTableName": "cephalon_cdc_checkpoints",
        "CdcCaptures": [
          {
            "Id": "orders-cdc",
            "DisplayName": "Orders CDC",
            "SourceModuleId": "orders",
            "TableSchema": "app",
            "TableName": "orders",
            "ServerId": 700001,
            "OutboxId": "tenant-event-outbox",
            "ChannelId": "orders",
            "MessageType": "OrdersChanged",
            "InitialPosition": "latest-available",
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

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the pack fails fast during service resolution. If neither is set, the MySQL provider cannot start.

## Configuration options (`Engine:Data:MySql`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionStringName` | `string?` | `null` | Root `ConnectionStrings` key to resolve for MySQL |
| `ConnectionString` | `string?` | `null` | Inline MySQL connection string |
| `DatabaseName` | `string` | required | Operator-facing database name for the configured binlog captures |
| `CheckpointTableName` | `string` | `"cephalon_cdc_checkpoints"` | Table that stores Cephalon-managed MySQL binlog checkpoints |
| `CdcCaptures` | `MySqlBinlogCaptureOptions[]` | `[]` | Contribute provider-native MySQL binlog captures |

## MySQL binlog capture options (`Engine:Data:MySql:CdcCaptures[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable CDC capture id |
| `DisplayName` | `string` | `Id` | Operator-facing capture name |
| `Description` | `string` | generated | Human-readable capture description |
| `SourceModuleId` | `string` | required | Module id that owns the capture surface |
| `SourceId` | `string` | `{provider}:{database}/{schema-or-database}/{table}` | Logical upstream source id when it should differ from the watched table path |
| `TableSchema` | `string` | `{DatabaseName}` | MySQL schema or database name of the tracked table |
| `TableName` | `string` | required | Table name of the tracked table |
| `ServerId` | `int` | `700001` | Replication client server id used for the capture connection; must be positive and stable |
| `OutboxId` | `string` | required | Linked outbox id that receives publications |
| `ChannelId` | `string` | required | Logical outbox channel id for emitted publications |
| `MessageType` | `string` | required | Logical message type for emitted publications |
| `EventFormat` | `string` | `"mysql-binlog-row-event"` | Operator-facing event format projected on the descriptor |
| `InitialPosition` | `string` | `"latest-available"` | Initial position when no durable checkpoint exists yet; supported values are `latest-available` and `earliest-available` |
| `MaxChangesPerRead` | `int` | `128` | Maximum number of captured row changes to stage during one provider-native iteration |
| `MaxAwaitTimeSeconds` | `int` | `5` | Maximum number of seconds to await row events during one provider-native iteration |
| `PollingIntervalSeconds` | `int` | `5` | Polling interval in seconds for one provider-native loop iteration |
| `ResourceIds` | `string[]` | `["{schema}.{table}"]` | Explicit logical resources observed by the capture |
| `Tags` | `string[]` | `["cdc", "mysql", "provider-native"]` | Operator-facing tags on the descriptor |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing metadata merged onto the descriptor |

## Provider-native CDC runtime

When `CdcCaptures` are configured:

- each capture is published through `/engine/cdc-captures*` with `provider = "mysql"`, `mode = "binlog"`, and an `executionBinding` whose authored and requested runtime id is `mysql-binlog-capture-pump`
- the execution runtime is published through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` with `executionOwnership = host-managed`, `executionTopology = provider-native`, and `acknowledgementMode = provider-native`
- the same runtime publishes through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` under `mysql-binlog-capture-flow` plus `mysql-binlog-capture-pump`
- the hosted runner resolves its starting point from the durable Cephalon checkpoint row when one exists, otherwise from the configured initial binlog posture, reads one bounded row-event batch per iteration, stages one outbox message per captured row change, and only persists the next durable checkpoint after the linked outbox accepted the batch
- each staged outbox message uses content type `application/vnd.cephalon.mysql.binlog+json`, keeps deterministic change ids and checkpoint tokens, and publishes operator-facing metadata such as `binlogFile`, `binlogPosition`, `binlogResumeMode`, `checkpointStore`, and `binlogCheckpointSource` on the same shared runtime story
- the transport intentionally uses the installed `SciSharp.MySQL.Replication` client together with a thin internal adapter path so Cephalon can keep explicit start-position ownership and durable checkpoint truth on the shared runtime surfaces instead of inventing a MySQL-only monitor or start-position registry

### Checkpoint table schema (`cephalon_cdc_checkpoints`)

The default table name is `CheckpointTableName`.

| Column | SQL type | Notes |
|--------|----------|-------|
| `CdcCaptureId` | `varchar(128)` | Stable CDC capture id; one checkpoint row per capture |
| `BinlogFile` | `varchar(255)` | Latest durable binlog file name |
| `BinlogPosition` | `bigint` | Latest durable file position |
| `CheckpointToken` | `varchar(512)` | Serialized checkpoint token in `binlogFile|position` form |
| `UpdatedAtUtc` | `datetime(6)` | UTC timestamp of the last durable checkpoint write |

## Runtime capabilities

When `MySqlDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.mysql` | Always |
| `data.relational-store` | Always |
| `data.cdc.mysql` | `CdcCaptures.Count > 0` |

## Not shipped in this slice

This pack intentionally still does not claim:

- `IReadStore` or `IWriteStore` dispatch backed directly by MySQL
- Entity Framework or `DbContext` integration
- MySQL-backed `IOutbox`, `IInbox`, or event-dispatch storage
- GTID-based resume or failover orchestration
- binlog retention or source-server lifecycle hardening beyond durable file-plus-position checkpoints
- Debezium or out-of-process MySQL CDC execution ownership beyond the shared `/engine/cdc-*` topology surfaces
- low-code generation or multi-table orchestration beyond one configured capture per table path

These remain later slices so the current provider claim stays truthful.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.SqlServer](data-sqlserver.md)
- [Cephalon.Data.Postgres](data-postgres.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
