# Cephalon.Observability.Gcp

`Cephalon.Observability.Gcp` adds GCP-hosted OTLP defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP traces, metrics, and optional collector-backed logs over the shared `Engine:Observability:Telemetry` contract
- explicit hosted GCP resource defaults for Compute Engine, GKE, Cloud Run, App Engine, and Cloud Functions
- an opt-in Google-managed traces/metrics path through `https://telemetry.googleapis.com` using Application Default Credentials
- startup diagnostics that summarize the active GCP export mode without logging secrets

## Main surfaces

- `Configuration/GcpTelemetryExportOptions.cs`
- `Hosting/GcpHostApplicationBuilderExtensions.cs`
- `Hosting/GcpSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers GCP-specific resource defaults and Google-managed traces/metrics wiring on top of that same OTLP baseline.

The GCP slice intentionally keeps two modes explicit instead of hiding them behind one ambiguous switch. If a host already has an OTLP collector or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds hosted GCP resource defaults. If the host wants the Google-managed ingestion path instead, it can enable `Engine:Observability:Telemetry:Gcp:UseGoogleManagedIngestion`, keep the protocol on `otlp/http`, and let the package authenticate traces and metrics through Application Default Credentials while logs stay on the collector or platform logging path.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
