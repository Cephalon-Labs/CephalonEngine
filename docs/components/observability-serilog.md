# Cephalon.Observability.Serilog

`Cephalon.Observability.Serilog` adds a supported Serilog provider-integration path for Cephalon hosts.

## What it owns

- host-builder registration for Serilog over the shared `Microsoft.Extensions.Logging.ILogger` pipeline
- configuration-driven Serilog activation from the standard top-level `Serilog` section
- code-based sink and enricher extension points for hosts that need more than configuration alone

## Main surfaces

- `Hosting/SerilogHostApplicationBuilderExtensions.cs`

## Source structure

- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine and baseline observability package still log through `ILogger`, while this companion package gives ASP.NET Core and worker hosts one explicit way to swap in Serilog sinks, enrichers, and formatting without inventing a Cephalon-specific logging abstraction.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
