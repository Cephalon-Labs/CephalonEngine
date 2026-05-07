# Cephalon.Data.MySql

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.MySql` is the MySQL provider-native CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC execution and runtime catalog family also fits a binlog-backed relational source with durable file-plus-position checkpoints, provider-owned execution, source-server identity plus binlog lifecycle validation, and module-preserving capture ownership truth without a MySQL-specific registry in `Cephalon.Engine`. The core package owns the contract and runtime loop; concrete binlog wire adapters are optional companion packages.

## What it owns

- contributes configured provider-native MySQL binlog captures through `MySqlBinlogCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "mysql"` and `mode = "binlog"`
- publishes capability metadata `data.mysql`, `data.relational-store`, and `data.cdc.mysql` introspectable at runtime through the manifest
- publishes the provider-native CDC execution graph `mysql-binlog-capture-flow`, hosted execution `mysql-binlog-capture-pump`, and execution runtime `mysql-binlog-capture-pump` when MySQL binlog captures are configured
- runs a provider-native hosted service that validates source-server identity and retained-binlog lifecycle posture, resolves start position from the Cephalon-managed checkpoint table or from the configured initial binlog posture, reads one bounded binlog row-event batch through an installed transport adapter, stages outbox publications, persists the next durable binlog checkpoint only after outbox stage success, and reports runtime posture through the shared `ICdcCaptureRuntimeReporter` surface
- stores durable MySQL binlog checkpoints in a Cephalon-managed checkpoint table, defaulting to `cephalon_cdc_checkpoints`
- extends that checkpoint table with provider-native resume metadata such as `SourceServerUuid`, `SourceServerId`, `GtidExecutedSet`, `BinlogFormat`, and `BinlogRowImage` so restart-safe operator truth stays on the shared CDC surfaces instead of a provider-local registry
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
- `Services/MySqlDataRuntimeIds.cs`
- `Services/UnsupportedMySqlBinlogTransport.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral CDC descriptor catalog, capture-side execution binding, shared runtime-state catalog, additive execution-runtime catalog, external runtime reporting seam, and the shared `/engine/cdc-*`, `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` truth model. `Cephalon.Data.MySql` adds the MySQL-specific CDC contract, file-plus-position checkpoint store, and provider-native hosted execution loop needed to project truthful MySQL behavior into those shared surfaces. Concrete binlog transport is an adapter boundary; the current SciSharp adapter lives in [`Cephalon.Data.MySql.SciSharpReplication`](data-mysql-scisharp-replication.md).

That keeps the engine honest. The shared `data-cdc-capture-pump` still exists as the generic in-process runtime, but it simply ignores captures whose effective owner resolves to `mysql-binlog-capture-pump`. The MySQL pack becomes the fourth concrete provider-native CDC runner after MongoDB, SQL Server, and PostgreSQL, and the first explicit MySQL binlog proof on the same ownership and topology model.

The dedicated CDC integration lane now includes an opt-in live MySQL proof. `tests/Cephalon.Tests.CdcIntegration` can run `MySqlCdcIntegrationTests.MySqlCdc_StagesOutboxAndCommitsCheckpointAgainstLiveBinlog` against either a pre-provisioned MySQL database or a disposable `mysql:8.4` Testcontainers service. That lane runs the current SciSharp adapter against a real row-based binlog source, inserts a row, stages the linked outbox message, reports runtime-state and execution-runtime truth, and verifies the durable `binlogFile|position` checkpoint row. Default test runs still discover but skip the MySQL live lane unless the external-service gate is explicitly enabled.

