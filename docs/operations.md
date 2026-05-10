# Cephalon Operations

This document captures the current operational surface for Cephalon as of `April 29, 2026`.

For the active phase-2 follow-through inventory, see `docs/operational-hardening-gap-inventory.md`.

## Generated-app deployment smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, publishes the generated host, runs the published output, and validates `/health/ready`, `/engine`, `/engine/snapshot`, and `/scalar`, run:

```powershell
pwsh ./scripts/validate-generated-app-publish.ps1
```

See `docs/generated-app-publishing.md` for the corresponding publish-profile and published-output guidance.

## Generated-app cold-start adoption smoke

For a repo-native external-adoption replay that publishes a temporary package feed, installs `Cephalon.Cli`, runs `cephalon doctor`, scaffolds a fresh app outside the repository, seeds the generated local package feed, reruns `cephalon doctor --app-root`, restores, builds, runs the generated host, and validates `/health/ready`, `/engine`, `/engine/snapshot`, and `/scalar`, run:

```powershell
pwsh ./scripts/validate-generated-app-adoption.ps1
```

See `docs/getting-started.md` for the corresponding install, doctor, scaffold, seed, restore, and first-run guidance.

## Template-pack cold-start adoption smoke

For a repo-native external-adoption replay that publishes a temporary package feed, installs `Cephalon.Cli`, installs `Cephalon.TemplatePack` into an isolated custom hive, reruns `cephalon doctor` with that custom hive visible, scaffolds a fresh `dotnet new cephalon-monolith` starter outside the repository, seeds the generated local package feed, reruns `cephalon doctor --app-root`, restores, builds, runs the generated host, and validates `/health/ready`, `/engine`, `/engine/snapshot`, and `/scalar`, run:

```powershell
pwsh ./scripts/validate-template-pack-adoption.ps1
```

See `docs/getting-started.md` for the corresponding template-pack install, doctor, scaffold, and first-run guidance.

## Out-of-tree package parity smoke

For a repo-native external-adoption replay that publishes a temporary package feed with the generated app package closure including `Cephalon.Behaviors.SourceGen`, `Cephalon.Diagnostics`, and `Cephalon.Resilience`, installs `Cephalon.Cli`, scaffolds a fresh app outside the repository, packs and stages `Cephalon.ReferenceModule.Operations` through `cephalon package stage`, patches `Engine:Discovery:PackageDirectories` plus `Engine:PackagePolicy` and `Engine:Trust`, reruns `cephalon doctor --app-root`, restores, builds, runs the generated host, and validates `/api/operations/status`, `/engine/packages`, `/engine/package-policy`, `/engine/trust-policy`, and `/engine/snapshot`, run:

```powershell
pwsh ./scripts/validate-out-of-tree-package-adoption.ps1
```

The script writes `artifacts/adoption-smoke/out-of-tree-package-adoption.json` by default; pass `-ReportPath <path>` when a release run needs the report in a different artifact folder. The release-readiness scorecard validates the same replay path and execution-report contract through `scripts/adoption-smoke-support.json` and emits it as `AdoptionSmokeEvidence` in the generated scorecard artifact. Keep that manifest aligned whenever this smoke path adds or removes script phases, staged packages, runtime probes, or report fields.

See `docs/external-package-lifecycle.md` for the corresponding stage, trust, and inspect guidance.

## Signed package governance smoke

For a repo-native external-adoption replay that publishes a temporary package feed, installs `Cephalon.Cli`, scaffolds a fresh app outside the repository, repacks `Cephalon.ReferenceModule.Operations` with a deterministic detached signature, stages the signed package through `cephalon package stage`, patches `Engine:Discovery:PackageDirectories` plus stricter `Engine:PackagePolicy` and `Engine:Trust:TrustedSignaturePublicKeys`, reruns `cephalon doctor --app-root`, restores, builds, runs the generated host, validates `/api/operations/status`, `/engine/packages`, `/engine/package-policy`, `/engine/trust-policy`, and `/engine/snapshot`, then proves the same host rejects a tampered signed package when signature verification is required, run:

```powershell
pwsh ./scripts/validate-signed-package-governance.ps1
```

For the matching certificate-chain trust replay, run:

```powershell
pwsh ./scripts/validate-signed-package-certificate-chain-governance.ps1
```

That lane reuses the same external-adoption path but patches `Engine:Trust:TrustedSignatureCertificates` plus `Engine:Trust:TrustedSignatureCertificateAuthorities`, then proves `/engine/packages`, `/engine/trust-policy`, and `/engine/snapshot` expose `trusted-certificate-chain` verification plus the signing `certificateThumbprint`.

See `docs/external-package-lifecycle.md` for the corresponding detached-signature and trust-governance guidance.

## Generated-app Windows Service smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, publishes the generated host, and previews the shipped Windows Service install/remove scripts against the published output without requiring admin rights, run:

```powershell
pwsh ./scripts/validate-generated-app-windows-service.ps1
```

See `docs/windows-service-deployment.md` for the corresponding Windows install, verify, and removal guidance.

## Generated-app IIS smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, publishes the generated host, verifies the SDK-generated `web.config`, and previews the shipped IIS install/remove scripts against the published output without requiring admin rights or a live IIS install, run:

```powershell
pwsh ./scripts/validate-generated-app-iis.ps1
```

See `docs/iis-deployment.md` for the corresponding Windows hosted-IIS install, verify, and removal guidance.

## Generated-app Azure App Service smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, publishes the generated host, packages it into the shipped Azure ZIP artifact, and previews the Azure CLI deploy contract without contacting Azure, run:

```powershell
pwsh ./scripts/validate-generated-app-app-service.ps1
```

See `docs/azure-app-service-deployment.md` for the corresponding Azure ZIP deploy, `WEBSITE_RUN_FROM_PACKAGE`, and `az webapp deploy` guidance.

## Generated-app container-image smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, previews the shipped build/push contract, builds the generated Docker image, and by default proves push through a local Docker registry, run:

```powershell
pwsh ./scripts/validate-generated-app-container-image.ps1
```

See `docs/container-image-publishing.md` for the corresponding provider-neutral image build/tag/push guidance.

## Generated-app Azure Container Apps smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, validates the generated Dockerfile locally, and previews the shipped Azure Container Apps source-deploy contract without contacting Azure, run:

```powershell
pwsh ./scripts/validate-generated-app-container-apps.ps1
```

See `docs/azure-container-apps-deployment.md` for the corresponding `az containerapp up --source` guidance.

## Generated-app Kubernetes smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, validates the generated Dockerfile locally, and previews the shipped Kubernetes manifest/apply contract without contacting a live cluster, run:

```powershell
pwsh ./scripts/validate-generated-app-kubernetes.ps1
```

See `docs/kubernetes-deployment.md` for the corresponding `kubectl kustomize`, namespace, service, and probe guidance.

## Generated-app Linux systemd smoke

For a repo-native adoption replay that scaffolds a fresh app, seeds the repo-local package feed, publishes the generated host, and verifies the shipped Linux `systemd` unit under WSL `systemd-analyze`, run:

```powershell
pwsh ./scripts/validate-generated-app-systemd.ps1
```

See `docs/linux-systemd-deployment.md` for the corresponding Linux install, verify, and `systemctl` guidance.

## Health surfaces

ASP.NET Core hosts that call `app.MapCephalon()` now expose three health routes:

- `/health`
- `/health/live`
- `/health/ready`

Health responses are JSON and include the check status, duration, and runtime-specific details such as restart count and the most recent failure context.
When modules or installed packages register `IDependencyHealthContributor`, those responses also include dependency details. `Cephalon.Observability.CassandraDependencies`, `Cephalon.Observability.ClickHouseDependencies`, `Cephalon.Observability.ConsulDependencies`, `Cephalon.Observability.ElasticsearchDependencies`, `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.KafkaDependencies`, `Cephalon.Observability.MemcachedDependencies`, `Cephalon.Observability.MongoDbDependencies`, `Cephalon.Observability.MqttDependencies`, `Cephalon.Observability.MySqlDependencies`, `Cephalon.Observability.NatsDependencies`, `Cephalon.Observability.Neo4jDependencies`, `Cephalon.Observability.OpenSearchDependencies`, `Cephalon.Observability.OracleDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, `Cephalon.Observability.RedisDependencies`, and `Cephalon.Observability.SqlServerDependencies` are the shipped companion packages for turning external upstreams into that dependency-health surface.

Current semantics:

- liveness stays `Healthy` while the process is alive, even during startup and shutdown transitions
- liveness becomes `Unhealthy` when the runtime enters `Failed` or `Stopped`
- liveness becomes `Degraded` when the runtime is live but one or more dependencies report degraded or unhealthy status
- readiness is `Healthy` only when the runtime reaches `Started`
- readiness is `Unhealthy` during startup, shutdown, stopped, and failed states so traffic can stay off the host until the runtime is actually ready
- readiness becomes `Unhealthy` when a required dependency reports `Unhealthy`
- readiness becomes `Degraded` when only optional dependencies are degraded or unhealthy, or when required dependencies are degraded without fully failing

Optional tuning through `Engine:FailurePolicy`:

- `StartupReadinessDelay` keeps readiness `Unhealthy` for a bounded warmup window after startup succeeds
- `ShutdownLivenessGracePeriod` keeps liveness `Healthy` while shutdown drains, then flips to `Unhealthy` if the drain window expires before stop completes
- `ManualRestartBackoff` delays explicit `RestartAsync(...)` calls after restartable startup failures
- health payloads expose those lifecycle windows through `activeWindow`, `activeWindowEndsAtUtc`, and `restartAvailableAtUtc` when applicable

## Dependency surface

`GET /engine/dependencies` exposes the dependency-health snapshot currently contributed to the runtime.

This keeps dependency visibility separate from the aggregate health routes:

- `/engine/dependencies` answers "what dependencies are currently reporting?"
- `/health/live` answers "is the process live?"
- `/health/ready` answers "is the runtime ready to take traffic with its current dependency state?"

### HTTP dependency probes

`Cephalon.Observability.HttpDependencies` reads `Engine:Observability:DependencyHealth:Http` and turns configured external HTTP endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Http": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "catalog-api",
              "DisplayName": "Catalog API",
              "Endpoint": "https://catalog.example.com/health",
              "Method": "GET",
              "Required": true,
              "TimeoutSeconds": 5,
              "ExpectedStatusCodes": [200]
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);
```

Operational notes:

- the HTTP dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state
- if `ExpectedStatusCodes` is omitted, standard successful HTTP responses are treated as healthy

### Consul dependency probes

`Cephalon.Observability.ConsulDependencies` reads `Engine:Observability:DependencyHealth:Consul` and turns configured Consul endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Consul": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "service-discovery",
              "DisplayName": "Service Discovery",
              "Endpoint": "https://consul.internal.example:8501",
              "AclToken": "${CONSUL_HTTP_TOKEN}",
              "Datacenter": "ops-dc",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonConsulDependencyHealth(builder.Configuration);
```

Operational notes:

- the Consul dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- if the configured endpoint is just the Consul base URL, the probe automatically resolves `GET /v1/status/leader`
- probes support `X-Consul-Token` ACL headers plus optional datacenter selection through the `dc` query parameter
- a non-empty leader response maps to `Healthy`; an empty leader response maps to `Unhealthy`
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Elasticsearch dependency probes

`Cephalon.Observability.ElasticsearchDependencies` reads `Engine:Observability:DependencyHealth:Elasticsearch` and turns configured Elasticsearch endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Elasticsearch": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "search-cluster",
              "DisplayName": "Search Cluster",
              "Endpoint": "https://search.internal.example:9200",
              "ApiKey": "${ELASTICSEARCH_API_KEY}",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonElasticsearchDependencyHealth(builder.Configuration);
```

Operational notes:

- the Elasticsearch dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- if the configured endpoint is just the cluster base URL, the probe automatically resolves `GET /_cluster/health`
- probes support API-key auth, bearer-token auth, or basic auth without hiding those choices in host code
- cluster `green` maps to `Healthy`, `yellow` maps to `Degraded`, `red` maps to `Unhealthy`, and Elasticsearch-side `timed_out` responses are treated as `Unhealthy`
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### ClickHouse dependency probes

`Cephalon.Observability.ClickHouseDependencies` reads `Engine:Observability:DependencyHealth:ClickHouse` and turns configured ClickHouse endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "ClickHouse": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "analytics-clickhouse",
              "DisplayName": "Analytics ClickHouse",
              "Host": "analytics.internal.example",
              "Protocol": "https",
              "Port": 8443,
              "Database": "analytics",
              "Username": "cephalon-runtime",
              "Password": "${CLICKHOUSE_PASSWORD}",
              "HealthQuery": "SELECT 1",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonClickHouseDependencyHealth(builder.Configuration);
```

Operational notes:

- the ClickHouse dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full connection string or discrete `Host` / `Protocol` / `Port` / `Database` settings so HTTP transport choices stay explicit
- each probe opens a dedicated ClickHouse connection, runs the configured SQL health query, and reports the result through the shared dependency-health contract
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### OpenSearch dependency probes

`Cephalon.Observability.OpenSearchDependencies` reads `Engine:Observability:DependencyHealth:OpenSearch` and turns configured OpenSearch endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "OpenSearch": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "catalog-search",
              "DisplayName": "Catalog Search",
              "Endpoint": "https://search.internal.example:9200",
              "Index": "catalog-items",
              "BearerToken": "${OPENSEARCH_BEARER_TOKEN}",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonOpenSearchDependencyHealth(builder.Configuration);
```

Operational notes:

- the OpenSearch dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- if the configured endpoint is just the cluster base URL, the probe automatically resolves `GET /_cluster/health`, and it can append an index-specific path when `Index` is configured
- probes support bearer-token auth or basic auth without hiding those choices in host code
- cluster `green` maps to `Healthy`, `yellow` maps to `Degraded`, `red` maps to `Unhealthy`, and OpenSearch-side `timed_out` or missing-cluster-manager responses are treated as `Unhealthy`
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Redis dependency probes

`Cephalon.Observability.RedisDependencies` reads `Engine:Observability:DependencyHealth:Redis` and turns configured Redis endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Redis": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "shared-cache",
              "DisplayName": "Shared Redis Cache",
              "Host": "redis.internal.example",
              "Port": 6379,
              "Required": true,
              "TimeoutSeconds": 5,
              "Username": "cephalon-runtime",
              "Password": "${REDIS_PASSWORD}",
              "Database": 2
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonRedisDependencyHealth(builder.Configuration);
```

Operational notes:

- the Redis dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- each probe opens a TCP connection, optionally authenticates, optionally selects a logical database, and then verifies `PING` -> `PONG`
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Memcached dependency probes

`Cephalon.Observability.MemcachedDependencies` reads `Engine:Observability:DependencyHealth:Memcached` and turns configured Memcached endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Memcached": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "session-cache",
              "DisplayName": "Session Cache",
              "Host": "memcached.internal.example",
              "Port": 11211,
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonMemcachedDependencyHealth(builder.Configuration);
```

Operational notes:

- the Memcached dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- each probe opens a TCP connection and verifies that the Memcached `version` command returns a valid `VERSION ...` response
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Postgres dependency probes

`Cephalon.Observability.PostgresDependencies` reads `Engine:Observability:DependencyHealth:Postgres` and turns configured Postgres endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Postgres": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "catalog-db",
              "DisplayName": "Catalog Database",
              "Host": "postgres.internal.example",
              "Port": 5432,
              "Database": "catalog",
              "Username": "cephalon-runtime",
              "Password": "${POSTGRES_PASSWORD}",
              "SslMode": "Require",
              "HealthQuery": "SELECT 1;",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonPostgresDependencyHealth(builder.Configuration);
```

Operational notes:

- the Postgres dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full `ConnectionString` or discrete host/port/database settings
- each probe opens a dedicated `Npgsql` connection with pooling disabled, runs the configured health query, and reports the result through the shared dependency-health contract
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### RabbitMQ dependency probes

`Cephalon.Observability.RabbitMqDependencies` reads `Engine:Observability:DependencyHealth:RabbitMq` and turns configured RabbitMQ endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "RabbitMq": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "events-broker",
              "DisplayName": "Events Broker",
              "Host": "rabbitmq.internal.example",
              "Port": 5671,
              "VirtualHost": "/operations",
              "Username": "cephalon-runtime",
              "Password": "${RABBITMQ_PASSWORD}",
              "UseTls": true,
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonRabbitMqDependencyHealth(builder.Configuration);
```

Operational notes:

- the RabbitMQ dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full AMQP `ConnectionString` or discrete host/port/vhost settings
- each probe opens a dedicated AMQP connection with auto-recovery disabled so the result reflects the current broker reachability and authentication state
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Kafka dependency probes

`Cephalon.Observability.KafkaDependencies` reads `Engine:Observability:DependencyHealth:Kafka` and turns configured Kafka clusters into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Kafka": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "events-kafka",
              "DisplayName": "Events Kafka",
              "BootstrapServers": "kafka-1.internal.example:9093,kafka-2.internal.example:9093",
              "ClientId": "cephalon-runtime",
              "Topic": "cephalon.events",
              "SecurityProtocol": "SaslSsl",
              "SaslMechanism": "ScramSha512",
              "Username": "cephalon-runtime",
              "Password": "${KAFKA_PASSWORD}",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonKafkaDependencyHealth(builder.Configuration);
```

Operational notes:

- the Kafka dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- each probe requests broker metadata from the configured `BootstrapServers` list and can optionally verify that a specific `Topic` is present in returned metadata
- `SecurityProtocol` and `SaslMechanism` stay explicit in configuration so hosts can declare plaintext, TLS, or SASL broker expectations without hiding them in host code
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### SQL Server dependency probes

`Cephalon.Observability.SqlServerDependencies` reads `Engine:Observability:DependencyHealth:SqlServer` and turns configured SQL Server endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "SqlServer": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "orders-sql",
              "DisplayName": "Orders SQL",
              "Host": "sql.internal.example",
              "Port": 1433,
              "Database": "orders",
              "Username": "cephalon-runtime",
              "Password": "${SQL_PASSWORD}",
              "Encrypt": "Mandatory",
              "TrustServerCertificate": false,
              "HealthQuery": "SELECT 1;",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonSqlServerDependencyHealth(builder.Configuration);
```

Operational notes:

- the SQL Server dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full `ConnectionString` or discrete host/port/database settings
- each probe opens a dedicated `SqlConnection` with pooling disabled, runs the configured health query, and reports the result through the shared dependency-health contract
- optional `Encrypt` and `TrustServerCertificate` settings let hosts keep SQL Server or Azure SQL transport expectations explicit instead of hidden in host-specific code
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### MySQL dependency probes

`Cephalon.Observability.MySqlDependencies` reads `Engine:Observability:DependencyHealth:MySql` and turns configured MySQL endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "MySql": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "catalog-mysql",
              "DisplayName": "Catalog MySQL",
              "Host": "mysql.internal.example",
              "Port": 3306,
              "Database": "catalog",
              "Username": "cephalon-runtime",
              "Password": "${MYSQL_PASSWORD}",
              "SslMode": "Required",
              "AllowPublicKeyRetrieval": false,
              "HealthQuery": "SELECT 1;",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonMySqlDependencyHealth(builder.Configuration);
```

Operational notes:

- the MySQL dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full `ConnectionString` or discrete host/port/database settings
- each probe opens a dedicated `MySqlConnection` with pooling disabled, runs the configured health query, and reports the result through the shared dependency-health contract
- optional `SslMode` and `AllowPublicKeyRetrieval` settings let hosts keep MySQL transport and authentication expectations explicit instead of hidden in host-specific code
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Oracle dependency probes

`Cephalon.Observability.OracleDependencies` reads `Engine:Observability:DependencyHealth:Oracle` and turns configured Oracle Database endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Oracle": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "orders-oracle",
              "DisplayName": "Orders Oracle",
              "Host": "oracle.internal.example",
              "Port": 1521,
              "ServiceName": "ORDERSPDB",
              "Username": "cephalon-runtime",
              "Password": "${ORACLE_PASSWORD}",
              "HealthQuery": "SELECT 1 FROM DUAL",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonOracleDependencyHealth(builder.Configuration);
```

