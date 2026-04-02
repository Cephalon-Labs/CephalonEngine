# Cephalon.Observability.AzureMonitor

`Cephalon.Observability.AzureMonitor` adds a supported Azure Monitor / Application Insights export path for Cephalon hosts.

## What it owns

- host-builder registration for Azure Monitor traces, metrics, and logs
- Azure Monitor wiring over the shared `Engine:Observability:Telemetry` contract
- optional `DefaultAzureCredential` authentication on top of the configured connection string
- explicit hosted Azure resource defaults for `cloud.provider`, `cloud.platform`, and `deployment.environment.name`
- startup diagnostics that summarize the active Azure Monitor export mode without logging secrets

## Main surfaces

- `Configuration/AzureMonitorExportOptions.cs`
- `Hosting/AzureMonitorHostApplicationBuilderExtensions.cs`
- `Hosting/AzureMonitorSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns the diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package turns that shared contract into a reusable Azure Monitor / Application Insights export path for ASP.NET Core or worker hosts.

The Azure slice keeps vendor-specific auth and hosted-platform defaults separate from the cloud-neutral OTLP package. Hosts that want a generic collector path can keep using `Cephalon.Observability.OpenTelemetry`, while hosts that want Azure-native exporter wiring can opt into this package and configure `Engine:Observability:Telemetry:AzureMonitor`.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