The slice stays intentionally scoped. `Cephalon.Data.MySql` does not claim general-purpose `IReadStore` or `IWriteStore` dispatch, Entity Framework integration, MySQL-backed outbox or inbox storage, GTID-based failover orchestration, a built-in MySQL wire adapter, or a MySQL-specific operator registry outside the shared runtime story. If a host wants relational read or write persistence today, `Cephalon.Data.EntityFramework` remains the honest baseline. `Cephalon.Data.MySql` claims provider-native CDC capture descriptors, source-server identity plus binlog lifecycle validation, GTID observe-only metadata, and durable checkpoint truth on the shared runtime surfaces.

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
    })
    .AddSciSharpMySqlBinlogReplication();
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
})
.AddSciSharpMySqlBinlogReplication();
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

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the installed transport adapter fails fast during service resolution. If neither is set, the MySQL provider cannot start. If MySQL CDC is configured but no transport adapter is installed, the hosted runner reports `failureKind = "binlog-transport-adapter-missing"` instead of hiding an implicit third-party dependency in the core package.

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
| `ExpectedSourceServerUuid` | `string` | `""` | Optional upstream server UUID that the runner must observe before it starts or resumes; leave blank for observe-only source-server identity metadata |
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
- when `Cephalon.Data` is active, the same capture and execution-runtime truth also appears under the `data-management` technology surfaces `cdc-captures` and `cdc-capture-runtimes` with sanitized metadata so connection-string or token-shaped values are not projected to operators
- the same runtime publishes through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` under `mysql-binlog-capture-flow` plus `mysql-binlog-capture-pump`
- the hosted runner resolves its starting point from the durable Cephalon checkpoint row when one exists, otherwise from the configured initial binlog posture, validates retained-binlog lifecycle posture plus optional `ExpectedSourceServerUuid`, reads one bounded row-event batch per iteration, stages one outbox message per captured row change, and only persists the next durable checkpoint after the linked outbox accepted the batch
- each staged outbox message uses content type `application/vnd.cephalon.mysql.binlog+json`, keeps deterministic change ids and checkpoint tokens, and publishes operator-facing metadata such as `binlogFile`, `binlogPosition`, `binlogResumeMode`, `binlogLifecycleState`, `binlogLifecycleAction`, `checkpointStore`, `binlogCheckpointSource`, `sourceServerUuid`, `sourceServerIdentityState`, `sourceServerIdentityAction`, `gtidMode`, and `gtidExecutedSet` on the same shared runtime story
- capture failure metadata flows through the provider-local typed failure metadata contract implemented by `MySqlBinlogCaptureException`, so the hosted-service error projection no longer depends on duck-typed reflection over arbitrary exception properties
- the core package intentionally requires an installed binlog transport adapter; the current `Cephalon.Data.MySql.SciSharpReplication` adapter isolates the `SciSharp.MySQL.Replication` path so Cephalon can keep explicit start-position ownership and durable checkpoint truth without carrying third-party non-public reflection in the core MySQL package

### Checkpoint table schema (`cephalon_cdc_checkpoints`)

The default table name is `CheckpointTableName`.

| Column | SQL type | Notes |
|--------|----------|-------|
| `CdcCaptureId` | `varchar(128)` | Stable CDC capture id; one checkpoint row per capture |
| `BinlogFile` | `varchar(255)` | Latest durable binlog file name |
| `BinlogPosition` | `bigint` | Latest durable file position |
| `CheckpointToken` | `varchar(512)` | Serialized checkpoint token in `binlogFile|position` form |
| `SourceServerUuid` | `varchar(128)` | Last observed MySQL source-server UUID captured with the durable checkpoint |
| `SourceServerId` | `bigint` | Last observed MySQL source-server id captured with the durable checkpoint |
| `GtidExecutedSet` | `longtext` | Last observed GTID set kept for operator diagnostics and resume context |
| `BinlogFormat` | `varchar(32)` | Last observed MySQL `binlog_format` value |
| `BinlogRowImage` | `varchar(32)` | Last observed MySQL `binlog_row_image` value |
| `UpdatedAtUtc` | `datetime(6)` | UTC timestamp of the last durable checkpoint write |

## Lifecycle and resume metadata

The provider-native MySQL runner keeps lifecycle and resume truth on the existing shared CDC runtime surfaces rather than on a provider-local operator endpoint.

Important metadata families now include:

- `binlogResumeMode`, `resumeCheckpoint`, `checkpointUpdatedAtUtc`, and `checkpoint*` metadata so operators can see whether the runner started from a durable checkpoint or from a configured initial posture
- `binlogLifecycleState` plus `binlogLifecycleAction` so retained-binlog validation can distinguish available, purged, or unavailable lifecycle posture before the runner starts reading
- `sourceServerUuid`, `sourceServerId`, `sourceServerIdentityState`, `sourceServerIdentityAction`, `checkpointSourceServerUuid`, and `checkpointSourceServerId` so restart-safe source-server identity truth stays visible during resume, mismatch, and fail-fast paths
- `gtidMode`, `gtidExecutedSet`, `checkpointGtidExecutedSet`, `binlogFormat`, `binlogRowImage`, and matching checkpoint metadata so hosts can observe provider-native replication posture without Cephalon claiming GTID-driven takeover orchestration

Representative failure kinds now include:

- `binary-logging-disabled`
- `binlog-format-unsupported`
- `source-server-identity-unavailable`
- `source-server-mismatch`
- `checkpoint-source-server-mismatch`
- `checkpoint-binlog-unavailable`

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
- GTID-based resume or failover orchestration beyond observe-only metadata
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