Operational notes:

- the Oracle dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full `ConnectionString` or discrete host/port/service-name settings
- each probe opens a dedicated `OracleConnection` with pooling disabled, runs the configured health query, and reports the result through the shared dependency-health contract
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Cassandra dependency probes

`Cephalon.Observability.CassandraDependencies` reads `Engine:Observability:DependencyHealth:Cassandra` and turns configured Cassandra clusters into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Cassandra": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "orders-cassandra",
              "DisplayName": "Orders Cassandra",
              "ContactPoints": [
                "cass-a.internal.example",
                "cass-b.internal.example"
              ],
              "Port": 9042,
              "Keyspace": "orders",
              "Username": "cephalon-runtime",
              "Password": "${CASSANDRA_PASSWORD}",
              "HealthQuery": "SELECT release_version FROM system.local;",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonCassandraDependencyHealth(builder.Configuration);
```

Operational notes:

- the Cassandra dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes accept `ContactPoints` as either an array or a comma-separated scalar value so hosts can keep cluster seed-node configuration explicit
- each probe opens a dedicated Cassandra session, optionally selects a keyspace, runs the configured CQL health query, and reports the result through the shared dependency-health contract
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### NATS dependency probes

`Cephalon.Observability.NatsDependencies` reads `Engine:Observability:DependencyHealth:Nats` and turns configured NATS endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Nats": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "events-nats",
              "DisplayName": "Events NATS",
              "Host": "nats.internal.example",
              "Port": 4222,
              "Token": "${NATS_TOKEN}",
              "ClientName": "cephalon-runtime",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonNatsDependencyHealth(builder.Configuration);
```

Operational notes:

- the NATS dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- each probe opens a TCP connection, reads the initial server `INFO` line, sends a NATS `CONNECT` payload, and verifies a `PING` -> `PONG` round-trip
- probes support token-based auth or username/password auth, and TLS stays explicit through `UseTls` and `TlsServerName`
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### Neo4j dependency probes

`Cephalon.Observability.Neo4jDependencies` reads `Engine:Observability:DependencyHealth:Neo4j` and turns configured Neo4j graph endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Neo4j": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "orders-graph",
              "DisplayName": "Orders Graph",
              "Uri": "neo4j://graph.internal.example:7687",
              "Database": "orders",
              "Username": "cephalon-runtime",
              "Password": "${NEO4J_PASSWORD}",
              "HealthQuery": "RETURN 1 AS health",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonNeo4jDependencyHealth(builder.Configuration);
```

Operational notes:

- the Neo4j dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full `Uri` or discrete `Host` / `Port` / `Scheme` settings, so routing and TLS semantics stay explicit through standard Neo4j schemes such as `neo4j`, `neo4j+s`, `bolt`, or `bolt+s`
- each probe opens a dedicated Neo4j driver session, optionally selects a database, runs the configured Cypher health query, and reports the result through the shared dependency-health contract
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### MQTT dependency probes

`Cephalon.Observability.MqttDependencies` reads `Engine:Observability:DependencyHealth:Mqtt` and turns configured MQTT endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "Mqtt": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "edge-mqtt",
              "DisplayName": "Edge MQTT",
              "Host": "mqtt.internal.example",
              "Port": 8883,
              "UseTls": true,
              "TlsServerName": "mqtt.internal.example",
              "ClientId": "cephalon-runtime",
              "Username": "cephalon-runtime",
              "Password": "${MQTT_PASSWORD}",
              "KeepAliveSeconds": 30,
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonMqttDependencyHealth(builder.Configuration);
```

Operational notes:

- the MQTT dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- each probe opens a TCP connection, optionally upgrades to TLS, sends an MQTT 3.1.1 `CONNECT`, validates `CONNACK`, and verifies a `PINGREQ` -> `PINGRESP` round-trip
- probes keep username/password auth, client identifier, keep-alive interval, and TLS server-name expectations explicit in configuration instead of hiding them in host code
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

### MongoDB dependency probes

`Cephalon.Observability.MongoDbDependencies` reads `Engine:Observability:DependencyHealth:MongoDb` and turns configured MongoDB endpoints into reusable `IDependencyHealthContributor` data.

Example:

```json
{
  "Engine": {
    "Observability": {
      "DependencyHealth": {
        "MongoDb": {
          "RefreshIntervalSeconds": 30,
          "Dependencies": [
            {
              "Id": "catalog-mongodb",
              "DisplayName": "Catalog MongoDB",
              "Host": "mongo.internal.example",
              "Port": 27017,
              "Database": "catalog",
              "Username": "cephalon-runtime",
              "Password": "${MONGODB_PASSWORD}",
              "AuthSource": "admin",
              "UseTls": true,
              "AllowInsecureTls": false,
              "DirectConnection": true,
              "HealthCommand": "ping",
              "Required": true,
              "TimeoutSeconds": 5
            }
          ]
        }
      }
    }
  }
}
```

Registration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonMongoDbDependencyHealth(builder.Configuration);
```

Operational notes:

- the MongoDB dependency-health package is optional and stays outside `Cephalon.Engine`
- the package performs one probe refresh during host startup and then refreshes in the background on the configured interval
- probes can use either a full `ConnectionString` or discrete host/port/database settings
- each probe creates a dedicated `MongoClient`, runs the configured health command against the selected database, and reports the result through the shared dependency-health contract
- optional `UseTls`, `AllowInsecureTls`, and `DirectConnection` settings let hosts keep MongoDB topology and transport expectations explicit instead of hidden in host-specific code
- required dependency failures pull readiness to `Unhealthy`; optional failures degrade readiness/liveness without hiding the runtime state

## Diagnostics surface

`GET /engine/diagnostics` exposes the engine's operational conventions in one place:

- `meterName`
- `activitySourceName`
- counter names used by the runtime
- execution-graph and hosted-execution transition counters alongside runtime, module, failure, and restart counters
- published diagnostics conventions and event-id catalogs for the active engine and companion packages
- the current liveness report
- the current readiness report
- dependency details folded into those runtime reports
- the mapped health routes

Current shipped event-id ranges include:

- `Cephalon.Engine`: `2000-2005`
- `Cephalon.MultiTenancy`: `4500-4502`
- `Cephalon.MultiTenancy.Governance`: `4510-4549`, `4552-4557`
- `Cephalon.MultiTenancy.Governance.HttpDelivery`: `4550-4551`
- `Cephalon.MultiTenancy.Governance.MailgunDelivery`: `4566-4567`
- `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore`: `4568-4571`
- `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`: `4572-4573`
- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`: `4576-4577`
- `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore`: `4578-4584`
- `Cephalon.MultiTenancy.Governance.SendGridDelivery`: `4560-4561`
- `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore`: `4562-4565`
- `Cephalon.MultiTenancy.Governance.SmtpDelivery`: `4558-4559`
- `Cephalon.Observability`: `3000-3006`
- `Cephalon.Observability.Gcp`: `3111-3111`
- `Cephalon.Observability.HuaweiCloud`: `3112-3112`
- `Cephalon.Observability.AlibabaCloud`: `3113-3113`
- `Cephalon.Observability.GrafanaCloud`: `3119-3119`
- `Cephalon.Observability.NewRelic`: `3154-3154`
- `Cephalon.Observability.Kubernetes`: `3117-3117`
- `Cephalon.Observability.OpenShift`: `3114-3114`
- `Cephalon.Observability.DigitalOcean`: `3115-3115`
- `Cephalon.Observability.Tanzu`: `3116-3116`
- `Cephalon.Observability.CassandraDependencies`: `3146-3147`
- `Cephalon.Observability.ConsulDependencies`: `3142-3143`
- `Cephalon.Observability.ElasticsearchDependencies`: `3138-3139`
- `Cephalon.Observability.HttpDependencies`: `3100-3101`
- `Cephalon.Observability.RedisDependencies`: `3120-3121`
- `Cephalon.Observability.PostgresDependencies`: `3122-3123`
- `Cephalon.Observability.RabbitMqDependencies`: `3124-3125`
- `Cephalon.Observability.SqlServerDependencies`: `3126-3127`
- `Cephalon.Observability.MySqlDependencies`: `3128-3129`
- `Cephalon.Observability.MongoDbDependencies`: `3130-3131`
- `Cephalon.Observability.KafkaDependencies`: `3132-3133`
- `Cephalon.Observability.NatsDependencies`: `3134-3135`
- `Cephalon.Observability.MqttDependencies`: `3136-3137`
- `Cephalon.Observability.MemcachedDependencies`: `3140-3141`
- `Cephalon.Observability.OracleDependencies`: `3144-3145`
- `Cephalon.Observability.Neo4jDependencies`: `3148-3149`
- `Cephalon.Observability.OpenSearchDependencies`: `3150-3151`
- `Cephalon.Observability.ClickHouseDependencies`: `3152-3153`

This is the quickest way to discover the engine's observability contract without opening code.

## Runtime story surface

`GET /engine/runtime-story` exposes the operator-facing lifecycle narrative in one place:

- the current runtime status and last failure context
- loaded package metadata for the active runtime
- per-execution-graph lifecycle state, including loaded, active, and deactivated timestamps
- per-hosted-execution lifecycle state, including loaded, active, and deactivated timestamps
- per-module lifecycle state, including loaded, initialized, started, and stopped timestamps
- an ordered timeline for package load, execution-graph transitions, hosted-execution transitions, module transitions, runtime transitions, restart attempts, and failures

This is the quickest way to answer the adjacent operational question that `/engine/status`, `/engine/packages`, `/engine/hosted-executions`, `/engine/diagnostics`, and `/engine/snapshot` already support in pieces: what loaded, what started, what failed, and why.

## Execution graph surface

`GET /engine/execution-graphs` exposes the operator-facing execution-graph catalog contributed by active modules.

Current payload highlights:

- each graph carries a stable `id`, `displayName`, `description`, `sourceModuleId`, and `entryNodeId`
- `nodes` can point back to module ids and capability keys so orchestration descriptors stay grounded in the existing runtime contract
- `edges` expose the directed graph transitions plus optional labels or routing conditions
- the same execution-graph catalog is also available through `/engine/snapshot` when operators want one merged runtime answer
- `/engine/runtime-story` now shows when each graph became load-visible, active, or inactive with the runtime lifecycle

Current note:

- this is a descriptive orchestration baseline, not a hosted workflow runner yet
- invalid graph ids, node references, module references, or capability references fail at build time instead of leaking broken operator data

## Hosted execution surface

`GET /engine/hosted-executions` exposes the operator-facing hosted or background execution catalog contributed by active modules.

Current payload highlights:

- each hosted execution carries a stable `id`, `displayName`, `description`, `sourceModuleId`, and `kind`
- `executionGraphId` can point back to one execution graph when the hosted/background surface drives a published workflow directly
- `startsWithHost` makes the intended host lifecycle relationship explicit for operators without inventing a separate engine-owned runner abstraction
- the same hosted-execution catalog is also available through `/engine/snapshot` when operators want one merged runtime answer
- `/engine/runtime-story` now shows when each hosted execution became load-visible, active, or inactive with the runtime lifecycle

Current note:

- hosted executions are descriptive operator-facing conventions on top of the existing Generic Host and module lifecycle model, not a replacement for `IHostedService`, `BackgroundService`, or module-owned runtime hooks
- invalid hosted-execution ids, unknown source modules, or unknown cross-module execution-graph references fail at build time instead of leaking broken operator data

## REST runtime surfaces

Cephalon now keeps the full REST publication and governance story introspectable through a linked
set of runtime endpoints:

- `GET /engine/rest-endpoints` exposes the final published REST endpoint catalog that ASP.NET Core
  actually mapped after shorthand authoring, authoring-policy filtering, suppression, and override
  selection completed
- `GET /engine/rest-endpoint-candidates` exposes the richer candidate catalog behind that final
  answer, including original shorthand or module-DSL source shape, authoring style, grouped
  publication ownership, original endpoint metadata, matched rule ids, and final selected/applied
  governance results
- `GET /engine/rest-endpoint-publication-groups` exposes the grouped publication story per
  behavior and per authoring style so operators can see which styles published, which candidates
  were suppressed, which explicit groups stayed authoritative, and which host rules were skipped
  because the targeted route group never opted into host governance
- `GET /engine/rest-endpoint-authoring-policies` exposes behavior-level shorthand authoring-policy
  intent such as preferred style, allowed/disallowed styles, and multiple-published-candidate
  answers without making that grouped summary itself another authoring input
- `GET /engine/rest-endpoint-suppressions` exposes the active suppression-rule catalog together
  with matched, suppressed, and skipped candidate outcomes plus grouped selection-basis summaries
- `GET /engine/rest-endpoint-overrides` exposes the active override-rule catalog together with
  matched, applied, and skipped candidate outcomes plus grouped selection-basis and
  declared-versus-effective action summaries

Current payload highlights:

- the final `/engine/rest-endpoints` answer keeps `CandidateId`, `PublicationGroupId`,
  `AuthoringStyle`, source behavior/module ownership, and original endpoint metadata lineage
  visible so operators can jump back to the candidate that produced the published route
- behavior-backed REST entries also keep stable ownership metadata visible: publication activation
  and profile metadata are `application-managed`, materialization/runtime catalog truth is
  `cephalon-managed`, and `restPublicationActivationMode` records whether the route came from
  explicit module DSL, `MapProfile<TBehavior>()`, generated profile shorthand, or an inline helper
- `HostGovernanceScopes`, `EndpointNames`, original explicit `TargetBindings`, route group
  prefixes, OpenAPI document names, tag names, HTTP methods, and effective API major versions are
  all part of the governable selector story across candidate, suppression, override, publication
  group, and snapshot answers
- explicit module DSL routes stay authoritative by default, but when a group uses
  `WithHostGovernanceScope(...)` the scope still becomes visible for future host targeting even
  before `AllowHostGovernance()` opts that route into suppressions or overrides
- if a host rule wins selection but does not materially change the final published answer, the
  runtime keeps the winning rule visible through `SelectedOverrideId` plus
  `OverrideSelectionBasis` while leaving `AppliedOverrideId = null`; grouped runtime answers keep
  the same selected-versus-applied truth so no-op governance does not look like a published change
- the same REST publication, candidate, governance, and grouped summaries also flow through
  `/engine/snapshot` so operators can choose one merged runtime answer without losing REST-specific
  troubleshooting detail

Current note:

- these surfaces describe the governable runtime truth for REST publication, not a second
  authoring system; module code and host configuration still remain the only authoring inputs
- non-REST transports stay out of these REST catalogs and continue to surface through their own
  transport-specific runtime contracts

## Multi-tenancy technology surfaces

When `MultiTenancy` is selected and `Cephalon.MultiTenancy` is registered, `GET /engine/technology-surfaces` and `GET /engine/snapshot` expose the active tenant runtime answer. When `Cephalon.MultiTenancy.Governance` is also registered, the same surfaces include concrete tenant-governance proofs.

Current payload highlights:

- `tenant-resolution` reports configured tenant count, configured tenant ids, default tenant id, domain-resolution posture, tenant-key posture, resolver count, ambient-context accessor count, and the enabled resolution strategies
- `tenant-governance-boundaries` reports the shipped `tenant-resolution-core` as `cephalon-managed`, marks tenant membership, tenant invitations, tenant administration, declared tenant-domain ownership, and approval/remediation governance actions as shipped companion-owned lanes that require `Cephalon.MultiTenancy.Governance` registration, and keeps broader onboarding, delivery, synchronization, and endpoint/UI workflows outside those owned lanes as `taxonomy-only` boundary entries
- `tenant-memberships` reports the governance companion's Cephalon-managed membership catalog, store, and evaluation posture, including membership count, tenant count, contributor count, configured membership count, runtime membership count, membership-store kind, membership-store durability, membership-store ownership, durable-store ownership, evaluation ownership, per-tenant status counts, role summaries, principal-kind breakdowns, and contributing module ids without exposing individual principal identifiers
- `tenant-invitations` reports the governance companion's Cephalon-managed invitation catalog, store, validation, delivery-dispatch, opt-in delivery retry queue, process-local retry execution coordination, delivery-status reconciliation, and delivery-status observation-store posture, including invitation count, tenant count, contributor count, configured invitation count, runtime invitation count, invitation-store kind, invitation-store durability, invitation-store ownership, durable-store ownership, validation ownership, retry queue enablement/ownership/kind/durability/scope/counts/latest retry outcome, delivery retry max attempts/delay/max items, retry execution coordination enablement/ownership/scope/mode/in-progress state/counts/latest outcome/timestamps, delivery status reconciliation ownership, observation-store kind/durability/scope/history limit/count/latest observation, external delivery status ownership, delivery status reported count/latest status/latest observed timestamp, delivery sender count/ids/ownership, external delivery ownership, delivery run counts/latest outcome, status breakdown, per-tenant retry counts, per-tenant status counts, role summaries, invitee-kind breakdowns, and contributing module ids without exposing individual invitee identifiers. Installing `Cephalon.MultiTenancy.Governance.HttpDelivery` registers a first-party `http-webhook` sender that appears in the same sender readiness and run-history metadata instead of creating a separate runtime surface; signed sends record only safe `httpSigned` plus optional `httpSigningKeyId` metadata, idempotent sends record safe `httpIdempotencyKey`/`httpIdempotencyKeySource` metadata, and retrying sends record safe attempt/retry metadata such as `httpAttemptCount`, `httpMaxAttempts`, `httpRetried`, and `httpRetryReason`. Installing `Cephalon.MultiTenancy.Governance.SmtpDelivery` registers a first-party `smtp-email` sender that appears in the same sender readiness and run-history metadata; SMTP sends record only safe relay host/port/TLS/message-id/recipient metadata and never record SMTP credentials or message bodies. Installing `Cephalon.MultiTenancy.Governance.SendGridDelivery` registers a first-party `sendgrid-email` sender on the same dispatcher; SendGrid sends record only safe endpoint/status/sandbox/Cephalon-message-id/recipient/category/custom-argument-count metadata and never record the SendGrid API key or message bodies. Installing `Cephalon.MultiTenancy.Governance.MailgunDelivery` registers a first-party `mailgun-email` sender on the same dispatcher; Mailgun sends record only safe endpoint/status/test-mode/domain/Cephalon-message-id/recipient/tag/variable/header-count metadata and never record the Mailgun API key or message bodies. Installing `Cephalon.MultiTenancy.Governance.AmazonSesDelivery` registers a first-party `amazon-ses-email` sender on the same dispatcher; Amazon SES sends record only safe region/configuration-set/status/message-id/recipient/tag metadata and never record AWS credentials, raw SDK payloads, or message bodies. Installing `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery` registers a first-party `microsoft-graph-email` sender on the same dispatcher; Graph sends record only safe endpoint/status/request-id/category/header/recipient metadata and never record bearer tokens, authorization headers, raw payloads, or message bodies.
- `tenant-administration` reports the governance companion's Cephalon-managed host-driven administration workflow posture, including workflow enablement, membership and invitation administration ownership, membership/invitation store kinds, durability, counts, supported commands, and core-package boundaries for public onboarding, host-adapter endpoint ownership, invitation delivery dispatch, invitation delivery status reconciliation, provider-specific sender ownership, external delivery/status ownership, and identity-provider sync
- `tenant-administration-http-endpoints` reports the ASP.NET Core governance adapter's optional command endpoint posture, including route pattern, `POST` method, mapped/configured/disabled state, authorization requirement, optional policy, endpoint-description visibility, and application-managed boundaries for public onboarding, tenant-admin UI, provider-specific invitation senders, external invitation delivery, and identity-provider sync
- `tenant-invitation-delivery-status-http-endpoints` reports the ASP.NET Core governance adapter's optional normalized callback and bounded observation-read endpoint posture, including callback route pattern, `POST` method, mapped/configured/disabled state, authorization requirement, optional policy, provider-message-match enforcement, endpoint-description visibility, provider-neutral callback signature verification configuration, safe signature header names, signing key-id configuration, timestamp tolerance, signed-callback replay protection policy/key/scope/durability/retention/cache-limit posture, observation read route pattern, `GET` method, authorization posture, response contract, default/max limits, summary ownership/scope/dimensions, attention ownership/categories, provider-message filter ownership/scope, remediation-filter ownership/scope, remediation-hint ownership/actions, and application-managed boundaries for provider-specific callback inboxes, provider-specific payload translation, provider-specific signature verification, provider polling, and distributed remediation execution
- `tenant-invitation-delivery-sendgrid-status-callbacks` reports the optional SendGrid ASP.NET Core callback translator's route/auth/body-limit/event-limit/translation/signature/replay/event-id-idempotency posture, including safe signed-webhook verification metadata, bounded process-local signed-callback replay policy/key/scope/durability/retention/cache-limit truth, and observation-store-backed `sg_event_id` duplicate-skip truth without exposing SendGrid public keys, raw signatures, recipient emails, or raw payload bodies
- `tenant-invitation-delivery-mailgun-status-callbacks` reports the optional Mailgun ASP.NET Core callback translator's route/auth/body-limit/event-limit/translation/signature/replay/event-id-idempotency posture, including explicit `not-configured` signature/replay/idempotency truth, bounded process-local token replay metadata, observation-store-backed `event-data.id` duplicate-skip truth, and application-managed inbox/polling boundaries without exposing recipient emails or raw payload bodies
- `tenant-domain-ownership` reports the governance companion's Cephalon-managed declared domain-ownership catalog, store, validation, in-process verification-workflow, proof-challenge issuance, proof-publication planning, HTTP file proof-publication state, proof-evaluation, HTTP file proof-collection, configured DNS TXT proof-collection, proof-verification runner, on-demand proof-polling runner, and opt-in background proof-polling posture, including domain ownership count, tenant count, contributor count, configured domain count, runtime domain ownership count, domain-ownership-store kind, domain-ownership-store durability, domain-ownership-store ownership, durable-store ownership, validation ownership, verification-workflow ownership, proof-challenge issuance ownership, proof-publication planning ownership, HTTP proof-publication ownership, published HTTP proof count, proof-evaluation ownership, HTTP proof-collection ownership, DNS TXT proof-collection enablement, DNS TXT resolver configuration, proof-verification runner ownership, external proof-polling ownership, background proof-polling ownership, background polling interval/batch/run counts/latest outcome, DNS/provider proof-publication ownership, status breakdown, verification-method breakdown, and contributing module ids without exposing individual domain names
- `tenant-governance-actions` reports the governance companion's Cephalon-managed approval/remediation action catalog, decision, in-process workflow, and action-store posture, including action count, tenant count, contributor count, configured action count, runtime action count, decision ownership, workflow execution ownership, action-store kind, action-store durability, action-store ownership, durable-store ownership, notification-delivery ownership, status breakdown, action-kind breakdown, subject-kind breakdown, and contributing module ids without exposing individual action metadata
- boundary entries outside the current membership, invitation, tenant-administration, declared domain-ownership, and governance-action proofs still carry `plannedOwnership = companion-planned`, `basePackageOwnership = not-owned`, and `suggestedPackage = Cephalon.MultiTenancy.Governance` so operators can distinguish current runtime ownership from planned companion work

