# Operational Hardening Gap Inventory

This document records the current phase-2 operational hardening gap inventory for Cephalon as of `April 2, 2026`.

It is meant to answer two questions clearly:

1. What operational baseline is already shipped?
2. What still remained before phase 2 could be considered complete, and what moved into later phases?

## Shipped baseline

The repository already ships a meaningful operational baseline:

- health routes and shared runtime health semantics through `RuntimeHealthEvaluator`, `/health`, `/health/live`, `/health/ready`, and `/engine/dependencies`
- explicit runtime failure-policy configuration and restart guards through `Engine:FailurePolicy`, `/engine/failure-policy`, and `/engine/status`
- runtime diagnostics names and counters through `Cephalon.Engine` meter/activity source plus `/engine/diagnostics`
- operator-facing runtime snapshot aggregation through `/engine/snapshot`
- startup summaries and telemetry guidance logging through `Cephalon.Observability`
- reusable OTLP exporter wiring through `Cephalon.Observability.OpenTelemetry`
- benchmark guardrail validation through `Cephalon.Benchmarks`, `performance-guardrails.json`, and `scripts/validate-release.ps1`
- release-validation automation through `.github/workflows/release-validation.yml`

That means phase 2 is follow-through work, not greenfield operational work.

## Evidence in code

- health and dependency evaluation baseline:
  - `src/Cephalon.Engine/Runtime/RuntimeHealthEvaluator.cs`
  - `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs`
- runtime failure and restart baseline:
  - `src/Cephalon.Engine/Configuration/FailurePolicy.cs`
  - `src/Cephalon.Engine/Runtime/EngineRuntime.cs`
  - `docs/runtime-failure-policy.md`
- diagnostics and snapshot baseline:
  - `src/Cephalon.Engine/Diagnostics/EngineDiagnostics.cs`
  - `src/Cephalon.Engine/Runtime/RuntimeIntrospectionSnapshotProvider.cs`
  - `src/Cephalon.AspNetCore/Diagnostics/DiagnosticsSurface.cs`
- observability startup-summary baseline:
  - `src/Cephalon.Observability/Hosting/ManifestSummaryHostedService.cs`
  - `src/Cephalon.Observability/Configuration/TelemetryExportOptions.cs`
- observability exporter companion baseline:
  - `src/Cephalon.Observability.OpenTelemetry/Hosting/OpenTelemetryHostApplicationBuilderExtensions.cs`
- release-validation and benchmark baseline:
  - `benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json`
- `scripts/validate-release.ps1`
- `.github/workflows/release-validation.yml`
- prepared composition and runtime hot paths are benchmarked separately from builder/provider setup so the guardrail catalog tracks `Build()` and lifecycle costs directly
- the guardrail catalog now also covers strict trust-policy composition plus correlated, bounded-truncation, and concurrent ASP.NET Core request-logging paths with request/response body capture enabled

## Gap status mapped to backlog tasks

### `#32` Dedicated exporter or OpenTelemetry companion packaging

Current baseline:

- hosts can declare telemetry export intent through `Engine:Observability:Telemetry`
- `Cephalon.Observability` logs that guidance on startup
- `Cephalon.Observability.OpenTelemetry` now wires OTLP logs, metrics, and traces through `AddCephalonOpenTelemetry()`
- `Cephalon.Observability.Serilog` now wires Serilog through `AddCephalonSerilog()` while keeping the shared `ILogger` contract intact
- the shipped companion package supports `otlp`, `otlp/grpc`, and `otlp/http`, with automatic signal-path normalization for OTLP HTTP collectors
- the shipped OpenTelemetry baseline now also adds ASP.NET Core server tracing so request traces line up with Cephalon HTTP log correlation when hosts export traces

Outcome:

- exporter packaging should remain outside `Cephalon.Engine`
- the repository now ships a reusable companion package instead of leaving exporter wiring to sample-only host code
- remaining phase-2 work now sits primarily in phase-6 cloud tracing/export follow-through, with the self-hosted slice shipped and Azure Monitor now the first explicit vendor-specific target; any extra provider packs or operator refinements stay adoption-driven expansion work

