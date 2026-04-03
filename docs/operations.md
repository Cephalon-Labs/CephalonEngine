# Cephalon Operations

This document captures the current operational surface for Cephalon as of `April 3, 2026`.

For the active phase-2 follow-through inventory, see `docs/operational-hardening-gap-inventory.md`.

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
- `Cephalon.Observability`: `3000-3006`
- `Cephalon.Observability.Gcp`: `3111-3111`
- `Cephalon.Observability.HuaweiCloud`: `3112-3112`
- `Cephalon.Observability.AlibabaCloud`: `3113-3113`
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

## Technology surface

`GET /engine/technology-surfaces` exposes the active runtime surfaces projected by selected technology packs.

Current `Cephalon.Agentics` highlights:

- each tool entry still carries the operator-facing tool descriptor
- linked `capabilityKeys`, `executionGraphId`, and `hostedExecutionId` now flow through the same surface when the tool declares them
- linked execution-graph and hosted-execution entries also surface the current runtime-story phase and active/inactive state
- invalid linked capability, execution-graph, or hosted-execution references fail when the agentic tool catalog is resolved instead of leaking broken operator metadata

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
- GCP-hosted defaults through `Cephalon.Observability.Gcp`
- Huawei Cloud-hosted defaults and managed APM traces through `Cephalon.Observability.HuaweiCloud`
- Kubernetes in-cluster collector defaults through `Cephalon.Observability.Kubernetes`
- OpenShift in-cluster collector defaults through `Cephalon.Observability.OpenShift`
- Tanzu proxy trace handoff defaults through `Cephalon.Observability.Tanzu`
- OTLP exporter wiring through `Cephalon.Observability.OpenTelemetry`, including the explicit self-hosted collector-default path

`.\scripts\validate-release.ps1` now runs that focused suite by default in addition to the broader repo test, benchmark, and reference-doc flow.
Use `-SkipOperationalConventions` only when you intentionally want the wider release flow without the named operational replay.

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