Current note:

- the base package owns tenant resolution and ambient tenant context only
- the governance companion currently owns tenant membership cataloging/evaluation, opt-in local durable membership state, tenant invitation cataloging/validation, opt-in local durable invitation state, host-agnostic invitation delivery dispatch/run-state/outcome persistence over registered sender extensions, opt-in local invitation delivery retry storage plus bounded retry execution, process-local retry execution coordination, and opt-in automatic background retry scheduling over retryable sender failures, host-agnostic invitation delivery status reconciliation over provider or receiver observations, opt-in local durable delivery-status observation storage for normalized reconciliation records, host-driven tenant-administration workflow commands over membership and invitation stores, declared tenant-domain ownership cataloging/validation, opt-in local durable domain-ownership state, in-process tenant-domain ownership verification workflow transitions, domain proof challenge issuance, domain proof publication planning, HTTP file proof publication state for host adapters, domain proof evaluation over reported evidence, on-demand HTTP file proof collection, configured on-demand DNS TXT proof collection through an explicit DNS-over-HTTPS resolver, domain proof verification runner orchestration, bounded on-demand domain proof polling, opt-in automatic background proof polling scheduling/run-state, approval/remediation action cataloging/decision, in-process approval/remediation action workflow transitions, and opt-in local durable action state
- ASP.NET Core hosts can install `Cephalon.MultiTenancy.Governance.AspNetCore`, call `MapCephalonTenantDomainOwnershipHttpProofs()` to serve published HTTP proof files from the governance catalog, call `MapCephalonTenantAdministrationCommands()` to expose `POST /engine/tenant-administration/commands` over `TenantAdministrationWorkflowRequest`, call `MapCephalonTenantInvitationDeliveryDispatches()` to expose fail-closed `POST /engine/tenant-invitations/delivery-dispatches` dispatch actions over `ITenantInvitationDeliveryDispatcher`, call `MapCephalonTenantInvitationDeliveryStatusCallbacks()` to expose `POST /engine/tenant-invitations/delivery-status` over normalized `TenantInvitationDeliveryStatusCallbackRequest` payloads with optional provider-neutral HMAC verification plus bounded process-local signed replay rejection, and call `MapCephalonTenantInvitationDeliveryStatusObservations()` to expose bounded/filterable `GET /engine/tenant-invitations/delivery-status/observations` reads plus filtered rollup summaries, attention-category drill-downs, provider-message drill-downs, remediation-action filters, and remediation hints over `ITenantInvitationDeliveryStatusObservationStore`
- hosts can install `Cephalon.MultiTenancy.Governance.HttpDelivery` and call `AddCephalonHttpInvitationDelivery(...)` when invitation dispatch should POST a generic JSON payload to a configured webhook, include a receiver-facing idempotency key, optionally sign that exact body with HMAC-SHA256, retry transient webhook outcomes within a bounded in-process attempt budget, and still record outcome truth through the governance dispatcher
- hosts can install `Cephalon.MultiTenancy.Governance.SmtpDelivery` and call `AddCephalonSmtpInvitationDelivery(...)` when invitation dispatch should hand a templated email to a configured SMTP relay through a replaceable client seam while still recording outcome truth through the governance dispatcher
- hosts can install `Cephalon.MultiTenancy.Governance.SendGridDelivery` and call `AddCephalonSendGridInvitationDelivery(...)` when invitation dispatch should POST a templated Mail Send API payload to SendGrid through a replaceable client seam while still recording outcome truth through the governance dispatcher
- hosts can install `Cephalon.MultiTenancy.Governance.MailgunDelivery` and call `AddCephalonMailgunInvitationDelivery(...)` when invitation dispatch should POST a templated multipart Messages API payload to Mailgun through a replaceable client seam while still recording outcome truth through the governance dispatcher
- hosts can install `Cephalon.MultiTenancy.Governance.AmazonSesDelivery` and call `AddCephalonAmazonSesInvitationDelivery(...)` when invitation dispatch should send a templated Amazon SES v2 `SendEmail` request through a replaceable AWS SDK handoff seam while still recording outcome truth through the governance dispatcher
- hosts can install `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery` and call `AddCephalonMicrosoftGraphInvitationDelivery(...)` when invitation dispatch should POST a templated Microsoft Graph `sendMail` payload through a replaceable client and access-token provider seam while still recording outcome truth through the governance dispatcher
- ASP.NET Core hosts can install `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` and call `MapCephalonSendGridInvitationDeliveryStatusCallbacks()` when SendGrid Event Webhook arrays should be translated into the existing delivery-status reconciler without custom host glue; when `RequireSignedEventWebhook` is enabled, the same endpoint can reject duplicate verified signed callbacks inside a bounded process-local replay window
- ASP.NET Core hosts can install `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` and call `MapCephalonMailgunInvitationDeliveryStatusCallbacks()` when Mailgun webhook objects should be translated, HMAC-verified, protected against duplicate signed tokens inside a bounded process-local replay window, and de-duplicated by observed Mailgun event id without custom host glue; durable inboxing, distributed replay, and distributed event-id ledgers remain separate follow-through
- ASP.NET Core hosts can install `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` and call `MapCephalonAmazonSesInvitationDeliveryStatusCallbacks()` when Amazon SNS `Notification` envelopes carrying Amazon SES event publishing payloads should be verified, protected against bounded process-local replay, de-duplicated by observed SNS message id, optionally confirmed for verified `SubscriptionConfirmation` envelopes, and translated into the existing delivery-status reconciler without custom host glue; SNS topic/subscription creation, durable inboxing, distributed replay, and distributed event-id ledgers remain separate follow-through
- hosts can enable `EnableInvitationDeliveryRetryQueue` when `sender-failed` dispatch outcomes should be retained for an explicit `ITenantInvitationDeliveryRetryRunner.RetryPendingAsync(...)` pass; configure `InvitationDeliveryRetryQueueFilePath` only when the local retry queue should survive process restarts
- the tenant-administration command endpoint is fail-closed by default; keep `RequireTenantAdministrationAuthorization = true` for real hosts, set `TenantAdministrationAuthorizationPolicy` when a named ASP.NET Core policy should guard the command surface, and disable authorization only for deliberate internal/test hosts
- the delivery status callback and observation read endpoints are fail-closed by default; keep `RequireTenantInvitationDeliveryStatusCallbackAuthorization = true` and `RequireTenantInvitationDeliveryStatusObservationAuthorization = true` for real hosts, set the related authorization policy when a named ASP.NET Core policy should guard callback ingress or observation reads, keep `RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch = true` unless the host deliberately owns another correlation boundary, and keep provider-neutral signed callback replay protection enabled when `TenantInvitationDeliveryStatusCallbackSigningSecret` is configured
- the SendGrid callback endpoint is also fail-closed by default; keep `RequireStatusCallbackAuthorization = true` for real hosts, enable `RequireSignedEventWebhook` with a SendGrid public key when callbacks should be verified before parsing, and keep `EnableSignedEventWebhookReplayProtection = true` unless the host has a stronger replay boundary outside Cephalon
- actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed membership/invitation/domain/action-store backends, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider invitation senders, distributed retry queues, provider-specific or distributed callback inboxes, cross-node callback replay protection, provider-specific delivery-status callback payload translation beyond shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature verification beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, identity-provider synchronization, public onboarding, and tenant-admin UI/backoffice flows remain future companion work until a package owns those paths explicitly

Tenant-administration command endpoint configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "AspNetCore": {
          "EnableTenantAdministrationCommandEndpoint": true,
          "TenantAdministrationCommandRoutePattern": "/engine/tenant-administration/commands",
          "RequireTenantAdministrationAuthorization": true,
          "TenantAdministrationAuthorizationPolicy": "tenant-admin",
          "ExcludeTenantAdministrationEndpointFromDescription": true
        }
      }
    }
  }
}
```

Tenant-invitation delivery retry queue configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "EnableInvitationDeliveryRetryQueue": true,
        "InvitationDeliveryRetryMaxAttempts": 3,
        "InvitationDeliveryRetryDelaySeconds": 300,
        "InvitationDeliveryRetryMaxItems": 25,
        "EnableInvitationDeliveryRetryExecutionCoordination": true,
        "EnableInvitationDeliveryRetryBackgroundScheduling": true,
        "InvitationDeliveryRetryBackgroundIntervalSeconds": 300,
        "InvitationDeliveryRetryBackgroundRunOnStartup": true,
        "InvitationDeliveryRetryBackgroundSource": "background-invitation-delivery-retry",
        "InvitationDeliveryRetryQueueFilePath": "App_Data/cephalon/invitation-delivery-retries.json"
      }
    }
  }
}
```

When the retry queue is enabled, retryable `sender-failed` dispatch outcomes are queued with a stable tenant/invitation/channel/sender boundary and can be replayed by calling `ITenantInvitationDeliveryRetryRunner.RetryPendingAsync(...)`. Process-local execution coordination is enabled by default for the retry runner; an overlapping manual/background pass returns `already-running` and leaves the retry queue untouched instead of dispatching the same local entries concurrently. Background scheduling remains disabled by default; set `EnableInvitationDeliveryRetryBackgroundScheduling` deliberately when the current process should run the same bounded retry runner on startup and on the configured interval. This stays a local process scheduler with a process-local overlap guard, not a distributed queue, cross-node lease, provider-specific sender, or exactly-once delivery system.

Tenant-invitation delivery status callback endpoint configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "AspNetCore": {
          "EnableTenantInvitationDeliveryStatusCallbackEndpoint": true,
          "TenantInvitationDeliveryStatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status",
          "RequireTenantInvitationDeliveryStatusCallbackAuthorization": true,
          "TenantInvitationDeliveryStatusCallbackAuthorizationPolicy": "tenant-delivery-status",
          "ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription": true,
          "RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch": true,
          "TenantInvitationDeliveryStatusCallbackSigningSecret": "${TENANT_INVITATION_STATUS_CALLBACK_SECRET}",
          "TenantInvitationDeliveryStatusCallbackSigningKeyId": "primary-2026-04",
          "TenantInvitationDeliveryStatusCallbackSignatureHeaderName": "X-Cephalon-Callback-Signature",
          "TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName": "X-Cephalon-Callback-Signature-Timestamp",
          "TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName": "X-Cephalon-Callback-Key-Id",
          "TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds": 300,
          "EnableTenantInvitationDeliveryStatusCallbackReplayProtection": true,
          "TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds": 300,
          "TenantInvitationDeliveryStatusCallbackReplayCacheLimit": 4096,
          "EnableTenantInvitationDeliveryStatusObservationEndpoint": true,
          "TenantInvitationDeliveryStatusObservationRoutePattern": "/engine/tenant-invitations/delivery-status/observations",
          "RequireTenantInvitationDeliveryStatusObservationAuthorization": true,
          "TenantInvitationDeliveryStatusObservationAuthorizationPolicy": "tenant-delivery-status-observations",
          "ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription": true,
          "TenantInvitationDeliveryStatusObservationDefaultLimit": 100,
          "TenantInvitationDeliveryStatusObservationMaxLimit": 500
        }
      }
    }
  }
}
```

HTTP invitation delivery configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "HttpInvitationDelivery": {
          "Enabled": true,
          "SenderId": "http-webhook",
          "Endpoint": "https://notifications.internal.example/invitations",
          "Method": "POST",
          "TimeoutSeconds": 10,
          "MaxAttempts": 3,
          "RetryDelayMilliseconds": 250,
          "RetryStatusCodes": [408, 429, 500, 502, 503, 504],
          "RetryTransportFailures": true,
          "EnableIdempotencyHeader": true,
          "IdempotencyHeaderName": "X-Cephalon-Idempotency-Key",
          "IdempotencyMetadataKey": "idempotencyKey",
          "ExpectedStatusCodes": [202],
          "SupportedChannels": ["email", "webhook"],
          "SigningSecret": "${INVITATION_DELIVERY_SIGNING_SECRET}",
          "SigningKeyId": "primary-2026-04",
          "SignatureHeaderName": "X-Cephalon-Webhook-Signature",
          "SignatureTimestampHeaderName": "X-Cephalon-Webhook-Signature-Timestamp",
          "SignatureKeyIdHeaderName": "X-Cephalon-Webhook-Key-Id",
          "ProviderMessageIdHeaderName": "X-Cephalon-Provider-Message-Id",
          "Headers": {
            "X-Delivery-Key": "${INVITATION_DELIVERY_KEY}"
          }
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonHttpInvitationDelivery(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Operational notes:

- `ExpectedStatusCodes` is optional; when omitted, any successful 2xx response is accepted as dispatched
- unsupported channels return `suppressed` and are still recorded as provider-managed sender decisions
- keep `EnableIdempotencyHeader` enabled when receivers should de-duplicate retry attempts; callers can pass the configured `IdempotencyMetadataKey` in dispatch metadata to supply their own stable header-safe key, overlong or header-unsafe metadata values are deterministically hashed, and otherwise the sender derives a hashed key from tenant id, invitation id, channel, and sender id
- set `SigningSecret` when receivers should verify the webhook body; the sender signs `{unixTimestamp}.{jsonBody}` with HMAC-SHA256, sends the signature as `v1=<lowercase hex>`, and records only `httpSigned` plus optional `httpSigningKeyId` metadata
- set `MaxAttempts` above 1 when a host wants bounded in-process retry; the sender retries non-accepted `RetryStatusCodes` and, when `RetryTransportFailures` is true, transient `HttpRequestException` failures using the fixed `RetryDelayMilliseconds` delay inside the configured timeout budget
- failed HTTP status codes, timeouts, and transport exceptions return `sender-failed`; response bodies are not copied into metadata unless `IncludeResponseBodyInMetadata` is enabled and then only up to `ResponseBodyMetadataLimit`; retry metadata records attempts and retry reasons without copying secrets or webhook signatures
- custom headers and signing secrets are added as configured, so hosts should source secrets from their normal configuration provider and avoid committing raw delivery credentials

### SMTP invitation delivery sender

Install `Cephalon.MultiTenancy.Governance.SmtpDelivery` when invitation dispatch should hand a templated email to an SMTP relay instead of posting a webhook.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "SmtpInvitationDelivery": {
          "Enabled": true,
          "SenderId": "smtp-email",
          "Host": "smtp.internal.example",
          "Port": 587,
          "UseSsl": true,
          "UserName": "${SMTP_USER}",
          "Password": "${SMTP_PASSWORD}",
          "FromAddress": "noreply@example.com",
          "FromDisplayName": "Example SaaS",
          "RecipientAddressMetadataKey": "email",
          "SupportedChannels": ["email"],
          "MessageIdDomain": "mail.example.com",
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "Headers": {
            "X-Product": "Example SaaS"
          }
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonSmtpInvitationDelivery(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Operational notes:

- recipient email resolution checks dispatch metadata first, invitation metadata second, and finally `InviteeId` when `InviteeKind` is `email`
- `ISmtpInvitationDeliveryClient` is replaceable, so test hosts or provider-specific wrappers can reuse the same Cephalon sender contract without changing the governance dispatcher
- deterministic SMTP `Message-Id` values are derived from tenant id, invitation id, channel, and sender id so retry attempts for the same dispatch boundary can be correlated safely
- sender metadata records relay host, port, TLS posture, message id, sender id, recipient address, recipient metadata key, and safe client metadata, but it does not record SMTP username, password, message bodies, or unsafe headers
- this package owns SMTP relay handoff only; Mailgun Messages API handoff lives in `Cephalon.MultiTenancy.Governance.MailgunDelivery`, SendGrid Mail Send handoff lives in `Cephalon.MultiTenancy.Governance.SendGridDelivery`, Amazon SES v2 handoff lives in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`, and Microsoft Graph `sendMail` handoff lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`, while additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS, chat, CRM, identity-provider onboarding, bounce handling, provider polling, distributed retry queues, callback inboxes, and tenant-admin UI remain future provider-pack or application-owned work

### Amazon SES invitation delivery sender

Install `Cephalon.MultiTenancy.Governance.AmazonSesDelivery` when invitation dispatch should hand a templated email to Amazon SES v2 instead of posting a generic webhook, using SMTP directly, or using another dedicated email API provider.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "AmazonSesInvitationDelivery": {
          "Enabled": true,
          "SenderId": "amazon-ses-email",
          "RegionSystemName": "us-east-1",
          "ConfigurationSetName": "cephalon-invitations",
          "FromEmail": "noreply@example.com",
          "FromName": "Example SaaS",
          "ReplyToAddresses": ["support@example.com"],
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Tags": {
            "source": "tenant-invitation"
          },
          "AcceptedStatusCodes": [200]
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonAmazonSesInvitationDelivery(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Operational notes:

- recipient email resolution checks dispatch metadata first, invitation metadata second, and finally `InviteeId` when `InviteeKind` is `email`
- `IAmazonSesInvitationDeliveryClient` is replaceable, so test hosts, gateway wrappers, or custom AWS SDK policies can reuse the same Cephalon sender contract without changing the governance dispatcher
- the default client uses the AWS SDK for .NET SES v2 `SendEmail` API and accepts a successful `200 OK` response with a SES `MessageId` as dispatched handoff truth
- AWS credentials, IAM policy, verified identities, DKIM/SPF/DMARC, sandbox exit, account-level suppression policy, and SES configuration-set event destinations stay with the host/AWS account rather than `Cephalon.Engine`
- sender metadata records region, configuration set, SES status code, SES message id, Cephalon message id, sender id, recipient email, recipient metadata key, reply-to count, tag count, and safe client metadata, but it does not record AWS credentials, raw SDK request bodies, or message bodies
- this package owns Amazon SES v2 accepted handoff only; Amazon SES over SNS callback translation, opt-in SNS signature verification, bounded process-local SNS replay protection, observation-store-backed SNS message-id idempotency, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation live in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore`, while SNS topic/subscription creation, SES event destination setup, automatic resubscribe/restore, subscription lifecycle governance, provider polling, durable callback inboxes, distributed replay/event-id ledgers, deliverability analytics, SMS, chat, CRM, identity-provider onboarding, distributed retry queues, and tenant-admin UI remain future provider-pack or application-owned work

