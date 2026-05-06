# Cephalon CDC Integration Tests

This project is the focused integration-test lane for provider-backed CDC behavior that needs a real data-system runtime rather than an in-memory or fake transport.

## Current baseline

- MongoDB change streams run against a disposable single-node replica set through `MongoDbReplicaSetRunner` and `EphemeralMongo7`, so the baseline does not require Docker or a developer-managed MongoDB instance.
- The MongoDB test proves outbox staging, provider-native runtime binding, runtime-state reporting, execution-runtime aggregation, and durable checkpoint persistence through the same contracts exposed by `Cephalon.Data` and `Cephalon.Data.MongoDB`.
- SQL Server and Postgres live CDC tests are intentionally not in this first lane. Their current coverage remains in the fake transport harnesses under the hosting/support test projects until the repo has an explicit external-service gating convention.

## Run

```powershell
dotnet test .\tests\Cephalon.Tests.CdcIntegration\Cephalon.Tests.CdcIntegration.csproj --no-restore --logger "console;verbosity=normal"
```
