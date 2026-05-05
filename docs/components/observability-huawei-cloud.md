# Cephalon.Observability.HuaweiCloud

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.HuaweiCloud` adds Huawei Cloud-hosted OTLP defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint is already in place
- explicit hosted Huawei Cloud resource defaults for ECS, CCE, and FunctionGraph deployments
- an opt-in Huawei Cloud APM managed trace-ingestion path over OTLP/gRPC plus the required `Authentication` header
- startup diagnostics that summarize the active Huawei Cloud export mode without logging secrets

## Main surfaces

- `Configuration/HuaweiCloudTelemetryExportOptions.cs`
- `Hosting/HuaweiCloudHostApplicationBuilderExtensions.cs`
- `Hosting/HuaweiCloudSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers Huawei Cloud-specific hosted defaults and managed APM trace-ingestion wiring on top of the same OTLP baseline.

The Huawei Cloud slice is intentionally explicit about its two modes. If a host already has a shared OTLP collector or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds hosted Huawei Cloud resource defaults. If the host wants Huawei Cloud APM direct trace ingestion instead, it can enable `Engine:Observability:Telemetry:HuaweiCloud:UseApmManagedTraceIngestion`, keep the protocol on `otlp` or `otlp/grpc`, and configure both `ApmEndpoint` and `AuthenticationToken`. That direct managed path is trace-only for now; logs and metrics stay on the collector or another runtime-specific route.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
