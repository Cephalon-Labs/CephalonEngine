# Cephalon Operations

This document captures the current operational surface for Cephalon as of `April 2, 2026`.

For the active phase-2 follow-through inventory, see `docs/operational-hardening-gap-inventory.md`.

## Health surfaces

ASP.NET Core hosts that call `app.MapCephalon()` now expose three health routes:

- `/health`
- `/health/live`
- `/health/ready`

Health responses are JSON and include the check status, duration, and runtime-specific details such as restart count and the most recent failure context.
When modules or installed packages register `IDependencyHealthContributor`, those responses also include dependency details.

Current semantics:

- liveness stays `Healthy` while the process is alive, even during startup and shutdown transitions
- liveness becomes `Unhealthy` when the runtime enters `Failed` or `Stopped`
- liveness becomes `Degraded` when the runtime is live but one or more dependencies report degraded or unhealthy status
- readiness is `Healthy` only when the runtime reaches `Started`
- readiness is `Unhealthy` during startup, shutdown, stopped, and failed states so traffic can stay off the host until the runtime is actually ready
- readiness becomes `Unhealthy` when a required dependency reports `Unhealthy`
- readiness becomes `Degraded` when only optional dependencies are degraded or unhealthy, or when required dependencies are degraded without fully failing

## Dependency surface

`GET /engine/dependencies` exposes the dependency-health snapshot currently contributed to the runtime.

This keeps dependency visibility separate from the aggregate health routes:

- `/engine/dependencies` answers "what dependencies are currently reporting?"
- `/health/live` answers "is the process live?"
- `/health/ready` answers "is the runtime ready to take traffic with its current dependency state?"

## Diagnostics surface

`GET /engine/diagnostics` exposes the engine's operational conventions in one place:

- `meterName`
- `activitySourceName`
- counter names used by the runtime
- the current liveness report
- the current readiness report
- dependency details folded into those runtime reports
- the mapped health routes

This is the quickest way to discover the engine's observability contract without opening code.

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
