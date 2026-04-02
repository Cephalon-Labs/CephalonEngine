# Cephalon Operations

This document captures the current operational surface for Cephalon as of `April 2, 2026`.

For the active phase-2 follow-through inventory, see `docs/operational-hardening-gap-inventory.md`.

## Health surfaces

ASP.NET Core hosts that call `app.MapCephalon()` now expose three health routes:

- `/health`
- `/health/live`
- `/health/ready`

Health responses are JSON and include the check status, duration, and runtime-specific details such as restart count and the most recent failure context.
When modules or installed packages register `IDependencyHealthContributor`, those responses also include dependency details. `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.MongoDbDependencies`, `Cephalon.Observability.MySqlDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, `Cephalon.Observability.RedisDependencies`, and `Cephalon.Observability.SqlServerDependencies` are the shipped companion packages for turning external upstreams into that dependency-health surface.

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
- published diagnostics conventions and event-id catalogs for the active engine and companion packages
- the current liveness report
- the current readiness report
- dependency details folded into those runtime reports
- the mapped health routes

Current shipped event-id ranges include:

- `Cephalon.Engine`: `2000-2003`
- `Cephalon.Observability`: `3000-3006`
- `Cephalon.Observability.HttpDependencies`: `3100-3101`
- `Cephalon.Observability.RedisDependencies`: `3120-3121`
- `Cephalon.Observability.PostgresDependencies`: `3122-3123`
- `Cephalon.Observability.RabbitMqDependencies`: `3124-3125`
- `Cephalon.Observability.SqlServerDependencies`: `3126-3127`
- `Cephalon.Observability.MySqlDependencies`: `3128-3129`
- `Cephalon.Observability.MongoDbDependencies`: `3130-3131`

This is the quickest way to discover the engine's observability contract without opening code.

## Runtime story surface

`GET /engine/runtime-story` exposes the operator-facing lifecycle narrative in one place:

- the current runtime status and last failure context
- loaded package metadata for the active runtime
- per-module lifecycle state, including loaded, initialized, started, and stopped timestamps
- an ordered timeline for package load, module transitions, runtime transitions, restart attempts, and failures

This is the quickest way to answer the adjacent operational question that `/engine/status`, `/engine/packages`, `/engine/diagnostics`, and `/engine/snapshot` already support in pieces: what loaded, what started, what failed, and why.

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
- `publisherId`, `publisherDisplayName`, `signatureKeyId`, and `signatureFingerprint` expose the primary package provenance summary kept for backward compatibility
- `signatures` exposes per-signer provenance and per-signer verification details when a package declares multiple signers
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
`Cephalon.Observability.OpenTelemetry` can then turn that same section into a supported OTLP export path for logs, metrics, and traces.

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

Host registration example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonOpenTelemetry();
```

Operational notes:

- the exporter package is optional and stays outside `Cephalon.Engine`
- registration is skipped when `Engine:Observability:Telemetry:Endpoint` is not configured
- `otlp`, `otlp/grpc`, and `otlp/http` are the supported protocol values for the shipped companion package
- when `otlp/http` is selected, the package appends `/v1/logs`, `/v1/metrics`, and `/v1/traces` automatically from the configured base endpoint

## Worker hosts

Worker hosts do not expose HTTP health routes, but the same runtime health semantics are available through `RuntimeHealthEvaluator` in DI.

That keeps readiness/liveness logic shared across:

- `Cephalon.AspNetCore`
- `Cephalon.Worker`
- `Cephalon.Observability`

## Related documents

- `docs/architecture.md`
- `docs/operational-hardening-gap-inventory.md`
- `docs/runtime-failure-policy.md`
- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`
