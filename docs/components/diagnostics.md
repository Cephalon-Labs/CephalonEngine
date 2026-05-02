# Cephalon.Diagnostics

`Cephalon.Diagnostics` is the engine-level OpenTelemetry semantic-convention adapter for Cephalon. It ships well-known `ActivitySource` and `Meter` names plus a small set of stable Cephalon-prefix attribute keys that complement OpenTelemetry semantic conventions where conventions exist (HTTP, DB, messaging, RPC, runtime).

## What it owns

- the stable engine-level `ActivitySource` name set published through `CephalonActivitySources`: `Cephalon.Engine`, `Cephalon.AspNetCore`, `Cephalon.Worker`
- the stable engine-level `Meter` name set published through `CephalonMeters` with the same three names
- a small set of stable Cephalon-prefix attribute keys (`cephalon.module.id`, `cephalon.behavior.id`, `cephalon.cell.id`, `cephalon.app.blueprint`, `cephalon.tenant.id`) for engine concepts that have no OpenTelemetry semantic-convention name
- public API contract lock-in from day one through `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `PublicAPI.Shipped.txt`, and `PublicAPI.Unshipped.txt`

## Main surfaces

- `CephalonActivitySources.cs`
- `CephalonMeters.cs`
- `CephalonDiagnosticsAttributeKeys.cs`

## How it fits

This pack is intentionally narrow at `M0` taxonomy-only. It does not own emission, exporter configuration, redaction, or sampling. The point of the pack today is to publish the *names* the engine and its host adapters will use when they emit spans, metrics, and logs, so consumer observability companion packs (`Cephalon.Observability.OpenTelemetry`, `Cephalon.Observability.Serilog`, `Cephalon.Observability.AzureMonitor`, `Cephalon.Observability.Aws`, etc.) can subscribe to them through stable identifiers.

Where an OpenTelemetry semantic convention already exists for a concept (HTTP server, DB client, messaging system, runtime, exception attributes), engine-emitted instrumentation will use the OpenTelemetry attribute name directly rather than re-declaring it under `cephalon.*`. The Cephalon-prefix keys in `CephalonDiagnosticsAttributeKeys` are deliberately scoped to engine concepts that have no semantic-convention equivalent (module id, behavior id, cell id, app blueprint, tenant id) so this pack stays a complement to OpenTelemetry rather than a replacement.

## Maturity and ownership

- maturity today: `M2` — `cephalon-managed`; both host adapters route telemetry through this package's name set:
    - `Cephalon.Engine` consumes `CephalonActivitySources.Engine` and `CephalonMeters.Engine` so engine-runtime spans (`engine.build`, `module.{phase}`, `runtime.*`) and metrics flow through the canonical name set; module-lifecycle spans emit `cephalon.module.id` via `CephalonDiagnosticsAttributeKeys.ModuleId`
    - `Cephalon.AspNetCore` declares its diagnostics convention against `CephalonActivitySources.AspNetCore` so structured HTTP request / response / body-capture / trace-correlation diagnostics share the canonical source name with observability companion packs
    - `Cephalon.Worker` declares an internal `WorkerDiagnostics` activity source against `CephalonActivitySources.Worker` and emits `worker.lifecycle.start` / `worker.lifecycle.stop` spans around the hosted-service lifecycle
- promote to `M3` when an explicit operator surface (catalog routes, snapshot keys) lands for the diagnostics name set itself, e.g. `/engine/diagnostics-conventions` projecting the active source / meter / attribute-key set so operators and AI tooling can introspect what the engine actually emits
- ownership: `cephalon-managed` (engine + both host adapters consume this package's constants)

## Cross-references

- [Engineering standards](../engineering-standards.md) — code-quality gates, library / API design, packaging
- [Compatibility](../compatibility.md) — public-API contract artefacts (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md) — `M0`–`M4` plus `taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed` truth
- [Supply-chain uplift plan](../supply-chain-uplift-plan.md) — multi-sprint plan that includes this pack as `ENG-323`
- [SRE posture](../sre-posture.md) — declares the OpenTelemetry semantic-convention discipline this pack embodies
- [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) — authoritative external source
