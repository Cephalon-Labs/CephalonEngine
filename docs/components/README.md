# Cephalon Component Docs

This catalog maps the current solution into hand-authored component-level documentation.

These pages explain what each shipped package owns and how it fits into Cephalon. Generated API reference output stays separate under `docs/reference/`.

See also: [Docs hub](../README.md)

## Maturity at a glance

Each shipped package carries a maturity label (`M0` through `M4`) and an ownership mode (`taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed`). Read the labels before assuming a package is runtime-ready.

- per-package truth: [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)
- consolidated cross-reference: [`conformance-matrix.md`](../conformance-matrix.md)
- runtime contract surfaces (per-package routes, snapshot keys, catalog interfaces): [`runtime-contract-index.md`](../runtime-contract-index.md)

When a component-page maturity label and the maturity audit disagree, the audit is the authoritative truth. When in doubt, follow the cross-reference into the conformance matrix.


## Core runtime

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.AspNetCore](aspnetcore.md)
- [Cephalon.AspNetCore.GraphQL](aspnetcore-graphql.md)
- [Cephalon.AspNetCore.JsonRpc](aspnetcore-jsonrpc.md)
- [Cephalon.AspNetCore.Grpc](aspnetcore-grpc.md)
- [Cephalon.Worker](worker.md)

## Technology and follow-through packs

- [Cephalon.Agentics](agentics.md)
- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.EventSourcing.EntityFramework](event-sourcing-entityframework.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.EventSourcing.Redis](event-sourcing-redis.md)
- [Cephalon.Eventing](eventing.md)
- [Cephalon.Data.SqlServer](data-sqlserver.md)
- [Cephalon.Data.Postgres](data-postgres.md)
- [Cephalon.Data.MySql](data-mysql.md)
- [Cephalon.Data.Oracle](data-oracle.md)
- [Cephalon.Data.Debezium](data-debezium.md)
- [Cephalon.Retrieval](retrieval.md)
- [Cephalon.Edge](edge.md)
- [Cephalon.Edge.KubernetesGateway](edge-kubernetes-gateway.md)
- [Cephalon.Edge.Traefik](edge-traefik.md)

## Phase 9 companion packs

- [Cephalon.Behaviors](behaviors.md)
- [Cephalon.Behaviors.Http](behaviors-http.md)
- [Cephalon.Behaviors.Messaging](behaviors-messaging.md)
- [Cephalon.Behaviors.Patterns](behaviors-patterns.md)
- [Cephalon.Behaviors.SourceGen](behaviors-sourcegen.md)

## Phase 10 companion packs

- [Cephalon.Data.Redis](data-redis.md)
- [Cephalon.EventSourcing.Redis](event-sourcing-redis.md)
- [Cephalon.Data.Neo4j](data-neo4j.md)
- [Cephalon.EventSourcing.Neo4j](event-sourcing-neo4j.md)
- [Cephalon.Data.Cassandra](data-cassandra.md)
- [Cephalon.EventSourcing.Cassandra](event-sourcing-cassandra.md)
- [Cephalon.Data.ClickHouse](data-clickhouse.md)
- [Cephalon.EventSourcing.ClickHouse](event-sourcing-clickhouse.md)
- [Cephalon.Data.Elasticsearch](data-elasticsearch.md)
- [Cephalon.EventSourcing.Elasticsearch](event-sourcing-elasticsearch.md)
- [Cephalon.Data.OpenSearch](data-opensearch.md)
- [Cephalon.EventSourcing.OpenSearch](event-sourcing-opensearch.md)

## Sprint 31 companion packs

- [Cephalon.Data.Qdrant](data-qdrant.md)
- [Cephalon.EventSourcing.Qdrant](event-sourcing-qdrant.md)
- [Cephalon.Data.Nats](data-nats.md)
- [Cephalon.EventSourcing.Nats](event-sourcing-nats.md)

## Phase 12 companion packs

- [Cephalon.Eventing.Behaviors](eventing-behaviors.md)

## Phase 8 companion packs

- [Cephalon.Audit](audit.md)
- [Cephalon.Audit.EntityFramework](audit-entityframework.md)
- [Cephalon.Data](data.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Eventing.Wolverine](eventing-wolverine.md)
- [Cephalon.Identity](identity.md)
- [Cephalon.Identity.AspNetCore](identity-aspnetcore.md)
- [Cephalon.Ids.Sfid](ids-sfid.md)
- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore](multi-tenancy-governance-amazonsesdelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery](multi-tenancy-governance-amazonsesdelivery.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery](multi-tenancy-governance-microsoftgraphdelivery.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity](multi-tenancy-governance-microsoftgraphdelivery-azureidentity.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)

## Tooling and adoption

- [Cephalon.Observability](observability.md)
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
- [Cephalon.Observability.NewRelic](observability-new-relic.md)
- [Cephalon.Observability.OracleCloud](observability-oracle-cloud.md)
- [Cephalon.Observability.Kubernetes](observability-kubernetes.md)
- [Cephalon.Observability.OpenShift](observability-openshift.md)
- [Cephalon.Observability.Tanzu](observability-tanzu.md)
- [Cephalon.Observability.AzureMonitor](observability-azure-monitor.md)
- [Cephalon.Observability.Serilog](observability-serilog.md)
- [Cephalon.Diagnostics](diagnostics.md)
- [Cephalon.Scaffolding](scaffolding.md)
- [Cephalon.Cli](cli.md)
- [Cephalon.ReferenceDocs](reference-docs.md)

## Additional repo surfaces

- benchmark suite: [Benchmarking](../benchmarking.md)
- template pack: [README](../../README.md) and `templates/Cephalon.TemplatePack/PACKAGE.md`
- module authoring: [Module authoring](../module-authoring.md)
- downstream observability guidance: [Observability provider authoring](../observability-provider-authoring.md)

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
- [Runtime failure policy](../runtime-failure-policy.md)
- [Benchmarking](../benchmarking.md)
- [Reference docs publishing](../reference-docs.md)
