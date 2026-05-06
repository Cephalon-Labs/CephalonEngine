# Cephalon CDC Integration Tests

This project is the focused integration-test lane for provider-backed CDC behavior that needs a real data-system runtime rather than an in-memory or fake transport.

## Current baseline

- MongoDB change streams run against a disposable single-node replica set through `MongoDbReplicaSetRunner` and `EphemeralMongo7`, so the baseline does not require Docker or a developer-managed MongoDB instance.
- The MongoDB test proves outbox staging, provider-native runtime binding, runtime-state reporting, execution-runtime aggregation, and durable checkpoint persistence through the same contracts exposed by `Cephalon.Data` and `Cephalon.Data.MongoDB`.
- SQL Server and Postgres live CDC tests are intentionally opt-in. Their current coverage remains in the fake transport harnesses under the hosting/support test projects until the provider-specific live tests land on the external-service gate below.

## External-service gate

The default test command must stay deterministic and must not require Docker, Testcontainers, SQL Server, or Postgres. Tests that need external SQL Server/Postgres runtimes should use `ExternalCdcServiceFactAttribute` from `ExternalServices/` so the lane is discovered but skipped until a developer or CI job opts in. Provider-specific tests should use `ExternalCdcServiceFactAttribute(ExternalCdcServiceProvider.SqlServer)` or `ExternalCdcServiceFactAttribute(ExternalCdcServiceProvider.Postgres)` so enabling the general external lane still skips a provider until that provider has a connection string or Testcontainers mode.

The shared gate uses these environment variables:

| Variable | Purpose |
| --- | --- |
| `CEPHALON_CDC_EXTERNAL_SERVICES` | Set to `1`, `true`, `yes`, or `on` to enable the external CDC service lane. |
| `CEPHALON_CDC_TESTCONTAINERS` | Set to `1`, `true`, `yes`, or `on` with `CEPHALON_CDC_EXTERNAL_SERVICES=1` when provider tests should create disposable services through Testcontainers. |
| `CEPHALON_CDC_SQLSERVER_CONNECTION_STRING` | Optional pre-provisioned SQL Server connection string. When present, SQL Server live tests should use it instead of Testcontainers. |
| `CEPHALON_CDC_POSTGRES_CONNECTION_STRING` | Optional pre-provisioned Postgres connection string. When present, Postgres live tests should use it instead of Testcontainers. |

Provider-specific live tests should prefer a pre-provisioned connection string when present, otherwise use Testcontainers only when both `CEPHALON_CDC_EXTERNAL_SERVICES` and `CEPHALON_CDC_TESTCONTAINERS` are enabled. If neither provider mode resolves, the test should remain skipped rather than silently passing with fake transport coverage.

## Run

```powershell
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```

To run the future SQL Server/Postgres live lane through Testcontainers:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```

To run the future live lane against pre-provisioned services:

```powershell
$env:CEPHALON_CDC_EXTERNAL_SERVICES = '1'
$env:CEPHALON_CDC_SQLSERVER_CONNECTION_STRING = 'Server=localhost;Database=cephalon_cdc;User Id=sa;Password=<password>;TrustServerCertificate=true'
$env:CEPHALON_CDC_POSTGRES_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=cephalon_cdc;Username=postgres;Password=<password>'
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```
