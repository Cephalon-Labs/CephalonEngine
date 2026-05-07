# Cephalon.Data.Oracle

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.Oracle` is the Oracle provider-native CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC execution and runtime catalog family also fits redo-log style relational capture through Oracle LogMiner with SCN-backed progress, provider-owned execution, durable `commitScn|changeScn|rsId|ssn` checkpoints, source-database identity truth, archive-log lifecycle validation, and module-preserving capture ownership without an Oracle-specific registry in `Cephalon.Engine`.

## What it owns

- contributes configured provider-native Oracle LogMiner captures through `OracleLogMinerCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "oracle"` and `mode = "logminer"`
- publishes capability metadata `data.oracle`, `data.relational-store`, and `data.cdc.oracle` introspectable at runtime through the manifest
- publishes the provider-native CDC execution graph `oracle-logminer-capture-flow`, hosted execution `oracle-logminer-capture-pump`, and execution runtime `oracle-logminer-capture-pump` when Oracle LogMiner captures are configured
- runs a provider-native hosted service that resolves the starting SCN from the Cephalon-managed checkpoint row or the configured initial posture, validates Oracle database identity plus archive-log posture before LogMiner starts, opens one bounded Oracle LogMiner session over the current SCN range, stages outbox publications for committed table changes, persists the next durable checkpoint only after outbox stage success, and reports runtime posture through the shared `ICdcCaptureRuntimeReporter` surface
- stores durable Oracle LogMiner checkpoints in a Cephalon-managed checkpoint table, defaulting to `CEPHALON_CDC_CHECKPOINTS`, while preserving additive checkpoint provenance such as `DatabaseId`, `DatabaseUniqueName`, `ResetLogsChangeNumber`, `ArchiveLogMode`, and `SupplementalLogDataMin`
- preserves authored capture ownership through `CdcCaptureDescriptor.SourceModuleId` while surfacing `metadata.contributorModuleId = "oracle-data"` when the provider pack contributes the descriptor on behalf of another module
- carries opt-in live integration evidence through `tests/Cephalon.Tests.CdcIntegration`, where `OracleCdc_StagesOutboxAndCommitsCheckpointAgainstLiveLogMiner` runs the provider against a real Oracle LogMiner source rather than a fake transport

## Main surfaces

- `Configuration/OracleDataOptions.cs`
- `Configuration/OracleLogMinerCaptureOptions.cs`
- `Modules/OracleDataModule.cs`
- `Registration/OracleDataEngineBuilderExtensions.cs`
- `Services/IOracleLogMinerTransport.cs`
- `Services/OracleLogMinerCaptureHostedService.cs`
- `Services/OracleLogMinerCheckpointToken.cs`
- `Services/OracleLogMinerExecutionRuntimeContributor.cs`
- `Services/OracleLogMinerTransport.cs`
- `Services/OracleDataRuntimeIds.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral CDC descriptor catalog, capture-side execution binding, shared runtime-state catalog, additive execution-runtime catalog, external runtime reporting seam, and the shared `/engine/cdc-*`, `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` truth model. `Cephalon.Data.Oracle` adds the Oracle-specific LogMiner reader, SCN-window plus redo-log selection, database-identity validation, archive-log lifecycle policy, durable checkpoint store, and provider-native hosted execution loop needed to project truthful Oracle behavior into those shared surfaces.

That keeps the engine honest. The shared `data-cdc-capture-pump` still exists as the generic in-process runtime, but it ignores captures whose effective owner resolves to `oracle-logminer-capture-pump`. The Oracle pack becomes the fifth concrete provider-native CDC runner after MongoDB, SQL Server, PostgreSQL, and MySQL, and now also carries provider-native lifecycle and resume hardening on the same ownership and topology model.

