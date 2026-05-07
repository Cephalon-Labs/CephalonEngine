# Cephalon.Observability.Tanzu

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.Tanzu` adds VMware Tanzu proxy handoff and hosted defaults for Cephalon hosts.

## What it owns

- host-builder registration for shared OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint or self-hosted OTLP path is already in place
- explicit VMware Tanzu hosted defaults for TKG, TKGI, and TAP on top of that same OTLP baseline
- an opt-in in-cluster proxy-service path for trace-focused handoff when no shared endpoint is configured
- optional OTLP headers and optional HTTPS trusted-CA bundle handling for OTLP/HTTP traces and metrics
- startup diagnostics that summarize the active Tanzu export mode without logging secrets
- a sanitized `telemetry-export-tanzu` runtime surface for `/engine/technology-surfaces` and `/engine/snapshot`

## Main surfaces

- `Configuration/TanzuTelemetryExportOptions.cs`
- `Hosting/TanzuHostApplicationBuilderExtensions.cs`
- `Hosting/TanzuSummaryHostedService.cs`
- `Hosting/TanzuTelemetryRuntimeContributor.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers Tanzu-specific proxy inputs and hosted resource defaults on top of that same OTLP baseline.

When the package is activated, it contributes the `telemetry-export-tanzu` runtime surface so operators can see the Tanzu observability pack is active without exposing proxy endpoints, headers, or trust material.

The Tanzu slice is intentionally explicit about its modes. If a host already has a shared OTLP collector, gateway, or route, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds Tanzu resource defaults such as cluster name, namespace, pod name, deployment environment, and the chosen hosted-platform stamp for `tkg`, `tkgi`, or `tap`.

If the host instead wants a Tanzu-local proxy path, it can enable `Engine:Observability:Telemetry:Tanzu:UseInClusterProxyService`, configure `ProxyServiceName` plus the explicit `ProxyPort`, and let the package build the in-cluster `svc.cluster.local` endpoint by using either the explicit proxy namespace or the workload namespace. That handoff path is intentionally trace-focused: the current Tanzu and Wavefront docs center proxy-mediated OpenTelemetry traces rather than one generic managed OTLP backend for every signal, so logs and metrics should stay on the shared collector path or the explicit self-hosted defaults.

HTTPS custom trust is similarly explicit. `TrustedCaCertificatePath` is meant for proxy, route, or collector endpoints that use a non-system trust root, and the current implementation only applies that custom trust path to OTLP/HTTP traces and metrics. Because the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, the package rejects configurations that would require that same custom CA bundle for logs instead of pretending they are supported.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
