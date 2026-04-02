# Cephalon Operations

This document captures the current operational surface for Cephalon as of `April 2, 2026`.

For the active phase-2 follow-through inventory, see `docs/operational-hardening-gap-inventory.md`.

## Health surfaces

ASP.NET Core hosts that call `app.MapCephalon()` now expose three health routes:

- `/health`
- `/health/live`
- `/health/ready`

Health responses are JSON and include the check status, duration, and runtime-specific details such as restart count and the most recent failure context.
When modules or installed packages register `IDependencyHealthContributor`, those responses also include dependency details. `Cephalon.Observability.CassandraDependencies`, `Cephalon.Observability.ConsulDependencies`, `Cephalon.Observability.ElasticsearchDependencies`, `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.KafkaDependencies`, `Cephalon.Observability.MemcachedDependencies`, `Cephalon.Observability.MongoDbDependencies`, `Cephalon.Observability.MqttDependencies`, `Cephalon.Observability.MySqlDependencies`, `Cephalon.Observability.NatsDependencies`, `Cephalon.Observability.Neo4jDependencies`, `Cephalon.Observability.OracleDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, `Cephalon.Observability.RedisDependencies`, and `Cephalon.Observability.SqlServerDependencies` are the shipped companion packages for turning external upstreams into that dependency-health surface.

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
- published diagnostics conventions and event-id catalogs for the active engine and companion packages
- the current liveness report
- the current readiness report
- dependency details folded into those runtime reports
- the mapped health routes

Current shipped event-id ranges include:

- `Cephalon.Engine`: `2000-2003`
- `Cephalon.Observability`: `3000-3006`
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
        "ResponseBodyLimit": 4096
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
});
builder.AddCephalon();
```

Operational notes:

- `AddCephalon()` already registers the `Engine:Observability:HttpLogging` contract, so the extra method is only needed for code-based overrides
- request/response body capture is opt-in and limited to textual payloads such as `text/*`, JSON, XML, GraphQL, JavaScript, and form payloads
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
- startup manifest and telemetry-export guidance emitted by `Cephalon.Observability`
- Serilog provider wiring through `Cephalon.Observability.Serilog`
- OTLP exporter wiring through `Cephalon.Observability.OpenTelemetry`

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
