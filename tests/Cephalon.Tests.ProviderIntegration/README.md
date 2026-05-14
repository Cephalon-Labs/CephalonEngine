# Cephalon Provider Integration Tests

This project is the focused integration-test lane for provider-backed behavior that needs a real infrastructure runtime rather than an in-memory, fake, or composition-only proof.

## Current baseline

- Redis live provider coverage proves the `Cephalon.Data.Redis` outbox/inbox surfaces and the `Cephalon.EventSourcing.Redis` stream provider against one live Redis runtime.
- The Redis test verifies service registration, runtime capabilities, outbox and inbox descriptors, event-stream descriptors, Redis Hash/Sorted Set/Set/Stream persistence, idempotent outbox and inbox behavior, dispatch-store success reporting, ordered event replay, provider-durable snapshot save/load, snapshot-assisted managed replay, projection rebuild, runtime-surface durable-snapshot readback, stale snapshot rejection, and optimistic-concurrency rejection.
- MongoDB data-provider coverage starts a repo-owned disposable replica set and proves `Cephalon.Data.MongoDB` outbox/inbox/dispatch-store behavior in the default lane.
- Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, and Qdrant now have opt-in live data-provider proof lanes. Each lane can run against either pre-provisioned provider services or disposable Testcontainers-backed runtimes, composes `Cephalon.Engine`, `Cephalon.Eventing`, and the real provider pack, then proves manifest capabilities, outbox/inbox descriptors, `event-driven-integration` technology surfaces, idempotent outbox/inbox writes, and real provider persistence. All except ClickHouse also prove `IEventDispatchStore` pending/success transitions; ClickHouse deliberately proves the truthful `unsupported` dispatch policy.
- SMTP invitation delivery now has an opt-in live relay proof lane. It composes `Cephalon.MultiTenancy.Governance.SmtpDelivery`, dispatches through the real governance delivery dispatcher, hands the message to a real SMTP relay, reads the accepted message back through the relay API, and verifies message id, recipients, context headers, provider metadata, and sanitized runtime-surface truth.

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
| `CEPHALON_PROVIDER_CASSANDRA_CONTACT_POINTS` | Optional pre-provisioned Cassandra contact points; comma-separated contact-point hosts. Testcontainers mode uses a disposable `cassandra:4.1` service. |
| `CEPHALON_PROVIDER_CASSANDRA_PORT` | Optional Cassandra native transport port; defaults to `9042`. |
| `CEPHALON_PROVIDER_CASSANDRA_KEYSPACE` | Optional pre-provisioned Cassandra keyspace. The provider creates it on first use when the service account has permission. |
| `CEPHALON_PROVIDER_CLICKHOUSE_HOST` | Optional pre-provisioned ClickHouse HTTP host. Testcontainers mode uses a disposable `clickhouse/clickhouse-server:24.8-alpine` service. |
| `CEPHALON_PROVIDER_CLICKHOUSE_PORT` | Optional ClickHouse HTTP port; defaults to `8123`. |
| `CEPHALON_PROVIDER_CLICKHOUSE_DATABASE` | Optional pre-provisioned ClickHouse database. |
| `CEPHALON_PROVIDER_CLICKHOUSE_USERNAME` | Optional ClickHouse username; defaults to `default`. |
| `CEPHALON_PROVIDER_CLICKHOUSE_PASSWORD` | Optional ClickHouse password. |
| `CEPHALON_PROVIDER_ELASTICSEARCH_URI` | Optional pre-provisioned Elasticsearch endpoint URI. Testcontainers mode uses a disposable `docker.elastic.co/elasticsearch/elasticsearch:8.17.0` single-node service with security disabled. |
| `CEPHALON_PROVIDER_ELASTICSEARCH_USERNAME` | Optional Elasticsearch username. |
| `CEPHALON_PROVIDER_ELASTICSEARCH_PASSWORD` | Optional Elasticsearch password. |
| `CEPHALON_PROVIDER_NATS_URI` | Optional pre-provisioned NATS URI; the service must have JetStream enabled because the pack uses JetStream KV buckets. Testcontainers mode uses a disposable `nats:2.10-alpine` service with `-js`. |
| `CEPHALON_PROVIDER_NEO4J_URI` | Optional pre-provisioned Neo4j Bolt URI. Testcontainers mode uses a disposable `neo4j:5-community` service. |
| `CEPHALON_PROVIDER_NEO4J_USERNAME` | Required with `CEPHALON_PROVIDER_NEO4J_URI`; Testcontainers mode supplies `neo4j`. |
| `CEPHALON_PROVIDER_NEO4J_PASSWORD` | Required with `CEPHALON_PROVIDER_NEO4J_URI`; Testcontainers mode supplies a generated test password. |
| `CEPHALON_PROVIDER_OPENSEARCH_URI` | Optional pre-provisioned OpenSearch endpoint URI. Testcontainers mode uses a disposable `opensearchproject/opensearch:2.18.0` single-node service with security disabled. |
| `CEPHALON_PROVIDER_OPENSEARCH_USERNAME` | Optional OpenSearch username. |
| `CEPHALON_PROVIDER_OPENSEARCH_PASSWORD` | Optional OpenSearch password. |
| `CEPHALON_PROVIDER_QDRANT_HOST` | Optional pre-provisioned Qdrant gRPC host. Testcontainers mode uses a disposable `qdrant/qdrant:v1.12.5` service. |
| `CEPHALON_PROVIDER_QDRANT_PORT` | Optional Qdrant gRPC port; defaults to `6334`. |
| `CEPHALON_PROVIDER_QDRANT_API_KEY` | Optional Qdrant API key. |
| `CEPHALON_PROVIDER_SMTP_HOST` | Optional pre-provisioned SMTP relay host for the invitation-delivery live proof. Testcontainers mode uses a disposable `mailhog/mailhog:v1.0.1` service. |
| `CEPHALON_PROVIDER_SMTP_PORT` | Optional SMTP relay port; defaults to `1025`. |
| `CEPHALON_PROVIDER_SMTP_API_URI` | Required with `CEPHALON_PROVIDER_SMTP_HOST`; points at a MailHog-compatible HTTP API root used to verify accepted messages. |

