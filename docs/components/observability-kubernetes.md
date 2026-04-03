# Cephalon.Observability.Kubernetes

`Cephalon.Observability.Kubernetes` adds platform-neutral Kubernetes collector defaults and resource guidance for Cephalon hosts.

## What it owns

- host-builder registration for generic Kubernetes collector-first OTLP wiring
- Kubernetes resource defaults such as cluster, namespace, pod, node, and container attributes
- optional in-cluster collector service discovery when no shared endpoint is configured
- optional HTTPS trusted-CA bundle handling for OTLP/HTTP traces and metrics against shared or in-cluster collectors

## Main surfaces

- `Configuration/KubernetesTelemetryExportOptions.cs`
- `Hosting/KubernetesHostApplicationBuilderExtensions.cs`
- `Hosting/KubernetesSummaryHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This companion package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. It keeps generic Kubernetes collector discovery, service-DNS assumptions, and cluster-local trust handling in a reusable package that hosts can opt into without pulling vendor-specific cloud logic back into the engine core.

Use this package when a deployment targets a platform-neutral Kubernetes environment or a self-managed cluster and wants an explicit in-cluster collector path on top of the shared `Engine:Observability:Telemetry` contract. When a deployment needs vendor-specific propagation, hosted resource defaults, or managed-ingestion semantics, pair that same shared contract with a more specific companion such as `Cephalon.Observability.Aws`, `Cephalon.Observability.Gcp`, `Cephalon.Observability.AzureMonitor`, `Cephalon.Observability.OpenShift`, `Cephalon.Observability.DigitalOcean`, or `Cephalon.Observability.Tanzu` instead of overloading this generic Kubernetes baseline.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Cephalon.Observability.OpenShift](observability-openshift.md)
- [Cephalon.Observability.DigitalOcean](observability-digitalocean.md)
- [Cephalon.Observability.Tanzu](observability-tanzu.md)
- [Operations](../operations.md)
- [Observability provider authoring](../observability-provider-authoring.md)
