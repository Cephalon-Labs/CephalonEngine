# Cephalon.Observability.HttpDependencies

> **Maturity:** `M2` · **Ownership:** `provider-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.HttpDependencies` adds a supported external HTTP/API dependency-health path for Cephalon hosts.

## What it owns

- configuration binding for `Engine:Observability:DependencyHealth:Http`
- background refresh of configured HTTP dependency probes
- `IDependencyHealthContributor` registration over cached probe results so runtime health stays introspectable
- packaging closure over `Cephalon.Observability.DependencyHealth.Core` so generated apps can consume HTTP probes from local or remote NuGet feeds

## Main surfaces

- `Configuration/HttpDependencyDefinition.cs`
- `Configuration/HttpDependencyHealthOptions.cs`
- `Hosting/HttpDependencyHealthServiceCollectionExtensions.cs`
- `Services/HttpDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps provider-specific upstream checks out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need external HTTP or API dependencies to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing probe loops per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog. The microservice multi-transport adoption proof (`scripts/validate-microservice-multi-transport-adoption.ps1`) now packages this companion and its `Cephalon.Observability.DependencyHealth.Core` dependency, generates a `Microservice` app outside the repository, runs the host, and verifies a configured HTTP self-probe through `/engine/dependencies`.

This pack should stay protocol-generic. That means HTTP method, headers, common auth, timeout policy, expected status/body rules, and TLS-related probe behavior can grow here. Product-aware response semantics should stay in dedicated HTTP-based companion packs instead of turning this package into a catch-all for every HTTP product.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.DependencyHealth.Core](observability-dependency-health-core.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
