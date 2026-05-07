# Cephalon.Observability.GrafanaCloud

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.GrafanaCloud` adds Grafana Cloud OTLP endpoint defaults and access-policy authentication guidance for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint is already in place
- an opt-in Grafana Cloud direct-ingestion path for OTLP/gRPC or OTLP/HTTP by using the Grafana Cloud OTLP endpoint plus either raw OTLP headers or structured access-policy settings
- explicit startup diagnostics that summarize the active endpoint mode, authentication mode, resource context, and signal selection without logging secrets
- a sanitized `telemetry-export-grafana-cloud` runtime surface for `/engine/technology-surfaces` and `/engine/snapshot`

## Main surfaces

- `Configuration/GrafanaCloudTelemetryExportOptions.cs`
- `Hosting/GrafanaCloudHostApplicationBuilderExtensions.cs`
- `Hosting/GrafanaCloudSummaryHostedService.cs`
- `Hosting/GrafanaCloudTelemetryRuntimeContributor.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers Grafana Cloud-specific OTLP endpoint and authentication guidance on top of that same baseline.

When the package is activated, it contributes the `telemetry-export-grafana-cloud` runtime surface so operators can see the Grafana Cloud observability pack is active without exposing OTLP endpoints, headers, or access-policy tokens.

The Grafana Cloud slice is intentionally explicit about its two modes. If a host already has a shared OTLP collector or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds Grafana-friendly resource context such as `service.namespace` and `deployment.environment.name`. If the host wants direct Grafana Cloud ingestion instead, it can enable `Engine:Observability:Telemetry:GrafanaCloud:UseDirectGrafanaCloudEndpoint`, keep the provider on `OpenTelemetry`, and configure either the generated OTLP `Headers` value or a structured `InstanceId` plus `AccessPolicyToken` pair for Basic-auth header construction.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
