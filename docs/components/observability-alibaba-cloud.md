# Cephalon.Observability.AlibabaCloud

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.AlibabaCloud` adds Alibaba Cloud-hosted OTLP defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP traces, metrics, and optional collector-backed logs over the shared `Engine:Observability:Telemetry` contract
- explicit hosted Alibaba Cloud resource defaults for ECS, Function Compute, and Alibaba Cloud OpenShift deployments
- an opt-in Alibaba Cloud Managed Service for OpenTelemetry path for direct traces and metrics ingestion over OTLP/gRPC or OTLP/HTTP
- startup diagnostics that summarize the active Alibaba Cloud export mode without logging secrets

## Main surfaces

- `Configuration/AlibabaCloudTelemetryExportOptions.cs`
- `Hosting/AlibabaCloudHostApplicationBuilderExtensions.cs`
- `Hosting/AlibabaCloudSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers Alibaba Cloud-specific hosted defaults and managed OpenTelemetry wiring on top of that same OTLP baseline.

The Alibaba Cloud slice is intentionally explicit about its two modes. If a host already has a shared OTLP collector or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds hosted Alibaba Cloud resource defaults. If the host wants Alibaba Cloud Managed Service for OpenTelemetry direct ingestion instead, it can enable `Engine:Observability:Telemetry:AlibabaCloud:UseManagedOpenTelemetryIngestion`, keep the protocol on `otlp`, `otlp/grpc`, or `otlp/http`, and configure either the OTLP/gRPC endpoint plus `AuthenticationToken` or the signal-specific OTLP/HTTP traces and metrics endpoints. That direct managed path is traces-and-metrics-only for now; logs stay on the collector, SLS, or another runtime-specific route.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
