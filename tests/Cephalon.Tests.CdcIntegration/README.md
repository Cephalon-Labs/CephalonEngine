# Cephalon CDC Integration Tests

This project is the focused integration-test lane for provider-backed CDC behavior that needs a real data-system runtime rather than an in-memory or fake transport.

## Current baseline

- MongoDB change streams run against a disposable single-node replica set through `MongoDbReplicaSetRunner` and `EphemeralMongo7`, so the baseline does not require Docker or a developer-managed MongoDB instance.
- The MongoDB test proves outbox staging, provider-native runtime binding, runtime-state reporting, execution-runtime aggregation, and durable checkpoint persistence through the same contracts exposed by `Cephalon.Data` and `Cephalon.Data.MongoDB`.
- SQL Server live CDC now runs on the shared external-service gate below. The test creates an isolated SQL Server database, enables native CDC on a real `dbo.orders` table, verifies outbox staging and runtime-state reporting through `Cephalon.Data.SqlServer`, and stays skipped unless a developer or CI job opts into a provider mode.
- Postgres live CDC now runs on the same external-service gate. The test creates an isolated PostgreSQL schema, table, publication, and logical replication slot, verifies provider-native outbox staging and runtime-state reporting through `Cephalon.Data.Postgres`, confirms slot-backed checkpoint truth, and stays skipped unless a developer or CI job opts into a provider mode.
- MySQL live CDC now runs on the same external-service gate. The test enables a real row-based binlog source, runs `Cephalon.Data.MySql` with the `Cephalon.Data.MySql.SciSharpReplication` adapter, verifies provider-native outbox staging and runtime-state reporting, confirms durable `binlogFile|position` checkpoint truth, and stays skipped unless a developer or CI job opts into a provider mode.

## External-service gate

The default test command must stay deterministic and must not require Docker, Testcontainers, SQL Server, Postgres, or MySQL. Tests that need external relational runtimes should use `ExternalCdcServiceFactAttribute` from `ExternalServices/` so the lane is discovered but skipped until a developer or CI job opts in. Provider-specific tests should use `ExternalCdcServiceFactAttribute(ExternalCdcServiceProvider.SqlServer)`, `ExternalCdcServiceFactAttribute(ExternalCdcServiceProvider.Postgres)`, or `ExternalCdcServiceFactAttribute(ExternalCdcServiceProvider.MySql)` so enabling the general external lane still skips a provider until that provider has a connection string or Testcontainers mode.

The shared gate uses these environment variables:

| Variable | Purpose |
| --- | --- |
| `CEPHALON_CDC_EXTERNAL_SERVICES` | Set to `1`, `true`, `yes`, or `on` to enable the external CDC service lane. |
| `CEPHALON_CDC_TESTCONTAINERS` | Set to `1`, `true`, `yes`, or `on` with `CEPHALON_CDC_EXTERNAL_SERVICES=1` when provider tests should create disposable services through Testcontainers. |
| `CEPHALON_CDC_SQLSERVER_CONNECTION_STRING` | Optional pre-provisioned SQL Server connection string. When present, SQL Server live tests should use it instead of Testcontainers. |
| `CEPHALON_CDC_POSTGRES_CONNECTION_STRING` | Optional pre-provisioned Postgres connection string. When present, Postgres live tests should use it instead of Testcontainers. |
| `CEPHALON_CDC_MYSQL_CONNECTION_STRING` | Optional pre-provisioned MySQL connection string. When present, MySQL live tests should use it instead of Testcontainers. |

Provider-specific live tests should prefer a pre-provisioned connection string when present, otherwise use Testcontainers only when both `CEPHALON_CDC_EXTERNAL_SERVICES` and `CEPHALON_CDC_TESTCONTAINERS` are enabled. If neither provider mode resolves, the test should remain skipped rather than silently passing with fake transport coverage.

## SQL Server live lane