### Amazon SES invitation delivery status callbacks

Install `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` when Amazon SES event publishing sends SNS HTTP notifications and the host wants those events translated into the existing invitation delivery-status reconciler.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "AmazonSesInvitationDelivery": {
          "AspNetCore": {
            "EnableStatusCallbackEndpoint": true,
            "StatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status/amazon-ses",
            "RequireStatusCallbackAuthorization": true,
            "StatusCallbackAuthorizationPolicy": "amazon-ses-sns",
            "RequireProviderMessageMatch": true,
            "RecordStatus": true,
            "Source": "amazon-ses-sns",
            "Actor": "amazon-ses",
            "MaxRequestBodyBytes": 262144,
            "MaxEventsPerRequest": 1000,
            "MapEngagementEventsAsDelivered": false,
            "AcceptRawSesEventPayloads": true,
            "RequireSnsSignatureVerification": true,
            "RequireSnsSignatureVersion2": true,
            "RequireAllowedSnsTopicArn": true,
            "AllowedSnsTopicArns": [
              "arn:aws:sns:us-east-1:123456789012:cephalon-governance"
            ],
            "ValidateSnsSigningCertificateChain": true,
            "EnableSnsReplayProtection": true,
            "SnsReplayRetentionSeconds": 300,
            "SnsReplayCacheLimit": 4096,
            "EnableSnsMessageIdIdempotency": true,
            "EnableSnsSubscriptionConfirmation": false,
            "SnsSubscriptionConfirmationTimeoutSeconds": 10,
            "EnableSnsUnsubscribeConfirmationObservation": false
          }
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonAmazonSesInvitationDeliveryAspNetCore(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});

var app = builder.Build();
app.MapCephalonAmazonSesInvitationDeliveryStatusCallbacks();
```

Operational notes:

- the endpoint parses bounded SNS `Notification` JSON, unwraps the SES event JSON in `Message`, and can accept raw SES event objects or arrays for controlled replay/test harness scenarios when `AcceptRawSesEventPayloads` stays enabled
- when `RequireSnsSignatureVerification` is enabled, the endpoint rejects raw SES replay payloads, requires an allowed `TopicArn` by default, requires SNS `SignatureVersion` 2 by default, validates the HTTPS Amazon SNS signing-certificate URL, validates the certificate unless a host deliberately disables chain validation for pinned test certificates, and verifies the RSA signature before mapping or reconciliation
- when `EnableSnsReplayProtection` is enabled with SNS signature verification, the endpoint stores a bounded process-local fingerprint derived from the verified `TopicArn` plus `MessageId` and rejects duplicate verified callbacks with `409 Conflict` before reconciliation
- when `EnableSnsMessageIdIdempotency` is enabled with the governance observation store, the endpoint checks the normalized `amazon-ses-sns:{MessageId}` observation id before reconciliation, skips already observed SNS messages with `duplicateEvents`, and reports diagnostic `4581`; durability follows the configured observation store and is not a distributed event-id ledger
- when `EnableSnsSubscriptionConfirmation` is enabled with SNS signature verification, the endpoint confirms only verified SNS `SubscriptionConfirmation` envelopes from allowed topics through the replaceable `IAmazonSesSnsSubscriptionConfirmationClient`; the built-in client performs a bounded no-redirect `GET` to a trusted HTTPS Amazon SNS `SubscribeURL`, returns confirmation aggregate fields, and emits diagnostics `4582` or `4583`
- when `EnableSnsUnsubscribeConfirmationObservation` is enabled with SNS signature verification, the endpoint observes only verified SNS `UnsubscribeConfirmation` envelopes from allowed topics, validates the trusted HTTPS Amazon SNS `SubscribeURL`, returns unsubscribe-confirmation aggregate fields, emits diagnostic `4584`, and never invokes `SubscribeURL`
- the translator extracts Cephalon context from SES `mail.tags`, including tenant id, invitation id, channel, sender id, and correlation id, and uses `mail.messageId` as the provider message id captured by the SES sender
- `Send` maps to `accepted`, `Delivery` maps to `delivered`, transient `Bounce` maps to `deferred`, other `Bounce` maps to `bounced`, `Complaint` and `Reject` map to `suppressed`, `Rendering Failure` maps to `failed`, and `DeliveryDelay` maps to `deferred`; `Open`, `Click`, and `Subscription` are skipped by default
- `PinnedSnsSigningCertificatePem` can be used for controlled tests or deliberate certificate pinning; production hosts usually leave it unset so the endpoint retrieves the AWS SNS signing certificate from the validated `SigningCertURL`
- `UnsubscribeConfirmation` stays skipped unless unsubscribe-confirmation observation is enabled; automatic topic/subscription provisioning, SES event-destination setup, automatic resubscribe/restore, token storage, and subscription lifecycle governance remain outside this package
- the runtime surface reports whether SNS signature verification is required, whether signature version 2 and topic allow-listing are required, how many allowed topic ARNs are configured, whether pinned certificate or chain validation paths are active, whether process-local replay protection is configured, whether observation-store-backed SNS message-id idempotency is configured, whether verified subscription confirmation is configured, and whether verified unsubscribe-confirmation observation is configured
- keep endpoint authorization, gateway policy, SNS topic policy, private networking, or AWS WAF controls in place as defense in depth; SNS topic/subscription setup, topic policies, automatic resubscribe/restore, subscription lifecycle governance, durable inboxes, distributed replay ledgers, distributed event-id ledgers, and provider polling remain separate follow-through

### Mailgun invitation delivery sender

Install `Cephalon.MultiTenancy.Governance.MailgunDelivery` when invitation dispatch should hand a templated email to the Mailgun Messages API instead of posting a generic webhook, using SMTP directly, or using SendGrid.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "MailgunInvitationDelivery": {
          "Enabled": true,
          "SenderId": "mailgun-email",
          "BaseUrl": "https://api.mailgun.net",
          "DomainName": "mg.example.com",
          "ApiKey": "${MAILGUN_API_KEY}",
          "FromEmail": "noreply@example.com",
          "FromName": "Example SaaS",
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Tags": ["cephalon-invitation"],
          "Variables": {
            "product": "example-saas"
          },
          "Headers": {
            "X-Product": "Example SaaS"
          },
          "EnableTestMode": false,
          "ProviderMessageIdJsonPropertyName": "id",
          "AcceptedStatusCodes": [200]
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonMailgunInvitationDelivery(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Operational notes:

- recipient email resolution checks dispatch metadata first, invitation metadata second, and finally `InviteeId` when `InviteeKind` is `email`
- set `BaseUrl` to `https://api.eu.mailgun.net` for Mailgun EU regional sending; the package always posts to `/v3/{DomainName}/messages`
- `IMailgunInvitationDeliveryClient` is replaceable, so test hosts, gateway wrappers, or custom HTTP policies can reuse the same Cephalon sender contract without changing the governance dispatcher
- the sender carries deterministic Cephalon message ids through Mailgun `v:*` user variables and safe `h:*` headers, and captures the JSON `id` response property as the provider message id by default
- `EnableTestMode` adds `o:testmode=yes` so Mailgun processes the request without delivering to recipients
- sender metadata records endpoint host, Mailgun status code, configured domain, test-mode posture, Cephalon message id, sender id, recipient email, recipient metadata key, tag count, variable count, header count, and safe client metadata, but it does not record the API key, authorization header, raw request body, or message bodies
- this package owns Mailgun Messages API handoff only; Mailgun webhook callback translation, optional HMAC signed-webhook verification, bounded process-local replay-token rejection, and observation-store-backed event-id idempotency live in `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore`, Amazon SES v2 handoff lives in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`, Microsoft Graph `sendMail` handoff lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`, while additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS, chat, CRM, identity-provider onboarding, bounce handling, provider polling, distributed retry queues, callback inboxes, and tenant-admin UI remain future provider-pack or application-owned work

### Mailgun invitation delivery status callbacks

`Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` translates Mailgun webhook payloads into the existing tenant-invitation delivery-status reconciler. It is optional and separate from the outbound Mailgun sender so hosts can choose whether Mailgun should call back into the application.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "MailgunInvitationDelivery": {
          "AspNetCore": {
            "EnableStatusCallbackEndpoint": true,
            "StatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status/mailgun",
            "RequireStatusCallbackAuthorization": true,
            "StatusCallbackAuthorizationPolicy": "mailgun-webhook",
            "ExcludeStatusCallbackEndpointFromDescription": true,
            "RequireProviderMessageMatch": true,
            "RecordStatus": true,
            "Source": "mailgun-webhook",
            "Actor": "mailgun",
            "MaxRequestBodyBytes": 262144,
            "MaxEventsPerRequest": 1000,
            "MapEngagementEventsAsDelivered": false,
            "NormalizeProviderMessageIdWithAngleBrackets": true,
            "RequireSignedWebhook": true,
            "WebhookSigningKey": "${MAILGUN_WEBHOOK_SIGNING_KEY}",
            "SignedWebhookSignatureToleranceSeconds": 300,
            "AcceptParentSignature": true,
            "EnableSignedWebhookReplayProtection": true,
            "SignedWebhookReplayRetentionSeconds": 300,
            "SignedWebhookReplayCacheLimit": 4096,
            "EnableWebhookEventIdIdempotency": true
          }
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonMailgunInvitationDelivery(builder.Configuration);
builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});

var app = builder.Build();
app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();
```

Operational notes:

- Mailgun posts webhook payloads as JSON event objects; the endpoint also accepts JSON arrays for controlled replay and test harness scenarios, with bounded body size and event count
- the outbound sender stores Cephalon context in Mailgun `v:*` user variables; callback events without `cephalonTenantId` and `cephalonInvitationId` are skipped instead of being attached to the wrong invitation
- `message.headers.message-id` is wrapped in angle brackets by default so it can match the Mailgun Messages API JSON `id` captured during dispatch
- delivery events map narrowly: `accepted` to `accepted`, `delivered` to `delivered`, `failed` plus temporary severity to `deferred`, `failed` plus permanent severity to `bounced`, other `failed` events to `failed`, and `complained`/`unsubscribed` to `suppressed`; `opened` and `clicked` are skipped unless a host deliberately enables engagement-event mapping
- set `RequireSignedWebhook` plus `WebhookSigningKey` when the endpoint should verify Mailgun's HMAC-SHA256 signature over `timestamp + token` before translation or reconciliation; missing, malformed, stale, or invalid signatures fail closed with `401`
- keep `AcceptParentSignature` enabled when Mailgun subaccount events should verify against the parent account signing key through `signature.parent-signature`
- keep `EnableSignedWebhookReplayProtection = true` unless the host has a stronger replay boundary outside Cephalon; duplicate verified Mailgun tokens are rejected with `409` inside the bounded process-local retention window and only SHA-256 token fingerprints are retained
- keep `EnableWebhookEventIdIdempotency = true` unless the host owns a stronger callback inbox or idempotency ledger; duplicate translated `event-data.id` values are skipped before reconciliation when the normalized `mailgun:{event-data.id}` observation id already exists in `ITenantInvitationDeliveryStatusObservationStore`
- the endpoint is authorization-required by default and should stay protected by ASP.NET Core policy, a gateway, Mailgun TLS client-certificate checks, or another host-owned control even when signed-webhook verification is enabled
- responses include aggregate and per-event translation outcomes plus `DuplicateEvents` but do not echo recipient email addresses or raw Mailgun payloads
- the package projects `tenant-invitation-delivery-mailgun-status-callbacks` with route/auth/translation/signature/replay/event-id-idempotency posture, including safe HMAC verification metadata, process-local token replay metadata, and observation-store-backed event-id idempotency metadata when configured; it still does not claim durable callback inboxes, distributed replay protection, distributed event-id ledgers, provider polling, or exactly-once delivery

### SendGrid invitation delivery sender

Install `Cephalon.MultiTenancy.Governance.SendGridDelivery` when invitation dispatch should hand a templated email to the SendGrid Mail Send API instead of posting a generic webhook or using SMTP directly.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "SendGridInvitationDelivery": {
          "Enabled": true,
          "SenderId": "sendgrid-email",
          "BaseUrl": "https://api.sendgrid.com",
          "ApiKey": "${SENDGRID_API_KEY}",
          "FromEmail": "noreply@example.com",
          "FromName": "Example SaaS",
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Categories": ["cephalon-invitation"],
          "CustomArgs": {
            "product": "example-saas"
          },
          "Headers": {
            "X-Product": "Example SaaS"
          },
          "EnableSandboxMode": false,
          "ProviderMessageIdHeaderName": "X-Message-ID",
          "AcceptedStatusCodes": [202]
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonSendGridInvitationDelivery(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Operational notes:

- recipient email resolution checks dispatch metadata first, invitation metadata second, and finally `InviteeId` when `InviteeKind` is `email`
- set `BaseUrl` to `https://api.eu.sendgrid.com` for SendGrid EU regional sending; the package always posts to `/v3/mail/send`
- `ISendGridInvitationDeliveryClient` is replaceable, so test hosts, gateway wrappers, or custom HTTP policies can reuse the same Cephalon sender contract without changing the governance dispatcher
- the sender carries deterministic Cephalon message ids through SendGrid `custom_args` and safe `X-Cephalon-*` headers, and captures SendGrid's `X-Message-ID` response header as the provider message id by default
- `EnableSandboxMode` adds `mail_settings.sandbox_mode.enable = true` and treats SendGrid's sandbox `200 OK` validation response as accepted alongside the normal `202 Accepted` response
- sender metadata records endpoint host, SendGrid status code, sandbox posture, Cephalon message id, sender id, recipient email, recipient metadata key, category count, custom-argument count, and safe client metadata, but it does not record the API key, authorization header, raw request body, or message bodies
- this package owns SendGrid Mail Send API handoff only; SendGrid Event Webhook callback translation, optional signed-webhook verification, bounded process-local signed-callback replay protection, and observation-store-backed SendGrid event-id idempotency live in `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore`, Mailgun Messages API handoff lives in `Cephalon.MultiTenancy.Governance.MailgunDelivery`, Amazon SES v2 handoff lives in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`, Microsoft Graph `sendMail` handoff lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`, while durable callback inboxes, distributed replay protection, provider polling, dynamic-template lifecycle management, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS, chat, CRM, identity-provider onboarding, distributed retry queues, and tenant-admin UI remain future provider-pack or application-owned work

### Microsoft Graph invitation delivery sender

Install `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery` when invitation dispatch should hand a templated email to Microsoft Graph `sendMail` instead of posting a generic webhook, using SMTP directly, or using a dedicated email API provider.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "MicrosoftGraphInvitationDelivery": {
          "Enabled": true,
          "SenderId": "microsoft-graph-email",
          "BaseUrl": "https://graph.microsoft.com",
          "ApiVersion": "v1.0",
          "SenderUserId": "invites@example.com",
          "AccessToken": "${MICROSOFT_GRAPH_ACCESS_TOKEN}",
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "SaveToSentItems": false,
          "Categories": ["cephalon-invitation"],
          "Headers": {
            "X-Cephalon-Product": "Example SaaS"
          },
          "AcceptedStatusCodes": [202]
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonMicrosoftGraphInvitationDelivery(builder.Configuration);
builder.Services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Operational notes:

- recipient email resolution checks dispatch metadata first, invitation metadata second, and finally `InviteeId` when `InviteeKind` is `email`
- `IMicrosoftGraphInvitationDeliveryAccessTokenProvider` is replaceable; real hosts should usually install `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity` for `DefaultAzureCredential`, managed identity, or workload identity token acquisition instead of storing a static access token in configuration
- `IMicrosoftGraphInvitationDeliveryClient` is replaceable, so test hosts, gateway wrappers, or custom HTTP policies can reuse the same Cephalon sender contract without changing the governance dispatcher
- the sender posts to `/v1.0/users/{SenderUserId}/sendMail` when `SenderUserId` is configured and `/v1.0/me/sendMail` otherwise; Microsoft Graph accepts successful `sendMail` requests with `202 Accepted`, so Cephalon records accepted handoff truth rather than a delivered-email claim
- the sender carries deterministic Cephalon message ids through safe custom `x-*` internet message headers and request metadata; Graph does not return a mail message id from `sendMail`, so the default provider message id remains empty unless a custom client supplies a truthful id
- sender metadata records endpoint host, Graph status code, sender id, sender-user posture, save-to-sent-items posture, recipient email, recipient metadata key, category count, safe header count, Graph request ids, and safe client metadata, but it does not record bearer tokens, authorization headers, raw request bodies, or message bodies
- this package owns Microsoft Graph `sendMail` handoff only; Azure Identity token acquisition lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity`, while Microsoft Entra app registration, permission consent, mailbox provisioning/access policy, Graph change notifications, delivery completion after accepted handoff, provider polling, durable callback inboxes, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider onboarding, distributed retry queues, and tenant-admin UI remain future provider-pack or application-owned work

### Microsoft Graph Azure Identity token provider

Install `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity` when the Microsoft Graph invitation sender should acquire bearer tokens through Azure.Identity instead of a static configured access token.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "MicrosoftGraphInvitationDelivery": {
          "AzureIdentity": {
            "Enabled": true,
            "Scopes": ["https://graph.microsoft.com/.default"],
            "TenantId": "00000000-0000-0000-0000-000000000000",
            "ManagedIdentityClientId": "11111111-1111-1111-1111-111111111111",
            "AuthorityHost": "AzurePublicCloud",
            "ExcludeInteractiveBrowserCredential": true,
            "ExcludeManagedIdentityCredential": false
          }
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonMicrosoftGraphInvitationDelivery(builder.Configuration);
builder.Services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(builder.Configuration);
```

Operational notes:

- the default scope is `https://graph.microsoft.com/.default`, which is the normal application-permission shape for service-style Graph access
- supported authority aliases are `AzurePublicCloud`, `AzureGovernment`, and `AzureChina`; an absolute HTTPS authority URI is also accepted for deliberate sovereign/private-cloud setups
- explicit `TokenCredential` injection is available for tests, shared host credential factories, or hosts that prefer a concrete credential such as `ManagedIdentityCredential`
- token acquisition diagnostics use `4574-4575` and record credential type, scope count, expiry, and failure reason without logging bearer tokens
- the package owns token acquisition only; Microsoft Entra app registration, `Mail.Send` consent, Exchange mailbox/application access policy, Graph `sendMail` acceptance, downstream delivery completion, provider polling, callbacks, and identity-provider synchronization remain outside this package

### SendGrid invitation delivery status callbacks

`Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` translates SendGrid Event Webhook payloads into the existing tenant-invitation delivery-status reconciler. It is optional and separate from the outbound SendGrid sender so hosts can choose whether SendGrid should call back into the application.

Configuration:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "SendGridInvitationDelivery": {
          "AspNetCore": {
            "EnableStatusCallbackEndpoint": true,
            "StatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status/sendgrid",
            "RequireStatusCallbackAuthorization": true,
            "StatusCallbackAuthorizationPolicy": "sendgrid-event-webhook",
            "ExcludeStatusCallbackEndpointFromDescription": true,
            "RequireProviderMessageMatch": true,
            "RecordStatus": true,
            "Source": "sendgrid-event-webhook",
            "Actor": "sendgrid",
            "MaxRequestBodyBytes": 262144,
            "MaxEventsPerRequest": 1000,
            "MapEngagementEventsAsDelivered": false,
            "NormalizeProviderMessageIdFromSgMessageId": true,
            "RequireSignedEventWebhook": true,
            "SignedEventWebhookPublicKey": "${SENDGRID_EVENT_WEBHOOK_PUBLIC_KEY}",
            "SignedEventWebhookSignatureHeaderName": "X-Twilio-Email-Event-Webhook-Signature",
            "SignedEventWebhookTimestampHeaderName": "X-Twilio-Email-Event-Webhook-Timestamp",
            "SignedEventWebhookSignatureToleranceSeconds": 300,
            "EnableSignedEventWebhookReplayProtection": true,
            "SignedEventWebhookReplayRetentionSeconds": 300,
            "SignedEventWebhookReplayCacheLimit": 4096,
            "EnableEventWebhookEventIdIdempotency": true
          }
        }
      }
    }
  }
}
```

Registration:

```csharp
builder.Services.AddCephalonSendGridInvitationDelivery(builder.Configuration);
builder.Services.AddCephalonSendGridInvitationDeliveryAspNetCore(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});