Provider-specific live tests prefer pre-provisioned service settings when present, otherwise use Testcontainers only when both `CEPHALON_PROVIDER_EXTERNAL_SERVICES` and `CEPHALON_PROVIDER_TESTCONTAINERS` are enabled. If neither mode resolves, the provider test remains skipped rather than silently passing with fake transport coverage.

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
| Redis | `RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis` | Redis server must support Streams, Hashes, Sets, Sorted Sets, and Lua script execution. |
| SMTP | `SmtpDelivery_DispatchesInvitationThroughLiveRelay` | SMTP relay must accept unauthenticated mail, and a MailHog-compatible API must expose accepted messages. |

To run one provider lane, enable external services and filter by the provider method name:

```powershell
$env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = '1'
$env:CEPHALON_PROVIDER_NATS_URI = 'nats://localhost:4222'
dotnet test .\tests\Cephalon.Tests.ProviderIntegration\Cephalon.Tests.ProviderIntegration.csproj --no-restore --filter FullyQualifiedName~NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream --logger "console;verbosity=normal"
```

To run the non-relational provider live lanes through disposable Testcontainers:

```powershell
$env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = '1'
$env:CEPHALON_PROVIDER_TESTCONTAINERS = '1'
dotnet test .\tests\Cephalon.Tests.ProviderIntegration\Cephalon.Tests.ProviderIntegration.csproj --no-restore --filter FullyQualifiedName~LiveDataProviderIntegrationTests --logger "console;verbosity=normal"
```

For release-manager or CI execution, prefer the shared script so the provider matrix, Docker preflight, locked restore, test filters, and result directories stay consistent:

```powershell
.\scripts\run-provider-live-testcontainers.ps1 -Providers All -Configuration Release
.\scripts\run-provider-live-testcontainers.ps1 -Providers Nats -Configuration Release
.\scripts\run-provider-live-testcontainers.ps1 -Providers Redis -Configuration Release
.\scripts\run-provider-live-testcontainers.ps1 -Providers Smtp -Configuration Release
```

The scheduled/manual GitHub Actions lane, `.github/workflows/provider-live-testcontainers.yml`, runs the same script on `ubuntu-latest` with one matrix job per provider. It is intentionally separate from release validation so default CI remains deterministic and Docker-free, while Docker-capable runners can still prove Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, Qdrant, Redis, and SMTP delivery against real disposable provider services.

## Redis live lane

`RedisProviderIntegrationTests.RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis` proves the Redis data companion and Redis event-sourcing companion against a live Redis service, including the provider-durable snapshot lifecycle used by the core replay worker.

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
