# Cephalon.Observability.ConsulDependencies

> **Maturity:** `M0` · **Ownership:** taxonomy-only · **Family:** `observability` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.Observability.ConsulDependencies` adds a supported Consul dependency-health path for Cephalon hosts.

## What it owns

- configuration binding for `Engine:Observability:DependencyHealth:Consul`
- background refresh of configured Consul leader probes
- Consul ACL-token handling plus datacenter-aware status queries
- `IDependencyHealthContributor` registration over cached probe results so runtime health stays introspectable

## Main surfaces

- `Configuration/ConsulDependencyDefinition.cs`
- `Configuration/ConsulDependencyHealthOptions.cs`
- `Hosting/ConsulDependencyHealthServiceCollectionExtensions.cs`
- `Services/ConsulDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Consul-specific leader checks out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need Consul control-plane reachability and leadership to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing ACL token handling, endpoint shaping, or leader-response semantics per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

Even though Consul uses HTTP, this pack stays separate from `Cephalon.Observability.HttpDependencies` because it owns Consul-specific headers, default endpoint resolution, and leader-response interpretation. That keeps the generic HTTP pack reusable while still giving Consul operators a first-class contract.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
