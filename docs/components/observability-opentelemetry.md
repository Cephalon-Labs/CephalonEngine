# Cephalon.Observability.OpenTelemetry

`Cephalon.Observability.OpenTelemetry` adds a supported OpenTelemetry OTLP export path for Cephalon hosts.

## What it owns

- host-builder registration for OpenTelemetry logs, metrics, and traces
- OTLP exporter wiring over the shared `Engine:Observability:Telemetry` contract
- explicit self-hosted collector defaults on top of that shared OTLP contract when `UseSelfHostedDefaults` is enabled
- signal-specific HTTP/protobuf endpoint normalization so hosts can configure a base collector URL once
- ASP.NET Core server-trace instrumentation so exported request traces line up with Cephalon HTTP log correlation

## Main surfaces

- `Hosting/OpenTelemetryHostApplicationBuilderExtensions.cs`

## Source structure

- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns the diagnostics names and lifecycle signals, `Cephalon.Observability` still owns startup summaries and the shared telemetry config contract, and this companion package turns that contract into a reusable OTLP integration path for ASP.NET Core or worker hosts that want real exporter wiring.

For ASP.NET Core hosts, the shipped baseline now also adds server-request tracing so `traceId` and `spanId` values emitted by Cephalon request logging can be followed through exported OTLP traces without adding cloud-specific dependencies to the engine layer.

For self-hosted deployments, the same companion package now supports an explicit no-vendor-default path: set `Engine:Observability:Telemetry:UseSelfHostedDefaults` to `true`, omit `Endpoint`, and the package will target the standard local OTLP collector ports (`http://localhost:4317` for `otlp` / `otlp/grpc`, `http://localhost:4318` for `otlp/http`). The host also adds `deployment.environment.name` from the active environment alongside the existing service-name and service-version resource defaults.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.AzureMonitor](observability-azure-monitor.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
