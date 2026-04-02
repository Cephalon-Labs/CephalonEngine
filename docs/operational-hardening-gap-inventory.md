# Operational Hardening Gap Inventory

This document records the current phase-2 operational hardening gap inventory for Cephalon as of `April 2, 2026`.

It is meant to answer two questions clearly:

1. What operational baseline is already shipped?
2. What still remains before phase 2 can be considered complete?

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

## Gap status mapped to backlog tasks

### `#32` Dedicated exporter or OpenTelemetry companion packaging

Current baseline:

- hosts can declare telemetry export intent through `Engine:Observability:Telemetry`
- `Cephalon.Observability` logs that guidance on startup
- `Cephalon.Observability.OpenTelemetry` now wires OTLP logs, metrics, and traces through `AddCephalonOpenTelemetry()`
- the shipped companion package supports `otlp`, `otlp/grpc`, and `otlp/http`, with automatic signal-path normalization for OTLP HTTP collectors

Outcome:

- exporter packaging should remain outside `Cephalon.Engine`
- the repository now ships a reusable companion package instead of leaving exporter wiring to sample-only host code
- remaining phase-2 work shifts to dependency-health packaging, broader diagnostics conventions, richer operator answers, and deeper health semantics

### `#33` Provider-specific dependency health packs beyond the baseline contributor model

Current baseline:

- `IDependencyHealthContributor` exists
- `RuntimeHealthEvaluator` aggregates dependency reports cleanly
- `Cephalon.Observability.HttpDependencies` now provides a reusable provider-specific pack for external HTTP and API upstreams
- `Cephalon.Observability.KafkaDependencies` now provides a reusable provider-specific pack for Kafka brokers, including bootstrap-server metadata checks plus optional topic verification and SASL/security protocol selection
- `Cephalon.Observability.MongoDbDependencies` now provides a reusable provider-specific pack for MongoDB databases, including connection-string or host/database configuration plus TLS and direct-connection controls
- `Cephalon.Observability.MySqlDependencies` now provides a reusable provider-specific pack for MySQL and MariaDB databases, including connection-string or host/database configuration plus SSL-mode and public-key retrieval settings
- `Cephalon.Observability.NatsDependencies` now provides a reusable provider-specific pack for NATS brokers, including protocol-level `INFO`, `CONNECT`, and `PING`/`PONG` verification plus explicit token or username/password auth settings
- `Cephalon.Observability.PostgresDependencies` now provides a reusable provider-specific pack for Postgres databases, including connection-string or host/database configuration plus configurable health queries
- `Cephalon.Observability.RabbitMqDependencies` now provides a reusable provider-specific pack for RabbitMQ brokers, including AMQP connection-string or host/virtual-host configuration plus optional TLS
- `Cephalon.Observability.RedisDependencies` now provides a reusable provider-specific pack for Redis and cache endpoints, including auth and logical database selection
- `Cephalon.Observability.SqlServerDependencies` now provides a reusable provider-specific pack for SQL Server and Azure SQL databases, including connection-string or host/database configuration plus configurable health queries and encryption mode selection

Gap:

- broader provider-specific packs for additional databases and brokers are still missing
- current shipped provider coverage now includes external HTTP/API upstreams plus Kafka broker metadata endpoints plus MongoDB document-database endpoints plus MySQL/MariaDB database endpoints plus NATS broker endpoints plus Postgres database endpoints plus RabbitMQ broker endpoints plus Redis/cache endpoints plus SQL Server and Azure SQL endpoints; richer infrastructure-specific packs beyond that baseline are still left to host or module authors

Why this stays separate:

- the contributor contract is already good enough
- the follow-through now widens reusable provider packaging instead of revisiting the engine abstraction

### `#34` Structured diagnostics and event IDs across packages

Current baseline:

- `Cephalon.Engine` already emits structured runtime/module transition and failure logs with event ids in the `2000` range
- `Cephalon.Observability` already emits startup-summary, diagnostics-catalog, and telemetry-guidance logs with event ids in the `3000` range
- active engine and companion packages now publish their diagnostics conventions through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- currently shipped package coverage includes `Cephalon.Engine`, `Cephalon.Observability`, `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.KafkaDependencies`, `Cephalon.Observability.MongoDbDependencies`, `Cephalon.Observability.MySqlDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, `Cephalon.Observability.RedisDependencies`, and `Cephalon.Observability.SqlServerDependencies`

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

- `scripts/validate-operational-conventions.ps1` now gives operators and maintainers a focused validation pass for ASP.NET Core health routes, worker-host health parity, observability startup guidance, and OTLP exporter wiring
- `scripts/validate-release.ps1` now runs that focused operational suite explicitly alongside the broader build, test, benchmark, guardrail, and reference-doc flow

Why this stays separate:

- the follow-through turns a previously implicit convention check into an explicit, repeatable release signal without moving exporter dependencies or health semantics back into the engine core

## Planning outcome

Current conclusion from this inventory:

- the existing phase-2 child-task split remains valid
- no additional child tasks were required from this audit
- the main missing work is packaging breadth and operator-facing hardening on top of a shipped baseline

Recommended execution sequence remains:

1. `#31` inventory and sequencing
2. `#33` dependency-health packs on top of the shipped exporter path
3. `#35` clearer runtime answers