var app = builder.Build();
app.MapCephalonSendGridInvitationDeliveryStatusCallbacks();
```

Operational notes:

- SendGrid posts Event Webhook payloads as a JSON array; the endpoint rejects oversized bodies and event batches before reconciliation
- the outbound sender stores Cephalon context in SendGrid `custom_args`; callback events without `cephalonTenantId` and `cephalonInvitationId` are skipped instead of being attached to the wrong invitation
- `sg_message_id` is normalized to the prefix before the first dot by default so it can match the `X-Message-ID` provider message id captured during Mail Send dispatch
- delivery events map narrowly: `processed` to `accepted`, `delivered` to `delivered`, `deferred` to `deferred`, `bounce` to `bounced`, and `dropped`/`spamreport`/`unsubscribe`/`group_unsubscribe` to `suppressed`; `open` and `click` are skipped unless a host deliberately enables engagement-event mapping
- set `RequireSignedEventWebhook` plus `SignedEventWebhookPublicKey` when the endpoint should verify SendGrid's ECDSA-SHA256 signature before JSON parsing or reconciliation; the signature input is the timestamp header plus the exact raw request body bytes, not re-serialized JSON
- keep `EnableSignedEventWebhookReplayProtection` enabled when signed verification is required; duplicate verified signatures inside `SignedEventWebhookReplayRetentionSeconds` return `409` before reconciliation and are reported through diagnostic `4564`
- the replay guard stores only safe signature fingerprints in a bounded in-memory cache; `SignedEventWebhookReplayCacheLimit` controls the per-process cap, and the runtime surface reports `process-local` scope plus `none` durability so operators do not confuse it with a distributed callback inbox
- keep `EnableEventWebhookEventIdIdempotency` enabled when provider retries might produce the same `sg_event_id` with a fresh timestamp/signature; translated duplicates are skipped before reconciliation, return `200` with `DuplicateEvents`, and are reported through diagnostic `4565`
- event-id idempotency uses the normalized `sendgrid:{sg_event_id}` observation id in `ITenantInvitationDeliveryStatusObservationStore`; it is `cephalon-managed` only when the governance observation store is enabled, and its durability follows that store, so in-memory stores are process-local while the built-in file store can retain observed ids locally
- the endpoint is authorization-required by default and should stay protected by ASP.NET Core policy, a gateway, or SendGrid OAuth token validation even when signed Event Webhook verification is enabled
- responses include aggregate and per-event translation outcomes but do not echo recipient email addresses or raw SendGrid payloads
- the package projects `tenant-invitation-delivery-sendgrid-status-callbacks` with signature verification, process-local replay posture, and observation-store-backed event-id idempotency posture; it still does not claim durable callback inboxes, distributed replay protection, distributed event-id ledgers, provider polling, or exactly-once delivery

## Data product surface

`GET /engine/data-products` exposes the operator-facing data product catalog contributed by active modules.

Current payload highlights:

- each data product carries a stable `id`, `displayName`, `description`, `sourceModuleId`, `domainId`, `contractId`, and `mode`
- `tags` and free-form `metadata` let a module publish freshness, classification, and other operator-facing data mesh hints without tying the engine to one provider or federation model
- the same data product catalog is also available through `/engine/snapshot` when operators want one merged runtime answer

Current note:

- the baseline is intentionally descriptor-first: query execution still belongs to the owning module or data pack, while the engine owns the runtime catalog
- invalid data-product source-module ownership fails at build time instead of leaking broken operator metadata

## Projection surface

`GET /engine/projections` exposes the operator-facing projection catalog contributed by active modules.

Current payload highlights:

- each projection carries a stable `id`, `displayName`, `description`, `sourceModuleId`, `targetStoreId`, and `mode`
- `sourceContracts`, `tags`, and free-form `metadata` keep the projected read model grounded in the surrounding CQRS or event-driven contract without tying the engine to one storage provider
- the same projection catalog is also available through `/engine/snapshot` when operators want one merged runtime answer

Current note:

- this is a descriptive runtime catalog for the active composition, not an Entity Framework-specific implementation contract yet
- invalid projection source-module ownership fails at build time instead of leaking broken operator metadata

## Outbox surface

`GET /engine/outboxes` exposes the operator-facing outbox catalog contributed by active modules and companion packs.

Current payload highlights:

- each outbox carries a stable `id`, `displayName`, `description`, `sourceModuleId`, `provider`, and `mode`
- `channelIds`, `tags`, and free-form `metadata` let a pack say whether the outbox is channel-scoped, what provider owns it, and whether dispatch/runtime linking is configured yet
- the same outbox catalog is also available through `/engine/snapshot` when operators want one merged runtime answer

Current note:

- this is a descriptive runtime answer for durable outbound staging surfaces, not a claim that Cephalon already ships a full event dispatch bridge
- invalid outbox source-module ownership fails at build time instead of leaking broken operator metadata

## CDC capture surface

`GET /engine/cdc-captures` exposes the operator-facing CDC capture catalog contributed by active modules.

Current payload highlights:

- each CDC capture carries a stable `id`, `displayName`, `description`, `sourceModuleId`, `provider`, `sourceId`, `outboxId`, `mode`, and `eventFormat`
- `resourceIds`, `tags`, and free-form `metadata` let a module publish the table, collection, or resource scope plus operator-facing capture hints without tying the engine to one provider runtime
- `executionBinding` now keeps authored/requested/effective execution-runtime truth on the same descriptor surface, including the resolved ownership mode, execution topology, and selection reason
- the same CDC capture catalog is also available through `/engine/snapshot` when operators want one merged runtime answer
- drill-down routes narrow the same catalog by capture id, source module, provider, outbox, source, and resource through `/engine/cdc-captures/{cdcCaptureId}`, `/engine/cdc-captures/modules/{moduleId}`, `/engine/cdc-captures/providers/{provider}`, `/engine/cdc-captures/outboxes/{outboxId}`, `/engine/cdc-captures/sources/{sourceId}`, and `/engine/cdc-captures/resources/{resourceId}`
- `/engine/cdc-captures/execution-runtimes/{executionRuntimeId}` now exposes the inverse view for every capture effectively owned by one execution runtime

Current note:

- the surface is still descriptor-first, but it is no longer descriptor-only: `Cephalon.Data.MongoDB` now ships the first provider-native capture implementation through configured MongoDB change streams while the engine still owns the catalog, outbox linkage, capture-side execution binding, and validation
- provider packs can contribute a capture on behalf of another module without rewriting authored ownership; when that happens, `sourceModuleId` stays authoritative and metadata can also surface `contributorModuleId`
- invalid authored source-module ownership or missing outbox references still fail at build time instead of leaking broken operator metadata

## CDC capture runtime-state surface

`GET /engine/cdc-captures/runtime` exposes the latest operator-facing CDC runtime-state catalog
reported for active captures.

Current payload highlights:

- each runtime-state entry keeps the descriptor-owned identity (`cdcCaptureId`, `sourceModuleId`,
  `provider`, `sourceId`, `outboxId`, `mode`, `eventFormat`, and `resourceIds`) alongside the
  latest reported `lastOutcome`, `lastObservedAtUtc`, checkpoint/change-id/error details, and
  latest/total captured-change plus produced-message counts
- `executionBinding` keeps the same authored/requested/effective execution-runtime answer visible on
  the live state surface, so runtime reports do not need to infer capture ownership back out of
  metadata-only hints
- each runtime-state entry now also carries typed `freshness`, `lag`, and `publication` answers so
  provider packs can surface freshness windows, pending source-change counts, and pending
  publication counts without forcing hosts or operators to parse ad-hoc metadata
- shared in-process execution can now also report `acknowledgement` and `acknowledgerServiceType`
  metadata on successful staged-batch acknowledgement, while acknowledgement failures keep
  `failureKind = acknowledgement` plus pending checkpoint/change-id metadata visible without
  falsely advancing the durable checkpoint answer
- the catalog projects active captures even before the first provider report arrives, so operators
  can still see declared ownership and linked outbox identity before execution starts
- when the linked publication path already reports runtime truth, `outboxDispatchState` carries the
  latest downstream dispatch posture directly on the same CDC runtime answer instead of forcing a
  second join back through `/engine/event-dispatches`
- when `AddData()` enables the shared CDC execution substrate, the same runtime also exposes the
  `data-cdc-capture-flow` execution graph plus the `data-cdc-capture-pump` hosted execution
  through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and
  `/engine/snapshot`; that execution graph now includes the explicit
  `acknowledge-cdc-progress` step between outbox staging and runtime-state reporting, while the
  per-capture CDC routes remain the detailed ownership and runtime-state truth for each active
  capture
- the same runtime-state catalog is also available through `/engine/snapshot` in
  `CdcCaptureStates` when operators want one merged runtime answer
- drill-down routes narrow the same runtime-state catalog by capture id, source module, provider,
  outbox, source, and resource through `/engine/cdc-captures/runtime/{cdcCaptureId}`,
  `/engine/cdc-captures/runtime/modules/{moduleId}`,
  `/engine/cdc-captures/runtime/providers/{provider}`,
  `/engine/cdc-captures/runtime/outboxes/{outboxId}`,
  `/engine/cdc-captures/runtime/sources/{sourceId}`, and
  `/engine/cdc-captures/runtime/resources/{resourceId}`
- that same runtime-state catalog now also supports reporter-, edge-, and coordination-aware
  drill-down through `/engine/cdc-captures/runtime/reporters/{reporterId}`,
  `/engine/cdc-captures/runtime/edge-nodes/{edgeNodeId}`,
  `/engine/cdc-captures/runtime/reporter-coordination/{coordinationState}`, and
  `/engine/cdc-captures/runtime/reporter-coordination/issues/{degradedReason}`
- `/engine/cdc-captures/runtime/execution-runtimes/{executionRuntimeId}` now exposes the inverse
  runtime-state view for every capture effectively owned by one execution runtime
- when `DataRuntimeOptions.EnableExternalCdcRuntimeReporting = true`, `POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports` accepts `CdcCaptureRuntimeObservation[]` payloads for that runtime, validates effective ownership per capture, enforces declared reporter and edge-node policy when present, and refreshes the same runtime-state catalog instead of a separate external-monitor surface
- `reporterCoordination` now also keeps participant-level `reporterParticipants` plus additive
  `hasStandbyReporters`, `hasRejectedReporters`, `participantCount`, `activeReporterCount`,
  `standbyReporterCount`, and `rejectedReporterCount` summaries so operators can see which
  reporters are currently active, waiting in standby after takeover, or explicitly rejected for
  lease conflicts without inferring that story from metadata alone
- that same `reporterCoordination` answer now also publishes `takeoverState`, `degradedReason`,
  `requiresTakeover`, and `hasCompletedTakeover`, so operator flows can distinguish
  `awaiting-takeover`, `rejected-reporter-conflict`, and `multiple-active-reporters` posture
  directly on the shared capture/runtime-state surface
- later accepted reports now clear stale rejected-conflict evidence and completed takeovers now
  stop surfacing previous owners as standby participants after the replacement reporter reaffirms
  its lease, while `previousReporterId`, `leaseExpiredAtUtc`, and `lastTakeoverObservedAtUtc`
  still preserve the historical handoff story

Current note:

- the shared runtime-state catalog is reporter-driven and descriptor-backed: the shared
  `Cephalon.Data` pump is one in-process execution substrate, and `Cephalon.Data.MongoDB` now also
  reports provider-native change-stream posture through the same surface instead of inventing a
  MongoDB-only monitor
- provider packs can now project typed freshness, lag, publication posture, bounded capture
  batches, checkpoints, reporter identity, reporter lease, edge provenance, and failure metadata
  through the shared contract today, while later out-of-process or edge-aware CDC execution
  topologies should stay additive over the same
  capture/runtime-state and execution-runtime surfaces instead of a second host-only registry

## CDC capture execution-runtime surface

`GET /engine/cdc-capture-runtimes` exposes the operator-facing CDC execution-runtime catalog
derived from active data packs plus the shared CDC runtime-state surface.

Current payload highlights:

- each execution runtime carries a stable `id`, `displayName`, `description`, bounded
  `cdcCaptureIds`, first-class `executionOwnership`, `executionTopology`,
  `acknowledgementMode`, `hostedExecutionId`, `executionGraphId`, `reporterLeaseSeconds`,
  `rejectConflictingReporterIds`, declared `edgeNodeIds`, and operator-facing `metadata`
- `summary` carries the aggregate latest-plus-total runtime answer for that execution runtime,
  including reported capture ids, latest outcome/observation time, latest checkpoint/change id,
  aggregate started/captured/idle/failed counts, total captured changes, total produced messages,
  latest acknowledgement posture, latest error, `lastReporterId`, `activeReporterId`,
  `reporterLeaseExpiresAtUtc`, `observedEdgeNodeIds`, `lastEdgeNodeId`, and typed
  `reporterCoordination`
- that same `reporterCoordination` answer now keeps participant-level `reporterParticipants` plus
  additive `hasStandbyReporters`, `hasRejectedReporters`, `participantCount`,
  `activeReporterCount`, `standbyReporterCount`, and `rejectedReporterCount` summaries so
  runtime-first operator views can explain active versus standby versus rejected reporters directly
- that same runtime-first coordination answer now also keeps `takeoverState`, `degradedReason`,
  `requiresTakeover`, and `hasCompletedTakeover`, so operators can read whether one runtime is
  awaiting failover, already completed a takeover, still carrying rejected conflicts, or exposing
  a multiple-active ambiguity without re-deriving it from raw lease timestamps
- later accepted reports now clear stale rejected-conflict evidence and completed takeovers now
  stop surfacing historical previous owners as standby participants after the replacement reporter
  has reasserted lease ownership, while the same summary still keeps historical takeover fields
  available for operator readback
- the same execution-runtime catalog is also available through `/engine/snapshot` in
  `CdcCaptureExecutionRuntimes` when operators want one merged runtime answer
- the drill-down route `/engine/cdc-capture-runtimes/{executionRuntimeId}` narrows the same catalog
  to one execution runtime by stable id
- that same runtime-first catalog now also supports reporter-, edge-, and coordination-aware
  drill-down through `/engine/cdc-capture-runtimes/reporters/{reporterId}`,
  `/engine/cdc-capture-runtimes/edge-nodes/{edgeNodeId}`,
  `/engine/cdc-capture-runtimes/reporter-coordination/{coordinationState}`, and
  `/engine/cdc-capture-runtimes/reporter-coordination/issues/{degradedReason}`
- `/engine/cdc-captures*` and `/engine/cdc-captures/runtime*` now also carry first-class
  `executionBinding` plus typed `reporterCoordination` answers, so runtime-first and capture-first
  ownership views stay aligned even when an external reporter lease expires, a new reporter takes
  over, a conflicting reporter is rejected, or two captures make one runtime look multiply active
- host-level `AddData(... configure => configure.CdcExecutionRuntimes ...)` or other
  `DataRuntimeOptions.CdcExecutionRuntimes` declarations can publish external-managed,
  provider-native, edge, or other runtime answers on the same catalog without falsely implying the
  engine hosts those runners itself
- when `DataRuntimeOptions.EnableExternalCdcRuntimeReporting = true`, `POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports` returns the refreshed descriptor for that runtime after merging the supplied observations into the shared runtime-state catalog, including refreshed reporter-lease and observed-edge-node summaries when they apply

Current note:

- the shipped shared `Cephalon.Data` runtime contributes `data-cdc-capture-pump` with
  `executionOwnership = host-managed`, `executionTopology = shared-in-process-polling`,
  `acknowledgementMode = post-stage-provider`, and links back to `data-cdc-capture-flow` plus
  `data-cdc-capture-pump`; `/engine/cdc-captures*` and `/engine/cdc-captures/runtime*` remain the
  detailed per-capture ownership and live-posture surfaces, while the shared pump now executes only
  captures whose effective owner resolves to that runtime
- the first shipped provider-native runtime is `mongodb-change-stream-capture-pump` from
  `Cephalon.Data.MongoDB`; it publishes `executionOwnership = host-managed`,
  `executionTopology = provider-native`, `acknowledgementMode = provider-native`, links to
  `mongodb-change-stream-capture-flow`, and keeps resume-token checkpoint truth on the same
  per-capture runtime-state catalog
- the external report-ingest route is additive and opt-in, not a second ownership source: capture ownership still comes from `executionBinding`, and the route rejects reports that do not match the effective `executionRuntimeId`

## Inbox surface

`GET /engine/inboxes` exposes the operator-facing inbox catalog contributed by active modules and companion packs.

Current payload highlights:

- each inbox carries a stable `id`, `displayName`, `description`, `sourceModuleId`, `provider`, and `mode`
- `channelIds`, `tags`, and free-form `metadata` let a pack say whether the inbox is channel-scoped, what provider owns it, and whether idempotency or dispatch/runtime linking is configured yet
- the same inbox catalog is also available through `/engine/snapshot` when operators want one merged runtime answer

Current note:

- this is a descriptive runtime answer for processed-message or idempotency-store surfaces, not a claim that Cephalon already ships a full subscription-dispatch runtime
- invalid inbox source-module ownership fails at build time instead of leaking broken operator metadata

## Event dispatch runtime surfaces

`GET /engine/event-dispatch-runtimes` exposes named dispatch-runtime descriptors when an eventing
pack contributes the abstraction-level descriptor catalog. `GET /engine/event-dispatches` exposes
the latest per-outbox dispatch-state reports when an eventing pack contributes the abstraction-level
runtime catalog.

Current payload highlights:

- dispatch-runtime descriptors carry the runtime id, display name, description, ownership metadata,
  owned outbox ids, and an aggregate `Summary` once live dispatch reports exist
- dispatch-runtime summaries include total report counts, retry-pending outbox count,
  terminal-failure observation count, terminal outbox count, and `HasTerminalFailures`
- dispatch states carry the outbox id, latest channel id, latest outcome, observation timestamp,
  message id, attempt, started/succeeded/failed/retry-scheduled/skipped counters, retry-pending
  posture, terminal-failure posture, terminal-failure count, optional error, and safe metadata
- `GET /engine/event-dispatch-runtimes/{dispatchRuntimeId}` narrows the descriptor catalog to one
  runtime and returns `404` when no matching descriptor exists
- `GET /engine/event-dispatches/{outboxId}` narrows the state catalog to one outbox path and
  returns `404` when no state has been reported for that path
- `GET /engine/event-dispatches/terminal-failures` filters the same state catalog to outbox paths
  whose latest report marks the dispatch path as terminally failed
- `POST /engine/event-dispatches/{outboxId}/commands/{operationId}` runs bounded dispatch-store
  remediation commands when `IEventDispatchRemediationDispatcher` is active; current operations are
  `retry-now`, `retry-later`, `skip`, `quarantine`, and dispatch-store `dead-letter`; command ids
  are unique, and a duplicate command id returns a rejected result without mutating dispatch-store
  state or replacing the original command-result record
- `GET /engine/event-dispatch-remediation-commands` lists accepted and rejected command results
  recorded by `IEventDispatchRemediationRuntimeCatalog`; `?limit={positiveInteger}` returns only
  the newest retained records
- `GET /engine/event-dispatch-remediation-commands/summary` returns the bounded command-result
  roll-up: total, accepted, rejected, errored, duplicate-command count, and latest command
  identity/outcome fields without scanning the full history; it also carries dropped-command count,
  retention-truncated state, incomplete-summary warning state, and oldest retained cutoff metadata
- `GET /engine/event-dispatch-remediation-commands/latest` returns the newest authoritative
  command-result record with full metadata and error payload, and duplicate command-id attempts do
  not replace that record
- `GET /engine/event-dispatch-remediation-commands/retention` returns the bounded command-result
  retention posture: history limit, retained count, total recorded count, dropped count, truncation
  flag, oldest retained command, and latest retained command so operators know when process-local
  history has discarded older command results
- `GET /engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}`
  filters command results by inclusive observed UTC window so incident timelines can read retained
  accepted and rejected command history without scanning the full bounded list; append
  `&limit={positiveInteger}` to return only the newest retained records inside that window
- `GET /engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}`
  summarizes retained command results inside the same observed UTC window so operator dashboards can
  read accepted, rejected, error, duplicate, latest-command, retention-truncated, and
  incomplete-window posture without materializing the list
- `GET /engine/event-dispatch-remediation-commands/{commandId}` narrows the command-result catalog
  to one command and returns `404` when no command has been recorded with that id
- `GET /engine/event-dispatch-remediation-commands/outboxes/{outboxId}` filters command results by
  target outbox id
- `GET /engine/event-dispatch-remediation-commands/messages/{messageId}` filters command results by
  event message id
- `GET /engine/event-dispatch-remediation-commands/channels/{channelId}` filters command results by
  event channel id
- `GET /engine/event-dispatch-remediation-commands/operations/{operationId}` filters command
  results by stable remediation operation id, such as `retry-now`, `retry-later`, `skip`,
  `quarantine`, or `dead-letter`
- `GET /engine/event-dispatch-remediation-commands/actors/{actorId}` filters command results by
  the optional `operatorActorId` recorded with the accepted or rejected command
- `GET /engine/event-dispatch-remediation-commands/correlations/{correlationId}` filters command
  results by the optional `operatorCorrelationId` recorded with the accepted or rejected command
- `GET /engine/event-dispatch-remediation-commands/reasons/{reason}` filters command results by
  the optional `operatorCommandReason` recorded with the accepted or rejected command
- `GET /engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}` filters
  command results by dispatch-store outcome, such as `retry-scheduled`, `skipped`, or `failed`
- `GET /engine/event-dispatch-remediation-commands/outcomes/{outcome}` filters command results by
  command outcome, such as `accepted` or `rejected`
- list and filter command-result routes that return arrays reject invalid, zero, or negative
  `limit` query values with `400`; summary, latest, retention, and single-command reads keep their
  existing direct contracts
- the same descriptor and state catalogs are also available through `/engine/snapshot` in
  `EventDispatchRuntimes` and `EventDispatchStates` when operators want one merged runtime answer

Current note:

- terminal-failure posture is an operator/drill-down answer over dispatch reports and supported
  dispatch-store state; it is not a broker-specific dead-letter queue, durable inbox, generic
  inbound broker-consumption, downstream delivery-completion, or cross-node exactly-once claim
- dispatch remediation commands apply only to the mutable dispatch store that owns the outbox; they
  do not claim broker dead-letter/replay ownership, and `dead-letter` records terminal dispatch-store
  intent with `brokerDeadLetter = false` rather than a broker DLQ operation
- command-result idempotency is intentionally fail-closed: operators should inspect
  `/engine/event-dispatch-remediation-commands/{commandId}` or
  `/engine/event-dispatch-remediation-commands/latest` after an uncertain client retry and use a new
  command id for a new action
- remediation command-result history is bounded and process-local by default; it gives operators a
  typed command-audit read model for the active host; summary responses now expose truncation and
  incomplete-window warning flags directly, and the `/retention` readback remains the full posture
  when operators need retained/recorded/dropped counts plus latest retained command detail; use
  `/observations?fromUtc={fromUtc}&toUtc={toUtc}` for retained incident windows, but
  do not treat it as durable compliance retention or broker replay ownership
- outbox-backed publication states use `accepted` to mean "staged for later dispatch"; dispatch
  completion and terminal dispatch failure remain separate event-dispatch runtime answers

## Event publication action and runtime-state surfaces

`POST /engine/event-publications` requests one bounded event publication when an eventing pack
registers the abstraction-level `IEventPublicationDispatcher`.
`GET /engine/event-publications/runtime` exposes the latest publication runtime states when the
selected runtime registers the abstraction-level `IEventPublicationRuntimeCatalog`.

Current payload highlights:

- the request body carries `id`, `channelId`, `eventType`, JSON or string `payload`,
  `occurredAtUtc`, `contentType`, `correlationId`, `tenantId`, optional headers, safe metadata,
  and optional actor id
- the response is `EventPublicationResult` with the publication id, channel id, event type,
  `accepted` outcome, accepted timestamp, and safe route-trigger metadata such as
  `trigger = aspnetcore-operator-route` and `route = /engine/event-publications`
- when `Engine:Messaging:Publications:Routing` is enabled, the response channel id is the
  effective routed channel; metadata reports `requestedChannelId`, `routingPolicy`,
  `routingState`, `routingRequestedChannelId`, `routingEffectiveChannelId`, `routingRule`,
  and `routingRuleChannelId` when a route is matched
- routed requests can use `channelId = auto` by default, exact event-type route keys match
  before trailing-`*` prefix routes, and explicit channel mismatches return `400` when
  `RejectMismatchedExplicitChannel` is enabled
- the route returns `404` when event publication is not active in the selected runtime or when the
  requested channel is not registered, and `400` when the publication body is invalid
- when the core in-process lane is selected, the route triggers the active `IEventPublisher`,
  invokes matching executor services added through `AddCephalonEventSubscriptionExecutor<TExecutor>()`
  or direct `IEventSubscriptionExecutor` registrations, and flows publication metadata back into the
  existing subscription runtime catalog as `publicationMetadata.*`
- when native publication scheduling is enabled, request metadata can include exactly one of
  `scheduledForUtc` or `delayMilliseconds`; the route returns `accepted` immediately with
  `schedulePolicy = bounded-process-local`, `scheduleState = scheduled`, `scheduleScope = process-local`,
  `scheduleDurability = none`, and pending-count metadata before the local timer hands the publication
  to the active publisher
- each runtime-state entry carries the publication id, latest channel id, event type, latest
  outcome, observation timestamp, accepted/succeeded/failed/skipped counters, latest subscription
  counts, optional error summary, and safe metadata
- `GET /engine/event-publications/runtime/{publicationId}` narrows the same catalog to one
  publication and returns `404` when no state has been reported for that publication
- `GET /engine/event-publications/runtime/channels/{channelId}` narrows the same catalog to one
  channel and returns the reported publication states for that channel
- the same publication-state catalog is also available through `/engine/snapshot` in
  `EventPublicationStates` when operators want one merged runtime answer

Current note:

- this is a bounded operator action over the active eventing publication path; the in-process lane
  can optionally suppress duplicate completed executions process-locally, can optionally resolve or
  validate event-type-to-channel publication routing, and can optionally accept bounded
  process-local delayed publication requests; the runtime-state catalog reports that local
  publication outcome, but the route is not a durable broker, durable inbox, cross-node idempotency,
  retry-queue, durable/distributed scheduler, broker delayed-delivery surface, broker topology
  materializer, provider partition owner, or provider-specific inbound-consumption claim
- outbox-backed publication states use `accepted` to mean "staged for later dispatch"; downstream
  dispatch completion remains the job of the dispatch-runtime and dispatch-state surfaces
- the action and read contracts live in `Cephalon.Abstractions.Data` so `Cephalon.Engine` and
  `Cephalon.AspNetCore` can expose them without referencing `Cephalon.Eventing`; the selected
  eventing pack still owns the implementation and runtime truth

## Event subscription readiness surface

`GET /engine/event-subscription-readiness` exposes the operator-facing readiness catalog for
declared event subscriptions when an eventing pack registers the abstraction-level
`IEventSubscriptionExecutionReadinessCatalog`.

Current payload highlights:

- each readiness entry carries a stable `subscriptionId`, `readinessState`, `executionOwnership`,
  `executionMode`, optional `executionRuntimeId`, ordered machine-readable `reasons`, and safe
  metadata
- `readinessState` is one of `runtime-bound`, `hosted-execution-linked`,
  `application-managed-state`, or `declared-only`, so declared subscriptions do not look execution
  ready until a managed binding, hosted execution link, or runtime report exists
- `GET /engine/event-subscription-readiness/{subscriptionId}` narrows the same catalog to one
  declared subscription and returns `404` when that subscription does not exist
- the same readiness catalog is also available through `/engine/snapshot` in
  `EventSubscriptionExecutionReadiness` when operators want one merged runtime answer

Current note:

- the contract lives in `Cephalon.Abstractions.Data` so `Cephalon.Engine` and host adapters can
  expose it without referencing `Cephalon.Eventing`; the eventing pack or an optional companion
  still owns the implementation and runtime truth

## Agent tool run surface

`GET /engine/agent-tool-runs` exposes the operator-facing run-state catalog for agent-tool runs
when an agentics pack registers the abstraction-level `IAgentToolRunCatalog`.
`POST /engine/agent-tools/{toolId}/runs` requests one bounded managed tool run when the active
runtime also registers the abstraction-level `IAgentToolDispatcher`.

Current payload highlights:

- each run entry carries a stable `toolId`, `runId`, latest outcome, latest observation timestamp,
  actor/correlation details, attempt number, outcome counters, retry-scheduled count,
  output/error summaries, and safe metadata
- `requiresApproval`, `retryPending`, `duplicateCompleted`, `terminalFailure`, and `isTerminal`
  keep policy waits, process-local retry waits, duplicate-completed suppression, terminal failed
  executor outcomes, and final success, failure, skipped, or denied outcomes separate
- `GET /engine/agent-tool-runs/retry-pending` filters the same catalog to runs whose latest report
  is `retry-scheduled`
- `GET /engine/agent-tool-runs/idempotency-duplicates` filters the same catalog to runs whose latest
  report is a process-local duplicate-completed suppression
- `GET /engine/agent-tool-runs/approval-required` filters the same catalog to runs whose latest
  report is waiting on an explicit policy approval decision
- `GET /engine/agent-tool-runs/terminal-failures` filters the same catalog to runs whose latest
  report is a terminal `failed` outcome
- `GET /engine/agent-tool-runs/{runId}` narrows the same catalog to one reported run and returns
  `404` when that run does not exist
- `GET /engine/agent-tool-runs/by-tool/{toolId}` narrows the same catalog to all runs reported for
  one tool id
- `POST /engine/agent-tools/{toolId}/runs` accepts optional body fields for `runId`, `actorId`,
  `correlationId`, `attempt`, string `arguments`, and safe `metadata`, generates route-owned
  defaults when they are absent, returns the `AgentToolExecutionResult`, returns `404` when execution
  is not active or the tool id is unknown, and returns `409` when the tool exists but no executor is
  registered
- the same run-state catalog is also available through `/engine/snapshot` in `AgentToolRuns` when
  operators want one merged runtime answer

Current note:

- the read and action contracts live in `Cephalon.Abstractions.Agentics` so `Cephalon.Engine` and
  host adapters can expose them without referencing `Cephalon.Agentics`; the agentics pack still owns
  the dispatcher implementation, executor, policy, observer, reporter, and in-memory catalog
  implementation
- retry posture is bounded and process-local through `AgenticRuntimeOptions.ExecutionMaxAttempts`
  and `ExecutionRetryDelayMilliseconds`; durable retry queues, autonomous planning, memory
  persistence, distributed scheduling, and provider-specific AI orchestration remain outside this
  proof until a package truly owns those paths
- idempotency posture is opt-in and process-local through
  `AgenticRuntimeOptions.EnableExecutionIdempotency` and
  `ExecutionIdempotencyRetentionMinutes`; it suppresses duplicate completed tool runs observed by the
  in-memory run catalog but does not claim a durable inbox, cross-node exactly-once delivery, broker
  deduplication, durable retry queues, distributed scheduling, or provider-specific AI orchestration
- approval-required and terminal-failure filters are read-only operator seams over the latest
  run-state catalog; they do not claim a durable approval workflow, dead-letter system, durable
  retry queue, distributed scheduler, or provider-specific AI orchestration

## Knowledge index surface

`GET /engine/knowledge-indexes` exposes the operator-facing index-state catalog for managed
retrieval indexes when a retrieval pack registers the abstraction-level `IKnowledgeIndexCatalog`.
`POST /engine/knowledge-indexes/{collectionId}/reindex` requests a manual reindex when the active
runtime also registers the abstraction-level `IKnowledgeIndexer`.
`POST /engine/knowledge-indexes/{collectionId}/queries` executes one bounded operator query when the
active runtime also registers the abstraction-level `IKnowledgeQueryEngine`.

Current payload highlights:

- each entry carries a stable `collectionId`, latest indexing run id and outcome, observation and
  indexed timestamps, source freshness, document count, freshness state, indexing counters, query
  count, latest query timestamp, query fingerprint, query length, matched-count summary, actor,
  correlation id, error summary, and safe metadata
- `GET /engine/knowledge-indexes/{collectionId}` narrows the same catalog to one collection and
  returns `404` when no activity has been recorded for that collection
- `POST /engine/knowledge-indexes/{collectionId}/reindex` accepts optional `runId`, `actorId`, and
  `correlationId` query values, generates safe defaults when they are absent, returns the
  `KnowledgeIndexingResult` for the replacement run, and returns `404` when indexing is not active
  or the collection is not registered
- `POST /engine/knowledge-indexes/{collectionId}/queries` accepts a JSON body with `queryText`,
  optional `maxResults`, `actorId`, `correlationId`, and safe metadata, returns the
  `KnowledgeQueryResult` for the current index, returns `404` when querying is not active or the
  collection is not registered, and returns `400` when the query request is invalid
- opt-in background reindexing uses the same catalog and indexer path, records
  `trigger = retrieval-background-scheduler`, `scheduler`, `schedulerIterationId`, collection
  scope, startup-run, delay, and interval metadata, and never introduces a separate scheduler route
- the same index-state catalog is also available through `/engine/snapshot` in `KnowledgeIndexes`
  when operators want one merged runtime answer

Current note:

- the read contract, manual reindex command seam, and bounded query command seam live in
  `Cephalon.Abstractions.Retrieval` so `Cephalon.Engine` and host adapters can expose, query, and
  remediate index posture without taking a direct dependency on `Cephalon.Retrieval`
  implementation types
- the indexer implementation and query execution path remain owned by the selected retrieval pack
  through `IKnowledgeIndexer`, `IKnowledgeQueryEngine`, and registered
  `IKnowledgeDocumentProvider` services
- `RetrievalOptions.EnableBackgroundReindexing` registers an opt-in generic-host scheduler only
  when ingestion is enabled; empty `BackgroundReindexCollectionIds` means all registered
  collections, while configured ids narrow the scheduler to matching registered collections

## Authorization policy surface

`GET /engine/authorization-policies` exposes the operator-facing authorization-policy catalog contributed by active modules.

Current payload highlights:

- each policy carries a stable `id`, `displayName`, `description`, supported authorization `modes`, and optional `tags` plus `metadata`
- supported modes stay aligned with the phase-8 core model: `RBAC`, `ABAC`, and `Policy`
- the same authorization-policy catalog is also available through `/engine/snapshot` when operators want one merged runtime answer

Current note:

- this is a host-agnostic runtime answer for active policy descriptors, not a replacement for ASP.NET Core schemes, claims mapping, or provider-specific identity wiring
- the engine stamps each policy with its contributing module through `metadata.sourceModuleId` so operators can trace ownership without a second registry

## Technology surface

`GET /engine/technology-surfaces` exposes the active runtime surfaces projected by selected technology packs.

Current `Cephalon.Eventing` highlights:

- `event-channels` and `event-subscriptions` keep channel and declared subscription descriptors
  visible under the selected `EventDrivenIntegration` technology
- `engine.AddEventingFromConfiguration(configuration)` can populate channel descriptors from
  `Engine:Messaging:Channels`; subscription descriptors and executors stay code-first through
  `EventingOptions.Subscriptions`, `IEventSubscriptionContributor`,
  `services.AddCephalonEventSubscriptionExecutor<TExecutor>()`, `EventSubscriptionAttribute`,
  optional descriptor providers, and code-first middleware helpers so publish/subscribe behavior
  remains typed and fast
- `Engine:Messaging:Subscriptions` and `Engine:Messaging:SubscriptionHandlers` are rejected by
  the native configuration reader; use code registration or reusable modules for subscription
  descriptors, handlers, and executors
- `event-subscriptions` now projects execution readiness through stable metadata keys such as
  `executionReadiness`, `executionPath`, and `executionReadinessReasons`
- the typed readiness answer is also available through `/engine/event-subscription-readiness` and
  `snapshot.EventSubscriptionExecutionReadiness`, so operators do not need to parse metadata when
  they only need the readiness posture
- the typed publication-state answer is also available through `/engine/event-publications/runtime*`
  and `snapshot.EventPublicationStates`, so operators can inspect the latest accepted/succeeded,
  failed, or skipped publication posture without parsing `event-publishers` metadata
- `Cephalon.Eventing` can move a subscription to `runtime-bound` itself when
  `EnableInProcessSubscriptionExecution` is selected and a matching
  `AddCephalonEventSubscriptionExecutor<TExecutor>()` or `IEventSubscriptionExecutor`
  registration exists; that path is `cephalon-managed`, direct, process-local, and reports
  `eventing.publish` / `eventing.subscribe` metadata with `retryPolicy = none` and
  `publicationRuntimeState = available` by default
- when `InProcessSubscriptionMaxAttempts` is greater than `1`, that same core path reports
  `retryPolicy = bounded-in-process`, `retryMaxAttempts`, `retryDelayMilliseconds`,
  `retryBackoff`, `retryBackoffMultiplier`, `retryMaxDelayMilliseconds`,
  `retryJitterPercent`, `retryDurability = none`, and `retryScope = process-local`, emits
  `retry-scheduled` observations with `retryEffectiveDelayMilliseconds` between attempts, and updates `event-subscriptions` counters such as
  `retryScheduledCount`, `lastAttempt`, and `reported.retryPolicy`
- when `EnableInProcessSubscriptionIdempotency` is enabled, the direct lane suppresses duplicate
  completed `subscriptionId + publicationId` executions inside the configured process-local
  retention window, reports `skipped`, and projects `idempotencyPolicy = completed-publication`,
  `idempotencyKey = subscription-publication`, `idempotencyRetentionMinutes`,
  `idempotencyDurability = none`, and `idempotencyScope = process-local` through capabilities,
  bindings, `event-publishers`, `event-subscriptions`, and `reported.*` metadata
- when `EnablePublicationScheduling` is enabled, the publication dispatcher accepts bounded delayed
  publication requests over `scheduledForUtc` or `delayMilliseconds` metadata, reports
  `schedulePolicy = bounded-process-local`, `scheduleScope = process-local`,
  `scheduleDurability = none`, pending count, and next due time through `eventing.publish`,
  `event-publishers`, and publication runtime metadata, and then uses an in-memory local timer to
  hand due publications to the selected publisher
- when `EnablePublicationRouting` is enabled, the publication dispatcher resolves or validates the
  effective channel through `Engine:Messaging:Publications:Routing`; `eventing.publish`,
  `event-publishers`, publication runtime metadata, and `eventing-superiority-profile` expose the
  route policy, route count, auto-channel id, strict-match flags, and per-publication `routing*`
  metadata so operators can verify that Cephalon-native routing, not Wolverine, selected the channel
- outbox-backed event publication reports `accepted` publication state with
  `handoff = outbox` and `deliveryCompletion = pending-dispatch`, keeping publication acceptance
  separate from later dispatch completion
- `event-dispatch-remediation-commands` exposes the bounded command-result read model for
  `retry-now`, `retry-later`, `skip`, `quarantine`, and dispatch-store `dead-letter`, including
  latest, observed-window, message, channel, operation, actor, correlation, reason, and dispatch-outcome drill-downs, while
  `/engine/event-dispatch-remediation-commands*` provides the typed route family for operators who
  do not want to parse technology-surface metadata
- Wolverine or another companion adapter can still move staged dispatch or one subscription to
  provider-managed ownership for brokered or staged dispatch scenarios; the shipped Wolverine path
  now keeps both the dispatch loop and managed subscription retry lanes bounded with max attempts,
  terminal exhausted-attempt failure reporting, and dispatch-store terminal metadata that stops
  poison staged publications from re-entering pending reads, while hosted execution links and
  application-managed reports remain truthful non-provider-owned states

Current `Cephalon.Agentics` highlights:

- each tool entry still carries the operator-facing tool descriptor
- linked `capabilityKeys`, `executionGraphId`, and `hostedExecutionId` now flow through the same surface when the tool declares them
- linked execution-graph and hosted-execution entries also surface the current runtime-story phase and active/inactive state
- invalid linked capability, execution-graph, or hosted-execution references fail when the agentic tool catalog is resolved instead of leaking broken operator metadata
- managed tool execution now flows through `IAgentToolDispatcher` when `AgenticRuntimeOptions.EnableExecution` is enabled
- bounded process-local retry is opt-in through `ExecutionMaxAttempts` and `ExecutionRetryDelayMilliseconds`, with `retryPolicy`, `retryMaxAttempts`, `retryDelayMilliseconds`, `retryDurability`, and `retryScope` metadata on the execution capability and tool surface
- process-local duplicate-completed suppression is opt-in through `EnableExecutionIdempotency` and `ExecutionIdempotencyRetentionMinutes`, with `idempotencyPolicy`, `idempotencyKey`, `idempotencyRetentionMinutes`, `idempotencyDurability`, and `idempotencyScope` metadata on the execution capability and tool surface
- each tool entry reports execution readiness through `executionEnabled`, `executionOwnership`, `executorConfigured`, and `executorCount`; tools without an executor are reported as `awaiting-executor` instead of being described as fully managed
- reported runs flow into `IAgentToolRunCatalog` and surface `runtimeState`, `runCount`, `lastRunId`, `lastOutcome`, retry-scheduled count, retry-pending posture, approval-required posture, duplicate-completed posture, terminal-failure posture, `totalReports`, approval/denial counters, actor/correlation details, and `reported.*` metadata on the same technology surface, while `/engine/agent-tool-runs*` and `snapshot.AgentToolRuns` expose the direct run-state read seam
- operators can now request one bounded managed tool run through
  `POST /engine/agent-tools/{toolId}/runs`; the route records supplied or generated run id, actor,
  correlation id, attempt, arguments, and safe trigger metadata on the same run-state answer
- `GET /engine/agent-tool-runs/retry-pending` filters runs whose latest report is waiting for another process-local attempt; approval-required and denied outcomes are policy decisions, not executor failures, so operators can distinguish "waiting for approval" from broken execution
- `GET /engine/agent-tool-runs/idempotency-duplicates` filters duplicate-completed suppressions so operators can see when a stable run id was intentionally not executed again inside the current process
- `GET /engine/agent-tool-runs/approval-required` and `/engine/agent-tool-runs/terminal-failures` keep approval-blocked runs and terminal failed runs visible without requiring operators to parse generic `reported.*` metadata
- the phase 13 `cell-based-architecture` baseline now also projects `cell-boundaries`, `cell-routes`, `cell-health-isolations`, and `cell-traffic-automations` surfaces whose entries stay aligned with `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, `snapshot.CellBoundaries`, `snapshot.CellRoutes`, `snapshot.CellHealthIsolations`, and `snapshot.CellTrafficAutomations`, so operators can read module ownership, blast-radius posture, source-cell to target-cell routing posture, health-isolation posture, effective automation/trigger/action/materialization modes, policy source, dependency linkage, and transport hints from one shared runtime truth
- the same traffic-automation surface now also carries first-class `providerId` plus `edgeNodeIds` targeting and ASP.NET Core drill-down routes on `/engine/cell-traffic-automations/providers/{providerId}` plus `/engine/cell-traffic-automations/edge-nodes/{edgeNodeId}`, so operators can correlate shared cell posture with provider control planes and edge-node topology without inventing a second traffic registry
- provider-managed automation entries now also expose `providerMaterializerId`, `providerMaterializationState`, `providerMaterializationObservedAtUtc`, and `providerMaterializationError`, so the same technology surface can answer whether startup reconciliation is still `pending`, already `applied`, currently `unavailable`, or last `failed` for the selected provider materializer
- edge-managed automation entries now also expose `edgeMaterializerId`, `edgeMaterializationState`, `edgeMaterializationObservedAtUtc`, and `edgeMaterializationError`, while `edgeMaterialization.*` runtime metadata can carry edge-reported action and targeted-node details, so operators can inspect the same edge-runtime reconciliation posture without a second edge-only traffic-materialization API
- the shared route answer now also exposes derived `materializationState`, `materializationObservedAtUtc`, and `materializationError` plus selection metadata such as `providerSelection.matchingCandidateCount`, `edgeSelection.matchingCandidateCount`, selected priorities, required versus selected dimensions, and `materialization.stateBreakdown`, so `provider-and-edge-managed` routes can publish one truthful overall posture even when provider and edge reconciliation disagree

