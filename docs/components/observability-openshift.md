# Cephalon.Observability.OpenShift

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.OpenShift` adds Red Hat OpenShift collector defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract when a shared collector endpoint or self-hosted OTLP path is already in place
- explicit OpenShift hosted defaults for cluster name, namespace, pod name, deployment environment, and `aro` / `rosa` cloud-provider stamping
- an opt-in in-cluster collector-service path for operator-managed OpenShift collectors when no shared endpoint is configured
- optional OTLP headers and optional HTTPS trusted-CA bundle handling for OTLP/HTTP traces and metrics
- startup diagnostics that summarize the active OpenShift export mode without logging secrets
- a sanitized `telemetry-export-openshift` runtime surface for `/engine/technology-surfaces` and `/engine/snapshot`

## Main surfaces

- `Configuration/OpenShiftTelemetryExportOptions.cs`
- `Hosting/OpenShiftHostApplicationBuilderExtensions.cs`
- `Hosting/OpenShiftSummaryHostedService.cs`
- `Hosting/OpenShiftTelemetryRuntimeContributor.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers OpenShift-specific collector discovery, trust material, and hosted resource defaults on top of that same OTLP baseline.

When the package is activated, it contributes the `telemetry-export-openshift` runtime surface so operators can see the OpenShift observability pack is active without exposing collector service endpoints, headers, or trust material.

The OpenShift slice is intentionally explicit about its modes. If a host already has a shared OTLP collector, route, or gateway path, the package keeps using the shared `Endpoint` or `UseSelfHostedDefaults` flow and only adds OpenShift resource defaults such as cluster name, namespace, pod name, and deployment environment. If the host instead wants a cluster-local collector-service path, it can enable `Engine:Observability:Telemetry:OpenShift:UseInClusterCollectorService`, configure `CollectorServiceName`, and let the package build the in-cluster `svc.cluster.local` endpoint by using either the explicit collector namespace or the workload namespace.

HTTPS custom trust is also explicit. `TrustedCaCertificatePath` is meant for projected OpenShift service CAs or other cluster-local trust bundles, and the current implementation only applies that custom trust path to OTLP/HTTP traces and metrics. Because the current OpenTelemetry logging exporter does not support custom `HttpClientFactory` wiring for HTTP, the package rejects configurations that would require that same custom CA bundle for logs instead of pretending they are supported.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
