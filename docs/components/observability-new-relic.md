# Cephalon.Observability.NewRelic

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.NewRelic` adds New Relic native OTLP endpoint defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint is already in place
- an opt-in New Relic native OTLP path that can derive the documented regional endpoints or use an explicit OTLP endpoint override
- `api-key` header guidance from either raw OTLP headers or a structured New Relic license-key setting
- startup diagnostics that summarize the active New Relic export mode without logging secrets
- a sanitized `telemetry-export-new-relic` runtime surface for `/engine/technology-surfaces` and `/engine/snapshot`

## Main surfaces

- `Configuration/NewRelicTelemetryExportOptions.cs`
- `Hosting/NewRelicHostApplicationBuilderExtensions.cs`
- `Hosting/NewRelicSummaryHostedService.cs`
- `Hosting/NewRelicTelemetryRuntimeContributor.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers New Relic-specific OTLP endpoint and `api-key` authentication guidance on top of the same OTLP baseline.

When the package is activated, it contributes the `telemetry-export-new-relic` runtime surface so operators can see the New Relic observability pack is active without exposing OTLP endpoints, headers, or license keys.

The New Relic slice is intentionally explicit about its two modes. If a host already has a shared OTLP collector or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds explicit resource context such as `service.namespace` and `deployment.environment.name`. If the host wants New Relic native OTLP ingestion instead, it can enable `Engine:Observability:Telemetry:NewRelic:UseNativeOtlpEndpoint`, keep the provider on `OpenTelemetry`, and configure either the raw OTLP `Headers` value or a structured `LicenseKey` that the package converts into the required `api-key` header. If `Endpoint` is omitted in that direct mode, the package derives the documented OTLP base endpoint from the configured `Region`.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