Current `Cephalon.Retrieval` highlights:

- each collection entry carries descriptor metadata plus managed runtime posture when `KnowledgeRetrieval` is active
- `indexingOwnership` reports `cephalon-managed`, `awaiting-provider`, or `not-configured` so missing document providers do not look like a ready index
- `queryOwnership` reports `cephalon-managed`, `awaiting-index`, `awaiting-provider`, or `not-configured` so query readiness stays separate from collection registration
- `runtimeState`, `freshnessState`, `documentCount`, `queryCount`, latest index outcome fields, and latest query match counts are projected through `/engine/technology-surfaces` and `/engine/snapshot`
- `backgroundReindexingEnabled`, `backgroundReindexingScheduled`,
  `backgroundReindexingOwnership`, `backgroundReindexingCollectionScope`, configured collection
  count, startup-run, delay, and interval metadata are projected beside the index/query posture
- the same typed index-state answer is also available through `/engine/knowledge-indexes*` and
  `snapshot.KnowledgeIndexes`, so operators do not need to parse technology-surface metadata when
  they only need collection index posture
- operators can now request a bounded manual reindex through
  `POST /engine/knowledge-indexes/{collectionId}/reindex`; the route records the supplied or
  generated run id, actor, correlation id, and safe trigger metadata on the same index-state answer