### `#87` `ILogger` provider wiring and Serilog host integration

Shipped follow-through:

- `Cephalon.Engine` and `Cephalon.Observability` continue to emit through `Microsoft.Extensions.Logging.ILogger`
- `Cephalon.Observability.Serilog` now provides `AddCephalonSerilog()` for host-neutral Serilog registration on `IHostApplicationBuilder`
- the companion package reads the standard top-level `Serilog` section, supports code-based sink and enricher extension, and keeps registration additive to the shared `ILogger` pipeline
- `Cephalon.AspNetCore` now ships `Engine:Observability:HttpLogging` plus `AddCephalonHttpLogging()` for opt-in request/response summaries, bounded body capture, request-scope correlation, and default sensitive-value redaction across query-string, JSON, form, and header-style plain-text payloads over the same shared `ILogger` pipeline
- when ASP.NET Core request logging is enabled, Serilog receives `RequestId`, `TraceId`, `SpanId`, and `TraceParent` through the same MEL scope flow, so request diagnostics and trace exports stay linkable without a new Cephalon logger API

Why this stays separate:

- logging-provider selection is orthogonal to cloud tracing/export concerns
- Serilog now integrates as an `ILogger` provider, not as a new Cephalon logging surface

### `#86` Azure Monitor exporter and hosted Azure defaults on top of the OTLP baseline

Current baseline:

- hosts can declare telemetry export intent through `Engine:Observability:Telemetry`
- `Cephalon.Observability.OpenTelemetry` already wires OTLP logs, metrics, and traces through `AddCephalonOpenTelemetry()`
- the shipped OTLP baseline now includes ASP.NET Core server instrumentation so exported traces can correlate with Cephalon request logging
- the current shipped export story is cloud-neutral and works without locking Cephalon into one vendor/runtime target

Re-scope decision:

- the original later follow-through expanded into a broader cloud/platform companion track spanning self-hosted collectors and runtimes plus AWS, Azure, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, and VMware Tanzu
- the self-hosted slice is now shipped, so `#86` is being narrowed to Azure Monitor exporter wiring plus hosted Azure defaults as the first explicit vendor-specific child under `ENG-029`
- AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, and VMware Tanzu remain later follow-up child items under `ENG-029` instead of staying folded into one ambiguous task

Why this target stays in phase 6:

- cloud tracing/export work is deployment-context-specific
- Azure-specific exporter wiring, auth, resource attributes, and hosted defaults still belong in companion packages rather than the reusable host/runtime primitives shipped in phase 2
- Azure is the cleanest first vendor-specific slice after the shipped self-hosted path because Cephalon already centers .NET host composition and operational guidance without needing to reopen phase 2

### `#33` Baseline provider-specific dependency health companion packages

Shipped scope:

- `IDependencyHealthContributor` exists
- `RuntimeHealthEvaluator` aggregates dependency reports cleanly
- `Cephalon.Observability.ConsulDependencies` now provides a reusable provider-specific pack for Consul control planes, including leader checks plus optional ACL-token and datacenter selection
- `Cephalon.Observability.ElasticsearchDependencies` now provides a reusable provider-specific pack for Elasticsearch clusters, including cluster-health requests plus API-key, bearer-token, or basic-auth handling
- `Cephalon.Observability.HttpDependencies` now provides a reusable provider-specific pack for external HTTP and API upstreams
- `Cephalon.Observability.KafkaDependencies` now provides a reusable provider-specific pack for Kafka brokers, including bootstrap-server metadata checks plus optional topic verification and SASL/security protocol selection
- `Cephalon.Observability.MemcachedDependencies` now provides a reusable provider-specific pack for Memcached cache endpoints, including text-protocol `version` verification over TCP
- `Cephalon.Observability.MongoDbDependencies` now provides a reusable provider-specific pack for MongoDB databases, including connection-string or host/database configuration plus TLS and direct-connection controls
- `Cephalon.Observability.MqttDependencies` now provides a reusable provider-specific pack for MQTT brokers, including protocol-level `CONNECT`, `CONNACK`, and `PINGREQ`/`PINGRESP` verification plus explicit username/password and TLS settings
- `Cephalon.Observability.MySqlDependencies` now provides a reusable provider-specific pack for MySQL and MariaDB databases, including connection-string or host/database configuration plus SSL-mode and public-key retrieval settings
- `Cephalon.Observability.NatsDependencies` now provides a reusable provider-specific pack for NATS brokers, including protocol-level `INFO`, `CONNECT`, and `PING`/`PONG` verification plus explicit token or username/password auth settings
- `Cephalon.Observability.ClickHouseDependencies` now provides a reusable provider-specific pack for ClickHouse analytics databases, including connection-string or host/protocol/database configuration plus configurable health queries
- `Cephalon.Observability.Neo4jDependencies` now provides a reusable provider-specific pack for Neo4j graph endpoints, including endpoint URIs or host/port/scheme configuration plus optional database selection and configurable Cypher health queries
- `Cephalon.Observability.OpenSearchDependencies` now provides a reusable provider-specific pack for OpenSearch clusters, including base-URL or full cluster-health endpoint configuration plus optional index selection and bearer/basic auth support
- `Cephalon.Observability.CassandraDependencies` now provides a reusable provider-specific pack for Cassandra clusters, including contact-point lists, optional keyspace selection, credentials, and configurable CQL health queries
- `Cephalon.Observability.OracleDependencies` now provides a reusable provider-specific pack for Oracle databases, including connection-string or host/service-name configuration plus configurable health queries
- `Cephalon.Observability.PostgresDependencies` now provides a reusable provider-specific pack for Postgres databases, including connection-string or host/database configuration plus configurable health queries
- `Cephalon.Observability.RabbitMqDependencies` now provides a reusable provider-specific pack for RabbitMQ brokers, including AMQP connection-string or host/virtual-host configuration plus optional TLS
- `Cephalon.Observability.RedisDependencies` now provides a reusable provider-specific pack for Redis and cache endpoints, including auth and logical database selection
- `Cephalon.Observability.SqlServerDependencies` now provides a reusable provider-specific pack for SQL Server and Azure SQL databases, including connection-string or host/database configuration plus configurable health queries and encryption mode selection

Outcome:

- the current phase-2 baseline provider set is now shipped across Cassandra cluster endpoints plus ClickHouse analytics endpoints plus Consul control-plane endpoints plus Elasticsearch cluster endpoints plus external HTTP/API upstreams plus Kafka broker metadata endpoints plus Memcached cache endpoints plus MongoDB document-database endpoints plus MQTT broker endpoints plus MySQL/MariaDB database endpoints plus NATS broker endpoints plus Neo4j graph endpoints plus OpenSearch cluster endpoints plus Oracle database endpoints plus Postgres database endpoints plus RabbitMQ broker endpoints plus Redis/cache endpoints plus SQL Server and Azure SQL endpoints
- additional provider-specific packs can land later as explicit adoption-driven expansion work instead of remaining part of this baseline task
- `Cephalon.Observability.HttpDependencies` should continue growing along generic HTTP semantics instead of absorbing product-aware response mapping; HTTP-based systems such as Elasticsearch stay in dedicated packs when they need first-class endpoint or payload contracts
- `Cephalon.Observability.NatsDependencies` can continue growing across NATS-native wire semantics without forcing unrelated workload semantics into the shared broker baseline

Why this stays separate:

- the contributor contract is already good enough
- future provider-pack additions should stay adoption-driven instead of reopening the current baseline task

### `#34` Structured diagnostics and event IDs across packages

Current baseline:

- `Cephalon.Engine` already emits structured runtime/module transition and failure logs with event ids in the `2000` range
- `Cephalon.Observability` already emits startup-summary, diagnostics-catalog, and telemetry-guidance logs with event ids in the `3000` range
- active engine and companion packages now publish their diagnostics conventions through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- currently shipped package coverage includes `Cephalon.Engine`, `Cephalon.Observability`, `Cephalon.Observability.CassandraDependencies`, `Cephalon.Observability.ClickHouseDependencies`, `Cephalon.Observability.ConsulDependencies`, `Cephalon.Observability.ElasticsearchDependencies`, `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.KafkaDependencies`, `Cephalon.Observability.MemcachedDependencies`, `Cephalon.Observability.MongoDbDependencies`, `Cephalon.Observability.MqttDependencies`, `Cephalon.Observability.MySqlDependencies`, `Cephalon.Observability.NatsDependencies`, `Cephalon.Observability.Neo4jDependencies`, `Cephalon.Observability.OpenSearchDependencies`, `Cephalon.Observability.OracleDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, `Cephalon.Observability.RedisDependencies`, and `Cephalon.Observability.SqlServerDependencies`

Gap:

- the currently shipped packages now share one published event-id catalog and package-by-package diagnostics convention
- future packages should follow the same contributor model when they add new operator-facing structured logs, but that no longer blocks the current phase-2 diagnostics baseline

Why this stays separate:

- the engine baseline exists and is now widened across the active observability packages
- remaining phase-2 work shifts to richer runtime answers rather than inventing another diagnostics abstraction for the current shipped set

### `#35` Clearer runtime answers for what loaded, started, failed, and why

Current baseline:

- `/engine/status`, `/engine/packages`, `/engine/dependencies`, `/engine/diagnostics`, and `/engine/snapshot` already expose useful point-in-time answers
- `/engine/runtime-story` now combines loaded packages, per-module lifecycle state, and an ordered runtime timeline into one operator-facing payload
- `IRuntime.OperationalStory` keeps the same lifecycle narrative available outside ASP.NET Core hosts
- `/engine/snapshot` now folds that runtime story into the broader manifest/status/technology/diagnostics payload

Outcome:

- operators now have one host-level route and one host-agnostic runtime contract for answering what loaded, what started, what failed, and why
- the runtime story keeps package load visibility, module lifecycle state, and ordered timeline events aligned without inventing a second manifest or health abstraction

Why this stays separate:

- no new engine abstraction is obviously missing yet
- the follow-through improved operator answers without duplicating the existing manifest/status/health surfaces

### `#73` Deeper readiness/liveness semantics and richer restart or backoff policy

Shipped follow-through:

- `Engine:FailurePolicy` now exposes `StartupReadinessDelay`, `ShutdownLivenessGracePeriod`, and `ManualRestartBackoff`
- `/health/live`, `/health/ready`, and `/engine/diagnostics` now surface active lifecycle windows such as startup warmup, shutdown drain, and restart backoff
- `/engine/status` and `/engine/runtime-story` now expose shutdown timing and restart-availability timestamps for restartable failures

What still stays later:

- dependency-specific grace periods or maintenance-mode policy are still optional follow-through, not part of the shipped baseline

### `#74` Release-validation guidance for health and export conventions

Shipped follow-through:

- `scripts/validate-operational-conventions.ps1` now gives operators and maintainers a focused validation pass for ASP.NET Core health routes, worker-host health parity, observability startup guidance, Serilog provider wiring, and OTLP exporter wiring
- `scripts/validate-release.ps1` now runs that focused operational suite explicitly alongside the broader build, test, benchmark, guardrail, and reference-doc flow

Why this stays separate:

- the follow-through turns a previously implicit convention check into an explicit, repeatable release signal without moving exporter dependencies or health semantics back into the engine core

## Planning outcome

Current conclusion from this inventory:

- the phase-2 child-task split served its purpose for the shipped baseline; with the self-hosted slice complete, `#86` should stop acting as a multi-cloud umbrella and instead carry the first explicit Azure Monitor follow-through under `ENG-029`
- phase 2 can now be treated as substantially complete on top of a shipped provider/logging/runtime-surface baseline, with any extra provider packs still treated as future adoption-driven expansion

Recommended execution sequence now is:

1. treat phase 2 as complete for the shipped operational baseline
2. track `#86` under `ENG-029` in phase 6 cloud and platform integrations as the Azure Monitor vendor slice, with AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, and VMware Tanzu split into later explicit child items
3. keep adoption-driven provider-pack additions separate unless a concrete infrastructure gap appears
