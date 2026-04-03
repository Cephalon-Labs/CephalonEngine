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

This package does not replace the engine's diagnostics primitives. It turns them into conventions and startup behavior that ASP.NET Core and worker hosts can opt into consistently, and it emits a startup summary of the active diagnostics catalog so operators can see which package-level event-id ranges are live. When a host needs reusable Cassandra contact-point and CQL health probes, pair it with `Cephalon.Observability.CassandraDependencies`; when a host needs reusable ClickHouse probes, pair it with `Cephalon.Observability.ClickHouseDependencies`; when a host needs reusable Consul control-plane probes, pair it with `Cephalon.Observability.ConsulDependencies`; when a host needs reusable Elasticsearch cluster probes, pair it with `Cephalon.Observability.ElasticsearchDependencies`; when a host needs reusable external API probes, pair it with `Cephalon.Observability.HttpDependencies`; when a host needs reusable Kafka probes, pair it with `Cephalon.Observability.KafkaDependencies`; when a host needs reusable Memcached cache probes, pair it with `Cephalon.Observability.MemcachedDependencies`; when a host needs reusable MongoDB probes, pair it with `Cephalon.Observability.MongoDbDependencies`; when a host needs reusable MQTT probes, pair it with `Cephalon.Observability.MqttDependencies`; when a host needs reusable MySQL or MariaDB probes, pair it with `Cephalon.Observability.MySqlDependencies`; when a host needs reusable NATS probes, pair it with `Cephalon.Observability.NatsDependencies`; when a host needs reusable Neo4j Bolt or routing probes, pair it with `Cephalon.Observability.Neo4jDependencies`; when a host needs reusable OpenSearch cluster-health probes, pair it with `Cephalon.Observability.OpenSearchDependencies`; when a host needs reusable Oracle Database probes, pair it with `Cephalon.Observability.OracleDependencies`; when a host needs reusable Postgres probes, pair it with `Cephalon.Observability.PostgresDependencies`; when a host needs reusable RabbitMQ probes, pair it with `Cephalon.Observability.RabbitMqDependencies`; when a host needs reusable Redis or cache probes, pair it with `Cephalon.Observability.RedisDependencies`; when a host needs reusable SQL Server or Azure SQL probes, pair it with `Cephalon.Observability.SqlServerDependencies`; when a host needs a supported OTLP export path, pair it with `Cephalon.Observability.OpenTelemetry`; when a host needs a generic Kubernetes collector-first path plus cluster-local resource defaults on top of that same shared telemetry contract, pair it with `Cephalon.Observability.Kubernetes`; when a host needs Alibaba Cloud-hosted OTLP defaults or managed OpenTelemetry traces/metrics on top of that same shared telemetry contract, pair it with `Cephalon.Observability.AlibabaCloud`; when a host needs AWS X-Ray-compatible tracing and hosted AWS resource defaults on top of that same shared telemetry contract, pair it with `Cephalon.Observability.Aws`; when a host needs DigitalOcean collector defaults and hosted runtime guidance on top of that same shared telemetry contract, pair it with `Cephalon.Observability.DigitalOcean`; when a host needs Grafana Cloud OTLP endpoint wiring and access-policy authentication guidance on top of that same shared telemetry contract, pair it with `Cephalon.Observability.GrafanaCloud`; when a host needs GCP-hosted OTLP defaults plus an optional Google-managed traces/metrics path on top of that same shared telemetry contract, pair it with `Cephalon.Observability.Gcp`; when a host needs Huawei Cloud-hosted OTLP defaults or managed APM trace ingestion on top of that same shared telemetry contract, pair it with `Cephalon.Observability.HuaweiCloud`; when a host needs Oracle Cloud APM-hosted OTLP defaults or an opt-in managed traces/metrics path on top of that same shared telemetry contract, pair it with `Cephalon.Observability.OracleCloud`; when a host needs OpenShift in-cluster collector wiring and hosted cluster defaults on top of that same shared telemetry contract, pair it with `Cephalon.Observability.OpenShift`; when a host needs VMware Tanzu hosted defaults or proxy-focused trace handoff on top of that same shared telemetry contract, pair it with `Cephalon.Observability.Tanzu`; when a host needs a supported Azure Monitor export path on top of the same shared telemetry contract, pair it with `Cephalon.Observability.AzureMonitor`; and when a host needs a supported Serilog provider path over the same shared `ILogger` contract, pair it with `Cephalon.Observability.Serilog` instead of pulling provider-specific dependencies into the engine or this baseline package.