`SqlServerCdcIntegrationTests.SqlServerCdc_StagesOutboxAndPersistsCheckpointAgainstLiveDatabase` is the first provider-specific live relational CDC test on this gate. It needs a SQL Server instance where the login can create and drop an isolated test database, enable database/table CDC, and run SQL Server Agent-backed CDC capture jobs. In Testcontainers mode the test uses the Microsoft SQL Server 2022 container image with `MSSQL_AGENT_ENABLED=true`; in pre-provisioned mode the supplied connection string is normalized to `master` before the isolated database is created.

To run only the SQL Server live lane through Testcontainers:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --filter FullyQualifiedName~SqlServerCdc_StagesOutboxAndPersistsCheckpointAgainstLiveDatabase --logger "console;verbosity=normal"
```

To run only the SQL Server live lane against a pre-provisioned service:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_SQLSERVER_CONNECTION_STRING = 'Server=localhost;User Id=sa;Password=<password>;TrustServerCertificate=true'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --filter FullyQualifiedName~SqlServerCdc_StagesOutboxAndPersistsCheckpointAgainstLiveDatabase --logger "console;verbosity=normal"
```

## Postgres live lane

`PostgresCdcIntegrationTests.PostgresCdc_StagesOutboxAndConfirmsSlotCheckpointAgainstLiveDatabase` proves the PostgreSQL logical-replication runner against a live service. It needs a PostgreSQL database where the login can create and drop an isolated schema, create/drop a publication, and create/drop logical replication slots. In Testcontainers mode the test uses `postgres:16-alpine` with `wal_level=logical`, `max_wal_senders=10`, and `max_replication_slots=10`; in pre-provisioned mode the supplied connection string is used directly and the test creates unique schema/publication/slot names inside that database.

To run only the Postgres live lane through Testcontainers:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --filter FullyQualifiedName~PostgresCdc_StagesOutboxAndConfirmsSlotCheckpointAgainstLiveDatabase --logger "console;verbosity=normal"
```

To run only the Postgres live lane against a pre-provisioned service:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_POSTGRES_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=cephalon_cdc;Username=postgres;Password=<password>'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --filter FullyQualifiedName~PostgresCdc_StagesOutboxAndConfirmsSlotCheckpointAgainstLiveDatabase --logger "console;verbosity=normal"
```

## MySQL live lane

`MySqlCdcIntegrationTests.MySqlCdc_StagesOutboxAndCommitsCheckpointAgainstLiveBinlog` proves the MySQL binlog runner and current SciSharp transport adapter against a live service. It needs a MySQL database where the login can create and drop an isolated table, read server/binlog posture, and use replication privileges for row-based binlog streaming. In Testcontainers mode the test uses `mysql:8.4` with binary logging, row binlog format, full row image, GTID mode, and enforced GTID consistency enabled; in pre-provisioned mode the supplied connection string is used directly and the test creates unique table/capture identifiers inside that database.

To run only the MySQL live lane through Testcontainers:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --filter FullyQualifiedName~MySqlCdc_StagesOutboxAndCommitsCheckpointAgainstLiveBinlog --logger "console;verbosity=normal"
```

To run only the MySQL live lane against a pre-provisioned service:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_MYSQL_CONNECTION_STRING = 'Server=localhost;Port=3306;Database=cephalon_cdc;User ID=replica;Password=<password>;AllowPublicKeyRetrieval=True;SslMode=Preferred'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --filter FullyQualifiedName~MySqlCdc_StagesOutboxAndCommitsCheckpointAgainstLiveBinlog --logger "console;verbosity=normal"
```

## Run

```powershell
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```

To run all external-service live lanes through Testcontainers:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```

To run all live lanes against pre-provisioned services:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_SQLSERVER_CONNECTION_STRING = 'Server=localhost;Database=cephalon_cdc;User Id=sa;Password=<password>;TrustServerCertificate=true'
$env:CEPHALON_CDC_POSTGRES_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=cephalon_cdc;Username=postgres;Password=<password>'
$env:CEPHALON_CDC_MYSQL_CONNECTION_STRING = 'Server=localhost;Port=3306;Database=cephalon_cdc;User ID=replica;Password=<password>;AllowPublicKeyRetrieval=True;SslMode=Preferred'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```