The slice stays intentionally scoped. `Cephalon.Data.Oracle` does not claim general-purpose `IReadStore` or `IWriteStore` dispatch, Entity Framework integration, Oracle-backed outbox or inbox storage, GoldenGate or XStream orchestration, Data Guard ownership handoff, or an Oracle-specific operator registry outside the shared runtime story. If a host wants relational read or write persistence today, `Cephalon.Data.EntityFramework` remains the honest baseline. `Cephalon.Data.Oracle` claims provider-native LogMiner CDC capture plus lifecycle and checkpoint truth on the shared runtime surfaces.

## Registration

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddOracleData(
    connectionString: "User Id=system;Password=oracle;Data Source=localhost/XEPDB1",
    databaseName: "XEPDB1",
    configure: options =>
    {
        options.CdcCaptures.Add(new OracleLogMinerCaptureOptions
        {
            Id = "orders-cdc",
            DisplayName = "Orders CDC",
            SourceModuleId = "orders",
            TableSchema = "ORDERS",
            TableName = "ORDERS",
            OutboxId = "tenant-event-outbox",
            ChannelId = "orders",
            MessageType = "OrdersChanged",
            ExpectedDatabaseId = 147258369m,
            ExpectedDatabaseUniqueName = "CEPHALON_XEPDB1"
        });
    });
```

For configuration-driven hosts, prefer the options overload and bind from `Engine:Data:Oracle`:

```csharp
engine.AddData(options =>
{
    options.EnableCdcExecution = true;
});