The same shared telemetry contract is also intentionally open to downstream packages. Teams can bind `ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry`, add provider-specific options under `Engine:Observability:Telemetry:{ProviderName}`, keep exporter/auth/resource logic in their own companion package, and optionally publish their own diagnostics convention plus startup summary through `IDiagnosticsConventionContributor` and `IHostedService`. That is the intended path for later Cloudflare or internal gateway integrations without modifying `Cephalon.Engine`, `Cephalon.Abstractions`, or the shared `ILogger` pipeline. See [Observability provider authoring](../observability-provider-authoring.md) for the recommended package shape, extension-method pattern, startup-summary checklist, and current Cloudflare-specific guidance.

## Related docs

- [Cephalon.Observability.CassandraDependencies](observability-cassandra-dependencies.md)
- [Cephalon.Observability.ClickHouseDependencies](observability-clickhouse-dependencies.md)
- [Cephalon.Observability.ConsulDependencies](observability-consul-dependencies.md)
- [Cephalon.Observability.ElasticsearchDependencies](observability-elasticsearch-dependencies.md)
- [Cephalon.Observability.HttpDependencies](observability-http-dependencies.md)
- [Cephalon.Observability.KafkaDependencies](observability-kafka-dependencies.md)
- [Cephalon.Observability.MemcachedDependencies](observability-memcached-dependencies.md)
- [Cephalon.Observability.MongoDbDependencies](observability-mongodb-dependencies.md)
- [Cephalon.Observability.MqttDependencies](observability-mqtt-dependencies.md)
- [Cephalon.Observability.MySqlDependencies](observability-mysql-dependencies.md)
- [Cephalon.Observability.NatsDependencies](observability-nats-dependencies.md)
- [Cephalon.Observability.Neo4jDependencies](observability-neo4j-dependencies.md)
- [Cephalon.Observability.OpenSearchDependencies](observability-opensearch-dependencies.md)
- [Cephalon.Observability.OracleDependencies](observability-oracle-dependencies.md)
- [Cephalon.Observability.PostgresDependencies](observability-postgres-dependencies.md)
- [Cephalon.Observability.RabbitMqDependencies](observability-rabbitmq-dependencies.md)
- [Cephalon.Observability.RedisDependencies](observability-redis-dependencies.md)
- [Cephalon.Observability.SqlServerDependencies](observability-sqlserver-dependencies.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Cephalon.Observability.AlibabaCloud](observability-alibaba-cloud.md)
- [Cephalon.Observability.Aws](observability-aws.md)
- [Cephalon.Observability.DigitalOcean](observability-digitalocean.md)
- [Cephalon.Observability.GrafanaCloud](observability-grafana-cloud.md)
- [Cephalon.Observability.Gcp](observability-gcp.md)
- [Cephalon.Observability.HuaweiCloud](observability-huawei-cloud.md)
- [Cephalon.Observability.OracleCloud](observability-oracle-cloud.md)
- [Cephalon.Observability.Kubernetes](observability-kubernetes.md)
- [Cephalon.Observability.OpenShift](observability-openshift.md)
- [Cephalon.Observability.Tanzu](observability-tanzu.md)
- [Cephalon.Observability.AzureMonitor](observability-azure-monitor.md)
- [Cephalon.Observability.Serilog](observability-serilog.md)
- [Observability provider authoring](../observability-provider-authoring.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
