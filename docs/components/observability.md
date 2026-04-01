# Cephalon.Observability

`Cephalon.Observability` is the diagnostics companion package for Cephalon hosts.

## What it owns

- observability option binding
- startup summaries for manifests, modules, capabilities, and telemetry guidance
- host-friendly registration over the engine's built-in logs, metrics, and activity source
- the shared telemetry configuration contract consumed by optional exporter companion packages

## Main surfaces

- `Configuration/ObservabilityOptions.cs`
- `Configuration/TelemetryExportOptions.cs`
- `Hosting/ObservabilityServiceCollectionExtensions.cs`
- `Hosting/ManifestSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package does not replace the engine's diagnostics primitives. It turns them into conventions and startup behavior that ASP.NET Core and worker hosts can opt into consistently. When a host needs a supported OTLP export path, pair it with `Cephalon.Observability.OpenTelemetry` instead of pulling exporter dependencies into the engine or this baseline package.

## Related docs

- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
