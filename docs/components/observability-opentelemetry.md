# Cephalon.Observability.OpenTelemetry

`Cephalon.Observability.OpenTelemetry` adds a supported OpenTelemetry OTLP export path for Cephalon hosts.

## What it owns

- host-builder registration for OpenTelemetry logs, metrics, and traces
- OTLP exporter wiring over the shared `Engine:Observability:Telemetry` contract
- signal-specific HTTP/protobuf endpoint normalization so hosts can configure a base collector URL once

## Main surfaces

- `Hosting/OpenTelemetryHostApplicationBuilderExtensions.cs`

## Source structure

- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns the diagnostics names and lifecycle signals, `Cephalon.Observability` still owns startup summaries and the shared telemetry config contract, and this companion package turns that contract into a reusable OTLP integration path for ASP.NET Core or worker hosts that want real exporter wiring.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
