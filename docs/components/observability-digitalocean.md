# Cephalon.Observability.DigitalOcean

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.DigitalOcean` adds DigitalOcean collector defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint or self-hosted OTLP path is already in place
- explicit DigitalOcean hosted defaults for Droplets, DOKS, and App Platform on top of that same collector-first baseline
- an opt-in in-cluster collector-service path for DOKS deployments when no shared endpoint is configured
- optional OTLP headers and optional HTTPS trusted-CA bundle handling for OTLP/HTTP traces and metrics
- best-effort Droplet metadata detection for `host.id`, `host.name`, and `cloud.region` when teams enable it explicitly
- startup diagnostics that summarize the active DigitalOcean export mode without logging secrets

## Main surfaces

- `Configuration/DigitalOceanTelemetryExportOptions.cs`
- `Hosting/DigitalOceanHostApplicationBuilderExtensions.cs`
- `Hosting/DigitalOceanSummaryHostedService.cs`
- `Hosting/DigitalOceanDropletResourceDetector.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers DigitalOcean-specific collector discovery, best-effort metadata defaults, and hosted resource defaults on top of that same OTLP baseline.

The DigitalOcean slice is intentionally collector-first. If a host already has a shared OTLP collector, gateway, or route, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds DigitalOcean resource defaults such as region, DOKS cluster name, namespace, Droplet identifiers, or App Platform context. If the host instead wants a DOKS-local collector-service path, it can enable `Engine:Observability:Telemetry:DigitalOcean:UseInClusterCollectorService`, configure `CollectorServiceName`, and let the package build the in-cluster `svc.cluster.local` endpoint by using either the explicit collector namespace or the workload namespace.

Droplet metadata is also explicit. `UseDropletMetadataDefaults` turns on a short best-effort call to the standard Droplet metadata service so hosts can fill `host.id`, `host.name`, and `cloud.region` when those values were not configured directly. App Platform context stays explicit too: teams can set `AppId` and `AppUrl` in configuration, or bind App Platform values such as `${APP_ID}` and `${APP_URL}` into the runtime environment and let the package consume those names if they are present.

HTTPS custom trust is similarly explicit. `TrustedCaCertificatePath` is meant for self-managed collectors or gateways that use a non-system trust root, and the current implementation only applies that custom trust path to OTLP/HTTP traces and metrics. Because the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, the package rejects configurations that would require that same custom CA bundle for logs instead of pretending they are supported.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
