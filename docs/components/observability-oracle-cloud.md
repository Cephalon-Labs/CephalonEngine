# Cephalon.Observability.OracleCloud

`Cephalon.Observability.OracleCloud` adds Oracle Cloud APM-hosted OTLP defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint is already in place
- explicit hosted Oracle Cloud resource defaults for Compute, OKE, and Functions deployments
- an opt-in Oracle Cloud APM managed-ingestion path for traces and metrics over OTLP/HTTP by using the documented data-upload endpoint and data-key headers
- startup diagnostics that summarize the active Oracle Cloud export mode without logging secrets

## Main surfaces

- `Configuration/OracleCloudTelemetryExportOptions.cs`
- `Hosting/OracleCloudHostApplicationBuilderExtensions.cs`
- `Hosting/OracleCloudSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers Oracle Cloud-specific hosted defaults and Oracle Cloud APM managed-ingestion wiring on top of the same OTLP baseline.

The Oracle Cloud slice is intentionally explicit about its two modes. If a host already has a shared OTLP collector or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds hosted Oracle Cloud resource defaults. If the host wants Oracle Cloud APM direct managed ingestion instead, it can enable `Engine:Observability:Telemetry:OracleCloud:UseManagedOpenTelemetryIngestion`, keep the protocol on `otlp/http`, and configure the APM `DataUploadEndpoint` plus signal-specific data keys. That direct managed path covers traces and metrics only; logs stay on the collector path, Oracle Log Analytics, or another runtime-specific route.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
