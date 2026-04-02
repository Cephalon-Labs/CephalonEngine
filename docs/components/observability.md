# Cephalon.Observability

`Cephalon.Observability` is the diagnostics companion package for Cephalon hosts.

## What it owns

- observability option binding
- startup summaries for manifests, modules, capabilities, and telemetry guidance
- host-friendly registration over the engine's built-in logs, metrics, and activity source
- the shared telemetry configuration contract consumed by optional exporter companion packages
- the shared dependency-health configuration convention consumed by optional upstream probe companion packages

## Main surfaces

- `Configuration/ObservabilityOptions.cs`
- `Configuration/TelemetryExportOptions.cs`
- `Hosting/ObservabilityServiceCollectionExtensions.cs`
- `Hosting/ManifestSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package does not replace the engine's diagnostics primitives. It turns them into conventions and startup behavior that ASP.NET Core and worker hosts can opt into consistently, and it emits a startup summary of the active diagnostics catalog so operators can see which package-level event-id ranges are live. When a host needs reusable Consul control-plane probes, pair it with `Cephalon.Observability.ConsulDependencies`; when a host needs reusable Elasticsearch cluster probes, pair it with `Cephalon.Observability.ElasticsearchDependencies`; when a host needs reusable external API probes, pair it with `Cephalon.Observability.HttpDependencies`; when a host needs reusable Kafka probes, pair it with `Cephalon.Observability.KafkaDependencies`; when a host needs reusable Memcached cache probes, pair it with `Cephalon.Observability.MemcachedDependencies`; when a host needs reusable MongoDB probes, pair it with `Cephalon.Observability.MongoDbDependencies`; when a host needs reusable MQTT probes, pair it with `Cephalon.Observability.MqttDependencies`; when a host needs reusable MySQL or MariaDB probes, pair it with `Cephalon.Observability.MySqlDependencies`; when a host needs reusable NATS probes, pair it with `Cephalon.Observability.NatsDependencies`; when a host needs reusable Postgres probes, pair it with `Cephalon.Observability.PostgresDependencies`; when a host needs reusable RabbitMQ probes, pair it with `Cephalon.Observability.RabbitMqDependencies`; when a host needs reusable Redis or cache probes, pair it with `Cephalon.Observability.RedisDependencies`; when a host needs reusable SQL Server or Azure SQL probes, pair it with `Cephalon.Observability.SqlServerDependencies`; when a host needs a supported OTLP export path, pair it with `Cephalon.Observability.OpenTelemetry`; and when a host needs a supported Serilog provider path over the same shared `ILogger` contract, pair it with `Cephalon.Observability.Serilog` instead of pulling provider-specific dependencies into the engine or this baseline package.

## Related docs

- [Cephalon.Observability.ConsulDependencies](observability-consul-dependencies.md)
- [Cephalon.Observability.ElasticsearchDependencies](observability-elasticsearch-dependencies.md)
- [Cephalon.Observability.HttpDependencies](observability-http-dependencies.md)
- [Cephalon.Observability.KafkaDependencies](observability-kafka-dependencies.md)
- [Cephalon.Observability.MemcachedDependencies](observability-memcached-dependencies.md)
- [Cephalon.Observability.MongoDbDependencies](observability-mongodb-dependencies.md)
- [Cephalon.Observability.MqttDependencies](observability-mqtt-dependencies.md)
- [Cephalon.Observability.MySqlDependencies](observability-mysql-dependencies.md)
- [Cephalon.Observability.NatsDependencies](observability-nats-dependencies.md)
- [Cephalon.Observability.PostgresDependencies](observability-postgres-dependencies.md)
- [Cephalon.Observability.RabbitMqDependencies](observability-rabbitmq-dependencies.md)
- [Cephalon.Observability.RedisDependencies](observability-redis-dependencies.md)
- [Cephalon.Observability.SqlServerDependencies](observability-sqlserver-dependencies.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Cephalon.Observability.Serilog](observability-serilog.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
