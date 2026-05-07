# Cephalon Provider Integration Tests

This project is the focused integration-test lane for provider-backed behavior that needs a real infrastructure runtime rather than an in-memory, fake, or composition-only proof.

## Current baseline

- Redis live provider coverage proves the `Cephalon.Data.Redis` outbox/inbox surfaces and the `Cephalon.EventSourcing.Redis` stream provider against one live Redis runtime.
- The Redis test verifies service registration, runtime capabilities, outbox and inbox descriptors, event-stream descriptors, Redis Hash/Sorted Set/Set/Stream persistence, idempotent outbox and inbox behavior, dispatch-store success reporting, ordered event replay, and optimistic-concurrency rejection.

## External-provider gate

The default test command must stay deterministic and must not require Docker, Testcontainers, Redis, or any developer-managed infrastructure. Tests that need external provider runtimes should use `ExternalProviderServiceFactAttribute` from `ExternalServices/` so the lane is discovered but skipped until a developer or CI job opts in.

The shared gate uses these environment variables:

| Variable | Purpose |
| --- | --- |
| `CEPHALON_PROVIDER_EXTERNAL_SERVICES` | Set to `1`, `true`, `yes`, or `on` to enable the external provider service lane. |
| `CEPHALON_PROVIDER_INTEGRATION` | Backward-friendly alias that also enables the external provider service lane. |
| `CEPHALON_PROVIDER_TESTCONTAINERS` | Set to `1`, `true`, `yes`, or `on` with `CEPHALON_PROVIDER_EXTERNAL_SERVICES=1` when provider tests should create disposable services through Testcontainers. |
| `CEPHALON_PROVIDER_REDIS_CONNECTION_STRING` | Optional pre-provisioned Redis connection string. When present, Redis live tests use it instead of Testcontainers. |
| `CEPHALON_REDIS_CONNECTION_STRING` | Alias for the pre-provisioned Redis connection string. |

Provider-specific live tests should prefer a pre-provisioned connection string when present, otherwise use Testcontainers only when both `CEPHALON_PROVIDER_EXTERNAL_SERVICES` and `CEPHALON_PROVIDER_TESTCONTAINERS` are enabled. If neither provider mode resolves, the test should remain skipped rather than silently passing with fake transport coverage.

## Redis live lane

`RedisProviderIntegrationTests.RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis` proves the Redis data companion and Redis event-sourcing companion against a live Redis service.

To run only the Redis live lane through Testcontainers:

```powershell
$env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = '1'
$env:CEPHALON_PROVIDER_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.ProviderIntegration\Cephalon.Tests.ProviderIntegration.csproj --no-restore --filter FullyQualifiedName~RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis --logger "console;verbosity=normal"
```

To run only the Redis live lane against a pre-provisioned service:

```powershell
$env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = '1'
$env:CEPHALON_PROVIDER_REDIS_CONNECTION_STRING = 'localhost:6379'
dotnet test .\tests\Cephalon.Tests.ProviderIntegration\Cephalon.Tests.ProviderIntegration.csproj --no-restore --filter FullyQualifiedName~RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis --logger "console;verbosity=normal"
```

## Run

```powershell
dotnet test .\tests\Cephalon.Tests.ProviderIntegration\Cephalon.Tests.ProviderIntegration.csproj --no-restore --logger "console;verbosity=normal"
```