- operators can now request one bounded lexical query through
  `POST /engine/knowledge-indexes/{collectionId}/queries`; the route returns a
  `KnowledgeQueryResult` to the caller and records actor, correlation id, safe metadata, trigger,
  route, query fingerprint, query length, and match count on the same index-state answer
- `lastQueryFingerprint` and `lastQueryLength` are reported instead of raw query text so operator introspection can correlate activity without leaking user prompts or private search terms
- this is a Cephalon-managed lexical in-process baseline with opt-in in-process freshness scheduling; vector search, embeddings, durable search storage, distributed indexes, rerankers, provider-specific search engines, distributed scheduler coordination, and leader-election semantics stay outside the current compatibility promise until a package owns them explicitly

## Trust surface

`GET /engine/trust-policy` exposes the effective package and capability trust snapshot:

- the current `Engine:Trust` policy
- package trust decisions for explicit package assembly loads
- checksum allow-list decisions from `Engine:Trust:AllowedPackageChecksums`
- capability access decisions, including trusted-only and denied capabilities

For ASP.NET Core REST modules, `RequireCapability(...)` can enforce those capability decisions at request time.

## Package policy surface

`GET /engine/package-policy` exposes the effective package-governance rules currently applied by the runtime.

Current payload highlights:

- whether raw assembly-path packages are allowed
- whether package manifests must declare `version`
- whether package manifests must declare minimum or maximum engine versions
- whether package manifests must declare supported target frameworks
- whether package manifests must declare publisher ids, signer fingerprints, signature key ids, signature values, or completed cryptographic signature verification across the declared signature set
- whether package manifests must declare `integrity.sha256`

This is the operator-facing contract for package metadata requirements before trust evaluation even starts.

## Package surface

`GET /engine/packages` exposes the resolved package-loading snapshot for independently shipped modules.

Current payload highlights:

- `kind`, `path`, and `sourcePath` explain how the package was discovered
- `version` comes from `cephalon.package.json` when the package was manifest-driven
- `minimumEngineVersion`, `maximumEngineVersion`, and `supportedTargetFrameworks` expose compatibility intent
- `dependencies` exposes any package-to-package requirements declared by the package manifest, including optional minimum and maximum version bounds
- `distribution` exposes the declared external release channel plus manifest/package fetch hints
- `provenance` exposes the declared source repository, source revision, build URI, and provenance statement URI
- `publisherId`, `publisherDisplayName`, `signatureKeyId`, `signatureFingerprint`, and `signatureCertificateThumbprint` expose the primary package provenance summary kept for backward compatibility
- `signatures` exposes per-signer provenance plus per-signer verification details such as `verificationSource` and certificate thumbprints when a package declares multiple signers
- `isSignatureVerified` and `signatureVerificationReason` explain the aggregate detached-signature verification outcome for the package
- `checksumSha256` exposes the computed hash of the resolved assembly
- `isTrusted` and `trustReason` explain why the current trust policy accepted or rejected the package

This is the main operator surface for package provenance and compatibility diagnostics. When a package declares multiple signers, per-signer outcomes stay visible while the top-level fields continue to summarize the primary signature for existing consumers.

## Telemetry export path

`Cephalon.Engine` emits built-in diagnostics through:

- meter: `Cephalon.Engine`
- activity source: `Cephalon.Engine`
- counters:
  - `cephalon.engine.builds`
  - `cephalon.runtime.transitions`
  - `cephalon.module.transitions`
  - `cephalon.runtime.failures`
  - `cephalon.module.failures`
  - `cephalon.runtime.restarts`

`Cephalon.Observability` reads `Engine:Observability:Telemetry` and logs the effective export guidance on startup.
`Cephalon.Observability.OpenTelemetry` can then turn that same section into a supported OTLP export path for logs, metrics, and traces, including the explicit self-hosted collector defaults that sit on top of the same shared contract.

The same shared contract is also the intended downstream extension point. Teams that install Cephalon packages can build their own provider-specific companion integration by reusing `ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry`, binding an additional provider-specific sub-section, keeping exporter/auth/resource logic in their own package, and optionally publishing a diagnostics convention plus startup summary through `IDiagnosticsConventionContributor` and `IHostedService`.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "Endpoint": "http://localhost:4318",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true
      }
    }
  }
}
```

Self-hosted example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "UseSelfHostedDefaults": true,
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonOpenTelemetry();
```

Operational notes:

- the exporter package is optional and stays outside `Cephalon.Engine`
- registration is skipped when `Engine:Observability:Telemetry:Endpoint` is not configured and `UseSelfHostedDefaults` is not enabled
- `otlp`, `otlp/grpc`, and `otlp/http` are the supported protocol values for the shipped companion package
- when `UseSelfHostedDefaults` is `true` and `Endpoint` is omitted, the package falls back to `http://localhost:4317` for `otlp` / `otlp/grpc` or `http://localhost:4318` for `otlp/http`
- when `otlp/http` is selected, the package appends `/v1/logs`, `/v1/metrics`, and `/v1/traces` automatically from the configured base endpoint
- the self-hosted path also adds `deployment.environment.name` from the active host environment alongside the existing service-name and service-version resource defaults
- downstream companion packages should reuse this same contract instead of introducing a second Cephalon telemetry abstraction; that is the intended path for Cloudflare or internal-provider integrations

## Downstream provider authoring path

Downstream provider packages should build on top of the same `Engine:Observability:Telemetry` contract instead of creating a second Cephalon telemetry abstraction.

Recommended authoring pattern:

- bind `ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry` first
- bind provider-specific settings from `Engine:Observability:Telemetry:{ProviderName}`
- keep provider-specific exporter, auth, trust, and hosted-default logic inside the downstream package
- publish a startup summary through `IHostedService` and a diagnostics convention through `IDiagnosticsConventionContributor`
- reject unsupported protocol or signal combinations explicitly instead of silently dropping signals

Cloudflare note:

- current Cloudflare Workers observability docs focus on Worker-native traces and logs plus exporting OpenTelemetry-compliant traces and logs from Workers to third-party OTLP destinations
- that current path does not yet describe a generic OTLP ingestion target for external Cephalon hosts, and metrics export is still not part of that Worker export story
- until Cloudflare documents a reusable host-side ingestion path that fits Cephalon's runtime model, treat Cloudflare as downstream authoring guidance rather than a first-party `Cephalon.Observability.Cloudflare` package

See [Observability provider authoring](observability-provider-authoring.md) for the recommended package shape and example extension-method pattern.

## AWS observability path

