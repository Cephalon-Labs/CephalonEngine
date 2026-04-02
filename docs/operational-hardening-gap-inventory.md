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
- `Cephalon.Observability.PostgresDependencies` now provides a reusable provider-specific pack for Postgres databases, including connection-string or host/database configuration plus configurable health queries
- `Cephalon.Observability.RabbitMqDependencies` now provides a reusable provider-specific pack for RabbitMQ brokers, including AMQP connection-string or host/virtual-host configuration plus optional TLS
- `Cephalon.Observability.RedisDependencies` now provides a reusable provider-specific pack for Redis and cache endpoints, including auth and logical database selection

Gap:

- broader provider-specific packs for additional databases and brokers are still missing
- current shipped provider coverage now includes external HTTP/API upstreams plus Postgres database endpoints plus RabbitMQ broker endpoints plus Redis/cache endpoints; richer infrastructure-specific packs beyond that baseline are still left to host or module authors

Why this stays separate:

- the contributor contract is already good enough
- the follow-through now widens reusable provider packaging instead of revisiting the engine abstraction

### `#34` Structured diagnostics and event IDs across packages

Current baseline:

- `Cephalon.Engine` already emits structured runtime/module transition and failure logs with event ids in the `2000` range
- `Cephalon.Observability` already emits startup-summary, diagnostics-catalog, and telemetry-guidance logs with event ids in the `3000` range
- active engine and companion packages now publish their diagnostics conventions through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- currently shipped package coverage includes `Cephalon.Engine`, `Cephalon.Observability`, `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, and `Cephalon.Observability.RedisDependencies`

Gap:

- the currently shipped packages now share one published event-id catalog and package-by-package diagnostics convention
- future packages should follow the same contributor model when they add new operator-facing structured logs, but that no longer blocks the current phase-2 diagnostics baseline

Why this stays separate:

- the engine baseline exists and is now widened across the active observability packages
- remaining phase-2 work shifts to richer runtime answers rather than inventing another diagnostics abstraction for the current shipped set

### `#35` Clearer runtime answers for what loaded, started, failed, and why

Current baseline:

- `/engine/status`, `/engine/packages`, `/engine/dependencies`, `/engine/diagnostics`, and `/engine/snapshot` already expose useful point-in-time answers

Gap:

- there is no richer lifecycle narrative or operator-oriented timeline that answers startup and failure questions in one place
- current surfaces are snapshots, not a stronger “what happened in order and why” diagnostic story

Why this stays separate:

- no new engine abstraction is obviously missing yet
- the follow-through should improve operator answers without duplicating the existing manifest/status/health surfaces

### `#73` Deeper readiness/liveness semantics and richer restart or backoff policy

Current baseline:

- liveness/readiness semantics already exist
- restart is already explicit and conservative

Gap:

- phase 2 still lacks richer draining, warmup, dependency grace-period, or backoff policy behavior
- failure-policy and health semantics remain intentionally conservative rather than environment-tuned

Why this is not blocking `#31`:

- the inventory confirms the baseline is real
- this is follow-through policy work, not missing foundational plumbing

### `#74` Release-validation guidance for health and export conventions

Current baseline:

- release validation already runs build, test, benchmarks, guardrail validation, and reference-doc publishing

Gap:

- the release-validation flow does not yet validate operational-health or telemetry-export conventions directly
- operator guidance for checking health/export behavior in release validation is still implicit rather than documented and scripted

Why this stays later than the inventory task:

- the guidance should be written after the phase-2 implementation shape for exporters and richer health semantics is clearer

## Planning outcome

Current conclusion from this inventory:

- the existing phase-2 child-task split remains valid
- no additional child tasks were required from this audit
- the main missing work is packaging, diagnostics broadening, and operator-facing hardening on top of a shipped baseline

Recommended execution sequence remains:

1. `#31` inventory and sequencing
2. `#33` dependency-health packs on top of the shipped exporter path
3. `#35` clearer runtime answers
4. `#73` deeper readiness/liveness and restart-policy follow-through
5. `#74` release-validation guidance once the operational surface above is clearer