engine.AddOracleData(options =>
{
    configuration.GetSection(OracleDataOptions.SectionPath).Bind(options);
    options.ConnectionStringName ??= "Oracle";
    options.DatabaseName = "XEPDB1";
});
```

```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=system;Password=oracle;Data Source=localhost/XEPDB1"
  },
  "Engine": {
    "Data": {
      "Oracle": {
        "ConnectionStringName": "Oracle",
        "DatabaseName": "XEPDB1",
        "CheckpointTableName": "CEPHALON_CDC_CHECKPOINTS",
        "CdcCaptures": [
          {
            "Id": "orders-cdc",
            "DisplayName": "Orders CDC",
            "SourceModuleId": "orders",
            "TableSchema": "ORDERS",
            "TableName": "ORDERS",
            "OutboxId": "tenant-event-outbox",
            "ChannelId": "orders",
            "MessageType": "OrdersChanged",
            "InitialPosition": "latest-available",
            "ExpectedDatabaseId": 147258369,
            "ExpectedDatabaseUniqueName": "CEPHALON_XEPDB1",
            "ResumeFromEarliestAvailableScnIfCheckpointUnavailable": false,
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

`ConnectionStringName` and `ConnectionString` are mutually exclusive. If both are set, the pack fails fast during service resolution. If neither is set, the Oracle provider cannot start.

## Configuration options (`Engine:Data:Oracle`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionStringName` | `string?` | `null` | Root `ConnectionStrings` key to resolve for Oracle |
| `ConnectionString` | `string?` | `null` | Inline Oracle connection string |
| `DatabaseName` | `string` | required | Operator-facing Oracle database name for the configured LogMiner captures |
| `CheckpointTableName` | `string` | `"CEPHALON_CDC_CHECKPOINTS"` | Table that stores Cephalon-managed Oracle LogMiner checkpoints |
| `CdcCaptures` | `OracleLogMinerCaptureOptions[]` | `[]` | Contribute provider-native Oracle LogMiner captures |

## Oracle LogMiner capture options (`Engine:Data:Oracle:CdcCaptures[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable CDC capture id |
| `DisplayName` | `string` | `Id` | Operator-facing capture name |
| `Description` | `string` | generated | Human-readable capture description |
| `SourceModuleId` | `string` | required | Module id that owns the capture surface |
| `SourceId` | `string` | `{provider}:{database}/{schema}.{table}` | Logical upstream source id when it should differ from the watched table path |
| `TableSchema` | `string` | required | Oracle schema name of the tracked table |
| `TableName` | `string` | required | Oracle table name of the tracked table |
| `OutboxId` | `string` | required | Linked outbox id that receives publications |
| `ChannelId` | `string` | required | Logical outbox channel id for emitted publications |
| `MessageType` | `string` | required | Logical message type for emitted publications |
| `EventFormat` | `string` | `"oracle-logminer-redo-event"` | Operator-facing event format projected on the descriptor |
| `InitialPosition` | `string` | `"latest-available"` | Initial position when no durable checkpoint exists yet; supported values are `latest-available` and `earliest-available` |
| `ExpectedDatabaseId` | `decimal?` | `null` | Optional expected Oracle `DBID`; when set, the provider-native runner fails fast if the live source database reports a different database id |
| `ExpectedDatabaseUniqueName` | `string` | empty | Optional expected `DB_UNIQUE_NAME`; when set, the provider-native runner fails fast if the live source database reports a different unique name |
| `ResumeFromEarliestAvailableScnIfCheckpointUnavailable` | `bool` | `false` | Controls whether a checkpoint older than the retained archive-log window fails fast or reseeds from the earliest retained SCN |
| `MaxChangesPerRead` | `int` | `128` | Maximum number of captured row changes to stage during one provider-native iteration |
| `MaxAwaitTimeSeconds` | `int` | `5` | Maximum number of seconds to await committed redo during one provider-native iteration |
| `PollingIntervalSeconds` | `int` | `5` | Polling interval in seconds for one provider-native loop iteration |
| `ResourceIds` | `string[]` | `["{schema}.{table}"]` | Explicit logical resources observed by the capture |
| `Tags` | `string[]` | `["cdc", "oracle", "provider-native"]` | Operator-facing tags on the descriptor |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing metadata merged onto the descriptor |

## Provider-native CDC runtime

When `CdcCaptures` are configured:

- each capture is published through `/engine/cdc-captures*` with `provider = "oracle"`, `mode = "logminer"`, and an `executionBinding` whose authored and requested runtime id is `oracle-logminer-capture-pump`
- the execution runtime is published through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` with `executionOwnership = host-managed`, `executionTopology = provider-native`, and `acknowledgementMode = provider-native`
- when `Cephalon.Data` is active, the same capture and execution-runtime truth also appears under the `data-management` technology surfaces `cdc-captures` and `cdc-capture-runtimes` with sanitized metadata so connection-string or token-shaped values are not projected to operators
- the same runtime publishes through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot` under `oracle-logminer-capture-flow` plus `oracle-logminer-capture-pump`
- the hosted runner resolves its starting point from the durable Cephalon checkpoint row when one exists, otherwise from the configured initial SCN posture, reads live `DBID`, `DB_UNIQUE_NAME`, `CURRENT_SCN`, archive-log mode, and resetlogs posture from `V$DATABASE`, validates configured or checkpoint-derived source identity before starting LogMiner, validates archive-log posture, resolves the earliest retained SCN, and then selects the redo plus archive-log files that cover the SCN window
- each staged outbox message uses content type `application/vnd.cephalon.oracle.logminer+json`, keeps deterministic change ids and checkpoint tokens, and publishes additive metadata such as `startScn`, `endScn`, `currentScn`, `earliestAvailableScn`, `resumeMode`, `checkpointStore`, `checkpointSource`, `databaseId`, `databaseUniqueName`, `databaseIdentityState`, `databaseIdentityAction`, `archiveLogMode`, `archiveLogLifecycleState`, `archiveLogLifecycleAction`, `resetLogsChangeNumber`, `supplementalLogDataMin`, `logMinerDictionary`, `logMinerMode`, `redoCursor`, and `logFileCount` on the same shared runtime story
- provider-specific failures now distinguish lifecycle and restart posture such as `archive-log-disabled`, `database-identity-mismatch`, `database-identity-unavailable`, `checkpoint-database-mismatch`, `checkpoint-scn-unavailable`, `checkpoint-scn-ahead-of-current`, `log-files-unavailable`, and `logminer-start`, while shared stage or checkpoint failures still remain visible as the shared `missing-outbox`, `outbox-stage`, and `checkpoint` categories on the same runtime surfaces
- capture failure metadata flows through the provider-local typed failure metadata contract implemented by `OracleLogMinerCaptureException`, so the hosted-service error projection no longer depends on duck-typed reflection over arbitrary exception properties

### Checkpoint table schema (`CEPHALON_CDC_CHECKPOINTS`)

The default table name is `CheckpointTableName`.

| Column | SQL type | Notes |
|--------|----------|-------|
| `CdcCaptureId` | `VARCHAR2(128)` | Stable CDC capture id; one checkpoint row per capture |
| `CommitScn` | `NUMBER(38)` | Latest durable commit SCN |
| `ChangeScn` | `NUMBER(38)` | Latest durable row-change SCN |
| `RecordSetId` | `VARCHAR2(64)` | Latest durable LogMiner record-set id (`RS_ID`) |
| `SqlSequenceNumber` | `NUMBER(19)` | Latest durable SQL sequence number (`SSN`) |
| `CheckpointToken` | `VARCHAR2(512)` | Serialized checkpoint token in `commitScn|changeScn|rsId|ssn` form |
| `DatabaseId` | `NUMBER(38)` | Latest durable Oracle `DBID` observed when the checkpoint was committed |
| `DatabaseUniqueName` | `VARCHAR2(128)` | Latest durable Oracle `DB_UNIQUE_NAME` observed when the checkpoint was committed |
| `ResetLogsChangeNumber` | `NUMBER(38)` | Latest durable `RESETLOGS_CHANGE#` observed when the checkpoint was committed |
| `ArchiveLogMode` | `VARCHAR2(32)` | Archive-log posture observed when the checkpoint was committed |
| `SupplementalLogDataMin` | `VARCHAR2(32)` | Additive supplemental-log metadata observed when the checkpoint was committed |
| `UpdatedAtUtc` | `TIMESTAMP WITH TIME ZONE` | UTC timestamp of the last durable checkpoint write |

## Live integration evidence

The dedicated CDC integration lane now includes `OracleCdcIntegrationTests.OracleCdc_StagesOutboxAndCommitsCheckpointAgainstLiveLogMiner`. The test is skipped by default through the shared external-service gate, then runs when `CEPHALON_CDC_EXTERNAL_SERVICES=1` and either `CEPHALON_CDC_ORACLE_CONNECTION_STRING` or `CEPHALON_CDC_TESTCONTAINERS=1` is present.

The pre-provisioned mode expects a database user that can create/drop the test table, add table-level supplemental logging, read `V$DATABASE`, `V$ARCHIVED_LOG`, `V$LOG`, `V$LOGFILE`, and `V$LOGMNR_CONTENTS`, and execute `DBMS_LOGMNR`. The Testcontainers mode uses `gvenzl/oracle-xe:21.3.0-slim-faststart`, enables `ARCHIVELOG`, opens the pluggable database, enables supplemental logging, grants the LogMiner privileges to the app user, inserts a real `ORDERS` row, waits for an outbox message, and verifies shared runtime-state, execution-runtime aggregation, staged LogMiner payload metadata, and durable `commitScn|changeScn|rsId|ssn` checkpoint persistence.

## Not shipped in this slice

This pack intentionally still does not claim:

- `IReadStore` or `IWriteStore` dispatch backed directly by Oracle
- Entity Framework or `DbContext` integration
- Oracle-backed `IOutbox`, `IInbox`, or event-dispatch storage
- GoldenGate, XStream, Data Guard failover ownership, or out-of-process Oracle CDC execution ownership beyond the shared `/engine/cdc-*` topology surfaces
- automatic operator remediation for archive-log gaps beyond the explicit fail-fast or reseed policy on one capture
- low-code generation or multi-table orchestration beyond one configured capture per table path

These remain later slices so the current provider claim stays truthful.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.SqlServer](data-sqlserver.md)
- [Cephalon.Data.Postgres](data-postgres.md)
- [Cephalon.Data.MySql](data-mysql.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
