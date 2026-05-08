# Cephalon Provider Integration Tests

This project is the focused integration-test lane for provider-backed behavior that needs a real infrastructure runtime rather than an in-memory, fake, or composition-only proof.

## Current baseline

- Redis live provider coverage proves the `Cephalon.Data.Redis` outbox/inbox surfaces and the `Cephalon.EventSourcing.Redis` stream provider against one live Redis runtime.
- The Redis test verifies service registration, runtime capabilities, outbox and inbox descriptors, event-stream descriptors, Redis Hash/Sorted Set/Set/Stream persistence, idempotent outbox and inbox behavior, dispatch-store success reporting, ordered event replay, and optimistic-concurrency rejection.
- MongoDB data-provider coverage starts a repo-owned disposable replica set and proves `Cephalon.Data.MongoDB` outbox/inbox/dispatch-store behavior in the default lane.
- Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, and Qdrant now have opt-in live data-provider proof lanes. Each lane composes `Cephalon.Engine`, `Cephalon.Eventing`, and the real provider pack, then proves manifest capabilities, outbox/inbox descriptors, `event-driven-integration` technology surfaces, idempotent outbox/inbox writes, and real provider persistence. All except ClickHouse also prove `IEventDispatchStore` pending/success transitions; ClickHouse deliberately proves the truthful `unsupported` dispatch policy.

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
| `CEPHALON_PROVIDER_CASSANDRA_CONTACT_POINTS` | Required for the Cassandra live lane; comma-separated contact-point hosts. |
| `CEPHALON_PROVIDER_CASSANDRA_PORT` | Optional Cassandra native transport port; defaults to `9042`. |
| `CEPHALON_PROVIDER_CASSANDRA_KEYSPACE` | Required Cassandra keyspace. The provider creates it on first use when the service account has permission. |
| `CEPHALON_PROVIDER_CLICKHOUSE_HOST` | Required ClickHouse HTTP host. |
| `CEPHALON_PROVIDER_CLICKHOUSE_PORT` | Optional ClickHouse HTTP port; defaults to `8123`. |
| `CEPHALON_PROVIDER_CLICKHOUSE_DATABASE` | Required ClickHouse database. |
| `CEPHALON_PROVIDER_CLICKHOUSE_USERNAME` | Optional ClickHouse username; defaults to `default`. |
| `CEPHALON_PROVIDER_CLICKHOUSE_PASSWORD` | Optional ClickHouse password. |
| `CEPHALON_PROVIDER_ELASTICSEARCH_URI` | Required Elasticsearch endpoint URI. |
| `CEPHALON_PROVIDER_ELASTICSEARCH_USERNAME` | Optional Elasticsearch username. |
| `CEPHALON_PROVIDER_ELASTICSEARCH_PASSWORD` | Optional Elasticsearch password. |
| `CEPHALON_PROVIDER_NATS_URI` | Required NATS URI; the service must have JetStream enabled because the pack uses JetStream KV buckets. |
| `CEPHALON_PROVIDER_NEO4J_URI` | Required Neo4j Bolt URI. |
| `CEPHALON_PROVIDER_NEO4J_USERNAME` | Required Neo4j username. |
| `CEPHALON_PROVIDER_NEO4J_PASSWORD` | Required Neo4j password. |
| `CEPHALON_PROVIDER_OPENSEARCH_URI` | Required OpenSearch endpoint URI. |
| `CEPHALON_PROVIDER_OPENSEARCH_USERNAME` | Optional OpenSearch username. |
| `CEPHALON_PROVIDER_OPENSEARCH_PASSWORD` | Optional OpenSearch password. |
| `CEPHALON_PROVIDER_QDRANT_HOST` | Required Qdrant gRPC host. |
| `CEPHALON_PROVIDER_QDRANT_PORT` | Optional Qdrant gRPC port; defaults to `6334`. |
| `CEPHALON_PROVIDER_QDRANT_API_KEY` | Optional Qdrant API key. |

Provider-specific live tests should prefer a pre-provisioned service setting when present, otherwise use Testcontainers only when both `CEPHALON_PROVIDER_EXTERNAL_SERVICES` and `CEPHALON_PROVIDER_TESTCONTAINERS` are enabled. Redis currently has a Testcontainers fallback; the newer Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, and Qdrant lanes intentionally require pre-provisioned service variables so the repo does not claim disposable provider support before it exists.

## Data-provider live lane

`LiveDataProviderIntegrationTests` proves the remaining non-relational data providers against real runtimes:

| Provider | Test filter token | Required runtime note |
| --- | --- | --- |
| Cassandra | `CassandraProvider_StagesOutboxInboxAndDispatchAgainstLiveService` | Native transport reachable and keyspace creation allowed or pre-created. |
| ClickHouse | `ClickHouseProvider_StagesOutboxAndInboxAgainstLiveService` | Database reachable over HTTP; dispatch store remains intentionally unsupported. |
| Elasticsearch | `ElasticsearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService` | Index creation and refresh must be allowed; the test polls eventually visible search rows. |
| NATS | `NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream` | JetStream and KV buckets must be enabled. |
| Neo4j | `Neo4jProvider_StagesOutboxInboxAndDispatchAgainstLiveService` | Bolt auth must allow label/constraint creation and node writes. |
| OpenSearch | `OpenSearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService` | Index creation and refresh must be allowed; the test polls eventually visible search rows. |
| Qdrant | `QdrantProvider_StagesOutboxInboxAndDispatchAgainstLiveService` | gRPC endpoint must be reachable and collection creation allowed. |

To run one provider lane, enable external services and filter by the provider method name:

```powershell
$env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = '1'
$env:CEPHALON_PROVIDER_NATS_URI = 'nats://localhost:4222'
dotnet test .\tests\Cephalon.Tests.ProviderIntegration\Cephalon.Tests.ProviderIntegration.csproj --no-restore --filter FullyQualifiedName~NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream --logger "console;verbosity=normal"
```

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
