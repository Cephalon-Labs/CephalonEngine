# Cephalon.Observability

`Cephalon.Observability` is the diagnostics companion package for Cephalon hosts.

## What it owns

- observability option binding
- startup summaries for manifests, modules, capabilities, and telemetry guidance
- host-friendly registration over the engine's built-in logs, metrics, and activity source

## Main surfaces

- `Configuration/ObservabilityOptions.cs`
- `Configuration/TelemetryExportOptions.cs`
- `Hosting/ObservabilityServiceCollectionExtensions.cs`
- `Hosting/ManifestSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package does not replace the engine's diagnostics primitives. It turns them into conventions and startup behavior that ASP.NET Core and worker hosts can opt into consistently.

## Related docs

- [Operations](../operations.md)
- [Architecture](../architecture.md)