`Cephalon.Observability.Aws` keeps AWS-specific propagation, AWS SDK instrumentation, and hosted AWS resource defaults in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "Endpoint": "http://localhost:4318",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "Aws": {
          "HostedPlatform": "ecs",
          "UseXRayTraceIds": true,
          "UseXRayPropagator": true,
          "EnableAwsSdkInstrumentation": true
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonAws();
```

Operational notes:

- the AWS package is optional and stays outside `Cephalon.Engine`
- registration is skipped when `Engine:Observability:Telemetry:Endpoint` is not configured and `UseSelfHostedDefaults` is not enabled
- `HostedPlatform` can be `ec2`, `ecs`, `eks`, `elasticbeanstalk`, or `lambda`
- the package keeps the shared OTLP exporter contract intact while adding AWS X-Ray-compatible trace IDs, optional AWS X-Ray propagation, and AWS SDK client tracing
- EC2, ECS, EKS, and Elastic Beanstalk use AWS resource detectors, while Lambda uses explicit AWS resource attributes plus optional Lambda context configuration
- if a deployment targets AWS-managed OTLP endpoints directly, prefer an ADOT collector or another SigV4-capable gateway in front of that endpoint instead of baking AWS auth rules into the host

## GCP observability path

`Cephalon.Observability.Gcp` keeps hosted GCP defaults and an optional Google-managed traces/metrics path in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "Gcp": {
          "HostedPlatform": "cloudrun",
          "Location": "asia-southeast1",
          "UseGoogleManagedIngestion": true,
          "UseApplicationDefaultCredentials": true
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonGcp();
```

Operational notes:

- the GCP package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds hosted GCP resource defaults
- when `Engine:Observability:Telemetry:Gcp:UseGoogleManagedIngestion` is `true` and no shared endpoint is configured, the package targets `https://telemetry.googleapis.com` for traces and metrics by using OTLP/HTTP plus Application Default Credentials
- Google-managed ingestion requires `Engine:Observability:Telemetry:Protocol` to stay on `otlp/http`
- direct Google-managed ingestion does not re-route logs; keep logs on the shared collector path or the platform logging path for the target runtime
- `HostedPlatform` can be `gce`, `gke`, `cloudrun`, `appengine`, or `functions`
- `Location` lets the package stamp `location`, `cloud.region`, and when applicable `cloud.availability_zone`

## Huawei Cloud observability path

`Cephalon.Observability.HuaweiCloud` keeps hosted Huawei Cloud defaults and an optional managed APM trace path in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp",
        "ExportLogs": false,
        "ExportMetrics": false,
        "ExportTraces": true,
        "HuaweiCloud": {
          "HostedPlatform": "cce",
          "Region": "ap-southeast-3",
          "UseApmManagedTraceIngestion": true,
          "ApmEndpoint": "https://apm.example.huaweicloud.com:4317",
          "AuthenticationToken": "replace-with-apm-authentication-token"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonHuaweiCloud();
```

Operational notes:

- the Huawei Cloud package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds hosted Huawei Cloud resource defaults
- when `Engine:Observability:Telemetry:HuaweiCloud:UseApmManagedTraceIngestion` is `true` and no shared endpoint is configured, the package targets the configured `ApmEndpoint` for traces by using OTLP/gRPC plus the Huawei Cloud `Authentication` header
- managed APM trace ingestion requires `Engine:Observability:Telemetry:Protocol` to stay on `otlp` or `otlp/grpc`
- direct Huawei Cloud managed APM ingestion does not re-route logs or metrics; keep those signals on the shared collector path or another runtime-specific route
- `HostedPlatform` can be `ecs`, `cce`, or `functiongraph`
- `Region` lets the package stamp `cloud.region` when a deployment wants that value to stay explicit

## Oracle Cloud observability path

`Cephalon.Observability.OracleCloud` keeps hosted Oracle Cloud defaults and an optional Oracle Cloud APM managed traces/metrics path in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": false,
        "ExportMetrics": true,
        "ExportTraces": true,
        "OracleCloud": {
          "HostedPlatform": "oke",
          "Region": "us-ashburn-1",
          "UseManagedOpenTelemetryIngestion": true,
          "DataUploadEndpoint": "https://aaaaaaaaaaaaaaaaaaaaaa.apm-agt.us-ashburn-1.oci.oraclecloud.com",
          "UsePublicTraceDataKey": true,
          "TraceDataKey": "replace-with-public-or-private-trace-data-key",
          "MetricsDataKey": "replace-with-private-metrics-data-key"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonOracleCloud();
```

Operational notes:

- the Oracle Cloud package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds hosted Oracle Cloud resource defaults
- when `Engine:Observability:Telemetry:OracleCloud:UseManagedOpenTelemetryIngestion` is `true` and no shared endpoint is configured, the package builds Oracle Cloud APM OTLP/HTTP traces and metrics endpoints from `DataUploadEndpoint`
- managed Oracle Cloud APM ingestion requires `Engine:Observability:Telemetry:Protocol` to stay on `otlp/http`
- direct managed trace ingestion requires `TraceDataKey`; direct managed metrics ingestion requires `MetricsDataKey`
- `UsePublicTraceDataKey` switches the trace path between Oracle Cloud APM public and private trace-key ingestion; metrics always stay on the private-key path
- direct Oracle Cloud APM managed ingestion does not re-route logs; keep logs on the shared collector path, Oracle Log Analytics, or another runtime-specific route
- `HostedPlatform` can be `compute`, `oke`, or `functions`
- `Region` lets the package stamp `cloud.region` when a deployment wants that value to stay explicit

## Grafana Cloud observability path

`Cephalon.Observability.GrafanaCloud` keeps Grafana Cloud OTLP endpoint wiring and access-policy authentication guidance in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "GrafanaCloud": {
          "UseDirectGrafanaCloudEndpoint": true,
          "Endpoint": "https://otlp-gateway-prod-us-central-0.grafana.net/otlp",
          "InstanceId": "replace-with-grafana-cloud-instance-id",
          "AccessPolicyToken": "replace-with-grafana-cloud-access-policy-token",
          "ServiceNamespace": "checkout"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonGrafanaCloud();
```

Operational notes:

- the Grafana Cloud package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds Grafana-friendly resource context
- when `Engine:Observability:Telemetry:GrafanaCloud:UseDirectGrafanaCloudEndpoint` is `true` and no shared endpoint is configured, the package targets the configured Grafana Cloud OTLP endpoint directly
- direct Grafana Cloud mode accepts either `Headers` in standard OTLP `key=value` format or the structured `InstanceId` plus `AccessPolicyToken` pair that the package converts into a Basic `Authorization` header
- `Protocol` can stay on `otlp`, `otlp/grpc`, or `otlp/http`; for HTTP/protobuf the package appends `/v1/traces`, `/v1/metrics`, and `/v1/logs` automatically
- `ServiceNamespace` lets the package stamp `service.namespace`, while the active host environment still contributes `deployment.environment.name`

## New Relic observability path

`Cephalon.Observability.NewRelic` keeps New Relic native OTLP endpoint wiring and `api-key` authentication guidance in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "NewRelic": {
          "UseNativeOtlpEndpoint": true,
          "Region": "eu",
          "LicenseKey": "replace-with-newrelic-license-key",
          "ServiceNamespace": "checkout"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonNewRelic();
```

Operational notes:

- the New Relic package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds explicit resource context
- when `Engine:Observability:Telemetry:NewRelic:UseNativeOtlpEndpoint` is `true` and no shared endpoint is configured, the package targets either the configured New Relic OTLP endpoint or the documented regional endpoint derived from `Region`
- direct New Relic mode accepts either `Headers` in standard OTLP `key=value` format or the structured `LicenseKey` value that the package converts into the required `api-key` header
- `Protocol` can stay on `otlp`, `otlp/grpc`, or `otlp/http`; New Relic currently recommends `otlp/http`, and for HTTP/protobuf the package appends `/v1/traces`, `/v1/metrics`, and `/v1/logs` automatically
- `Region` can be `us`, `eu`, or `fedramp`; if omitted, the package defaults to the New Relic US OTLP endpoint
- `ServiceNamespace` lets the package stamp `service.namespace`, while the active host environment still contributes `deployment.environment.name`

## Alibaba Cloud observability path

`Cephalon.Observability.AlibabaCloud` keeps hosted Alibaba Cloud defaults and an optional managed OpenTelemetry traces/metrics path in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp",
        "ExportLogs": false,
        "ExportMetrics": true,
        "ExportTraces": true,
        "AlibabaCloud": {
          "HostedPlatform": "ecs",
          "Region": "cn-hangzhou",
          "UseManagedOpenTelemetryIngestion": true,
          "ManagedGrpcEndpoint": "https://otel.example.aliyuncs.com:8000",
          "AuthenticationToken": "replace-with-managed-otel-authentication-token"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonAlibabaCloud();
```

Operational notes:

- the Alibaba Cloud package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds hosted Alibaba Cloud resource defaults
- when `Engine:Observability:Telemetry:AlibabaCloud:UseManagedOpenTelemetryIngestion` is `true` and no shared endpoint is configured, the package targets the configured Alibaba Cloud Managed Service for OpenTelemetry path for traces and metrics
- managed Alibaba Cloud OTLP/gRPC ingestion requires `Engine:Observability:Telemetry:Protocol` to stay on `otlp` or `otlp/grpc` and uses the Alibaba Cloud `Authentication` header
- managed Alibaba Cloud OTLP/HTTP ingestion requires `Engine:Observability:Telemetry:Protocol` to stay on `otlp/http` and uses the configured signal-specific traces and metrics endpoints directly
- direct Alibaba Cloud managed ingestion does not re-route logs; keep logs on the shared collector path, SLS, or another runtime-specific route
- `HostedPlatform` can be `ecs`, `fc`, `functioncompute`, or `openshift`
- `Region` lets the package stamp `cloud.region` when a deployment wants that value to stay explicit

## OpenShift observability path

`Cephalon.Observability.OpenShift` keeps OpenShift collector discovery, trust material, and hosted cluster defaults in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "OpenShift": {
          "HostedPlatform": "openshift",
          "ClusterName": "prod-cluster",
          "Namespace": "payments",
          "UseInClusterCollectorService": true,
          "CollectorServiceName": "otel-collector",
          "CollectorNamespace": "observability"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonOpenShift();
```

Operational notes:

- the OpenShift package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds OpenShift resource defaults
- when `Engine:Observability:Telemetry:OpenShift:UseInClusterCollectorService` is `true` and no shared endpoint is configured, the package targets `http(s)://{service}.{namespace}.svc.cluster.local:{port}`
- `HostedPlatform` can be `openshift`, `aro`, or `rosa`
- `ClusterName`, `Namespace`, `POD_NAMESPACE`, and `HOSTNAME` let the package stamp `k8s.cluster.name`, `k8s.namespace.name`, `service.namespace`, and `k8s.pod.name`; `aro` and `rosa` also stamp `cloud.provider`
- `TrustedCaCertificatePath` can be used for HTTPS OTLP/HTTP traces and metrics when an in-cluster collector, route, or gateway uses a cluster-local CA bundle
- the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, so configurations that need `TrustedCaCertificatePath` for OTLP/HTTP logs are rejected early instead of being treated as supported
- `Headers` lets the package pass raw OTLP header values through to the target collector or gateway when a route expects explicit headers

## Kubernetes observability path

`Cephalon.Observability.Kubernetes` keeps platform-neutral Kubernetes collector discovery, cluster-local trust material, and Kubernetes resource defaults in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "Kubernetes": {
          "ClusterName": "prod-cluster",
          "Namespace": "payments",
          "PodName": "payments-api-5799c",
          "NodeName": "node-a",
          "ContainerName": "api",
          "UseInClusterCollectorService": true,
          "CollectorServiceName": "otel-collector",
          "CollectorNamespace": "observability"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonKubernetes();
```

Operational notes:

- the Kubernetes package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds Kubernetes resource defaults
- when `Engine:Observability:Telemetry:Kubernetes:UseInClusterCollectorService` is `true` and no shared endpoint is configured, the package targets `http(s)://{service}.{namespace}.{serviceDnsSuffix}:{port}`
- `ClusterName`, `Namespace`, `PodName`, `PodUid`, `NodeName`, and `ContainerName` keep generic Kubernetes resource attributes explicit without forcing teams into a vendor-specific companion package
- `POD_NAMESPACE`, `POD_NAME`, `HOSTNAME`, `POD_UID`, `NODE_NAME`, and `CONTAINER_NAME` can fill those same resource attributes when the deployment already injects them through the Kubernetes downward API or runtime environment
- `ServiceDnsSuffix` defaults to `svc.cluster.local` and stays configurable for clusters that use a non-default service DNS suffix
- `TrustedCaCertificatePath` can be used for HTTPS OTLP/HTTP traces and metrics when an in-cluster collector, gateway, or route uses a cluster-local CA bundle
- the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, so configurations that need `TrustedCaCertificatePath` for OTLP/HTTP logs are rejected early instead of being treated as supported
- use this package for generic or self-managed Kubernetes clusters; if a deployment also needs provider-specific propagation, managed-ingestion, or hosted defaults, pair the shared telemetry contract with a more specific companion package instead

## DigitalOcean observability path

`Cephalon.Observability.DigitalOcean` keeps DigitalOcean collector defaults, best-effort Droplet metadata, and hosted runtime guidance in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "DigitalOcean": {
          "HostedPlatform": "doks",
          "Region": "sgp1",
          "ClusterName": "prod-cluster",
          "Namespace": "payments",
          "UseInClusterCollectorService": true,
          "CollectorServiceName": "otel-collector",
          "CollectorNamespace": "observability"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonDigitalOcean();
```

Operational notes:

- the DigitalOcean package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds DigitalOcean resource defaults
- when `Engine:Observability:Telemetry:DigitalOcean:UseInClusterCollectorService` is `true` and no shared endpoint is configured, the package targets `http(s)://{service}.{namespace}.svc.cluster.local:{port}` for DOKS deployments
- `HostedPlatform` can be `droplet`, `doks`, or `app-platform`
- `UseDropletMetadataDefaults` turns on a short best-effort call to the Droplet metadata service so the package can fill `host.id`, `host.name`, and `cloud.region` when they were not configured directly
- App Platform context stays explicit: set `AppId` and `AppUrl` directly, or bind `${APP_ID}` and `${APP_URL}` into runtime environment variables with those names if you want the package to pick them up automatically
- `ClusterName`, `Namespace`, `POD_NAMESPACE`, and `HOSTNAME` let the package stamp `k8s.cluster.name`, `k8s.namespace.name`, `service.namespace`, and `k8s.pod.name` for DOKS workloads
- `TrustedCaCertificatePath` can be used for HTTPS OTLP/HTTP traces and metrics when a shared or in-cluster collector uses a non-system CA bundle
- the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, so configurations that need `TrustedCaCertificatePath` for OTLP/HTTP logs are rejected early instead of being treated as supported
- there is no first-party DigitalOcean managed OTLP endpoint in this package; use the shared collector path, self-hosted defaults, or a self-managed gateway instead of treating this companion as a vendor-direct exporter

## Tanzu observability path

`Cephalon.Observability.Tanzu` keeps Tanzu-specific hosted defaults and trace-focused proxy handoff in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "ExportLogs": false,
        "ExportMetrics": false,
        "ExportTraces": true,
        "Tanzu": {
          "HostedPlatform": "tap",
          "ClusterName": "prod-cluster",
          "Namespace": "payments",
          "UseInClusterProxyService": true,
          "ProxyServiceName": "wavefront-proxy",
          "ProxyNamespace": "observability",
          "ProxyPort": 4318
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonTanzu();
```

Operational notes:

- the Tanzu package is optional and stays outside `Cephalon.Engine`
- when `Engine:Observability:Telemetry:Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and only adds Tanzu resource defaults
- when `Engine:Observability:Telemetry:Tanzu:UseInClusterProxyService` is `true` and no shared endpoint is configured, the package targets `http(s)://{service}.{namespace}.svc.cluster.local:{port}` for trace-focused proxy handoff
- `HostedPlatform` can be `tkg`, `tkgi`, or `tap`
- `ClusterName`, `Namespace`, `POD_NAMESPACE`, and `HOSTNAME` let the package stamp `k8s.cluster.name`, `k8s.namespace.name`, `service.namespace`, and `k8s.pod.name`; the hosted-platform selection also stamps `cloud.provider=vmware` plus a package-specific `cloud.platform`
- `ProxyPort` stays required on purpose so the proxy handoff path remains explicit instead of pretending the current Tanzu docs expose one generic vendor-wide OTLP port
- `ProxyPath` lets teams keep an explicit base path when the proxy or route is mounted away from `/`
- `TrustedCaCertificatePath` can be used for HTTPS OTLP/HTTP traces and metrics when a shared collector, gateway, or Tanzu proxy route uses a non-system CA bundle
- the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, so configurations that need `TrustedCaCertificatePath` for OTLP/HTTP logs are rejected early instead of being treated as supported
- the first-party Tanzu proxy handoff mode intentionally supports traces only; keep logs and metrics on the shared collector path or the explicit self-hosted defaults instead of treating this companion as a generic managed exporter

## Azure Monitor exporter path

`Cephalon.Observability.AzureMonitor` keeps Azure Monitor / Application Insights export wiring in a dedicated companion package on top of the same shared `Engine:Observability:Telemetry` contract.

Example:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "AzureMonitor": {
          "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000",
          "UseDefaultAzureCredential": true,
          "HostedPlatform": "appservice"
        }
      }
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonAzureMonitor();
```

Operational notes:

- the Azure Monitor package is optional and stays outside `Cephalon.Engine`
- registration is skipped when `Engine:Observability:Telemetry:AzureMonitor:ConnectionString` is not configured
- `UseDefaultAzureCredential` adds Azure Active Directory authentication on top of the configured connection string endpoint
- `HostedPlatform` can be `appservice`, `functions`, `aks`, `containerapps`, or `vm`
- when `HostedPlatform` is configured, the package adds `cloud.provider=azure`, the corresponding `cloud.platform` value, and `deployment.environment.name` when the host environment name is set

## Serilog provider path

`Cephalon.Observability.Serilog` lets hosts keep logging through injected `ILogger<T>` services while routing the resulting events through Serilog sinks, enrichers, and formatting.

Example:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ],
    "Enrich": [ "FromLogContext" ],
    "Properties": {
      "Application": "Cephalon.Host"
    }
  }
}
```

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonSerilog();
```

Operational notes:

- the Serilog package is optional and stays outside `Cephalon.Engine` and `Cephalon.Observability`
- registration continues to flow through `Microsoft.Extensions.Logging.ILogger`; Cephalon does not introduce a separate logging abstraction
- the package reads the standard top-level `Serilog` configuration section and skips registration when that section is absent and no code-based Serilog callback is supplied
- hosts can still append sinks, enrichers, or policy with code when configuration alone is not enough
- bootstrap logging before the host builder exists stays an explicit host concern rather than hidden in the Cephalon runtime layer

## ASP.NET Core request and response logging

`Cephalon.AspNetCore` can opt into HTTP request/response logging through `Engine:Observability:HttpLogging`.
This keeps the feature in the shared host surface instead of introducing a second logging abstraction, and it can capture bounded textual request/response bodies when teams explicitly enable it.

Example:

```json
{
  "Engine": {
    "Observability": {
      "HttpLogging": {
        "Enabled": true,
        "LogRequestBody": true,
        "LogResponseBody": true,
        "RequestBodyLimit": 4096,
        "ResponseBodyLimit": 4096,
        "RedactSensitiveValues": true,
        "RedactedFieldNames": [ "password", "token", "secret", "apiKey" ],
        "RedactionValue": "[REDACTED]"
      }
    }
  }
}
```

Optional code-level override:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCephalonHttpLogging(options =>
{
    options.Enabled = true;
    options.LogRequestBody = true;
    options.LogResponseBody = true;
    options.RequestBodyLimit = 4096;
    options.ResponseBodyLimit = 4096;
    options.RedactSensitiveValues = true;
    options.RedactedFieldNames = ["password", "token", "secret", "apiKey"];
    options.RedactionValue = "[REDACTED]";
});
builder.AddCephalon();
```

Operational notes:

- `AddCephalon()` already registers the `Engine:Observability:HttpLogging` contract, so the extra method is only needed for code-based overrides
- request/response body capture is opt-in and limited to textual payloads such as `text/*`, JSON, XML, GraphQL, JavaScript, and form payloads
- sensitive query-string and payload fields such as `password`, `token`, `secret`, `apiKey`, `authorization`, and `cookie` are redacted by default before the log event is written, including JSON bodies, form payloads, and `text/plain` key/value or header-style content such as `Authorization: Bearer ...`
- teams can override the sensitive-field list and placeholder through `RedactedFieldNames` and `RedactionValue` when a host needs stricter or domain-specific masking
- request scopes carry `RequestId`, `TraceId`, `SpanId`, and `TraceParent`, so logs written inside the request pipeline keep the same correlation context
- `/engine/diagnostics` publishes the ASP.NET Core event-id range for request start, request body, response completion, response body, and request failure events
- the same correlation values flow through Serilog when `Cephalon.Observability.Serilog` is enabled
- `Cephalon.Observability.OpenTelemetry` now adds ASP.NET Core tracing instrumentation, so OTLP-exported request traces can be matched with the corresponding Cephalon HTTP logs

## Release-validation guidance

`.\scripts\validate-operational-conventions.ps1` is the focused operational validation pass for health and export conventions.
It executes a curated test suite that validates:

- ASP.NET Core `/health/live`, `/health/ready`, `/engine/diagnostics`, and `/engine/dependencies` behavior
- ASP.NET Core request/response logging, bounded body capture, and trace/log correlation behavior
- worker-host parity through `RuntimeHealthEvaluator`
- startup manifest and telemetry-export guidance emitted by `Cephalon.Observability`, including self-hosted OTLP default endpoint guidance
- Serilog provider wiring through `Cephalon.Observability.Serilog`
- Alibaba Cloud-hosted defaults and managed OpenTelemetry traces/metrics through `Cephalon.Observability.AlibabaCloud`
- AWS-hosted OTLP defaults through `Cephalon.Observability.Aws`
- Grafana Cloud direct endpoint wiring through `Cephalon.Observability.GrafanaCloud`
- New Relic native OTLP defaults through `Cephalon.Observability.NewRelic`
- GCP-hosted defaults through `Cephalon.Observability.Gcp`
- Huawei Cloud-hosted defaults and managed APM traces through `Cephalon.Observability.HuaweiCloud`
- Kubernetes in-cluster collector defaults through `Cephalon.Observability.Kubernetes`
- OpenShift in-cluster collector defaults through `Cephalon.Observability.OpenShift`
- Tanzu proxy trace handoff defaults through `Cephalon.Observability.Tanzu`
- OTLP exporter wiring through `Cephalon.Observability.OpenTelemetry`, including the explicit self-hosted collector-default path

`.\scripts\validate-release.ps1` now runs that focused suite by default in addition to the broader repo test, benchmark, and reference-doc flow.
Use `-SkipOperationalConventions` only when you intentionally want the wider release flow without the named operational replay.

`.\scripts\validate-phase8-conventions.ps1` is the focused validation pass for the current phase-8 architecture/data/security/starter baseline.
It executes a curated suite that validates:

- structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` settings plus phase-8 app-profile truth
- host-agnostic phase-8 contracts, runtime catalogs, and runtime-snapshot answers for data products, projections, inboxes, outboxes, audit stores, and authorization policies
- `Cephalon.Data`, `Cephalon.Data.EntityFramework`, and `Cephalon.Ids.Sfid` through the shipped relational-first CQRS, inbox, outbox, and `Sfid` baseline
- `Cephalon.Eventing` plus optional `Cephalon.Eventing.Wolverine` through the staged publication, declarative subscription, public execution-binding catalog, public execution-readiness catalog, runtime-reporting, native bounded process-local retry/idempotency/scheduled-publication proofs, bounded Wolverine managed-dispatch retry, bounded Wolverine managed-subscription retry, terminal exhausted-attempt failure posture, dispatch-store terminal failure behavior, and adapter-surface path
- `Cephalon.Identity`, `Cephalon.Identity.AspNetCore`, `Cephalon.MultiTenancy`, and `Cephalon.Audit` through their runtime surfaces, adapter behavior, and package/reference-doc truth
- low-ceremony starter output across `Cephalon.Scaffolding`, `Cephalon.Cli`, `Cephalon.TemplatePack`, adoption docs, and starter samples so generated apps stay aligned with the runtime story

`.\scripts\validate-release.ps1` now also runs that focused phase-8 suite by default.
Use `-SkipPhase8Conventions` only when you intentionally want the wider release flow without the named phase-8 replay.

For the Docker Desktop / WSL-friendly operator smoke path, `.\scripts\validate-container-runtime.ps1` runs `samples/Cephalon.Sample.ModularMonolith/compose.yaml`, waits for collector health plus `/health/ready`, `/engine`, `/engine/snapshot`, and `/api/v1/catalog/overview`, and then tears the stack down. That containerized validation stays optional on purpose, so Docker is not a hard dependency of `.\scripts\validate-release.ps1`.

## Worker hosts

Worker hosts do not expose HTTP health routes, but the same runtime health semantics are available through `RuntimeHealthEvaluator` in DI.

That keeps readiness/liveness logic shared across:

- `Cephalon.AspNetCore`
- `Cephalon.Worker`
- `Cephalon.Observability`

## Related documents

- `docs/architecture.md`
- `docs/container-runtime.md`
- `docs/operational-hardening-gap-inventory.md`
- `docs/runtime-failure-policy.md`
- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`
