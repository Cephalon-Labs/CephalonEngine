# Cephalon.Observability.DependencyHealth.Core

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.DependencyHealth.Core` is the shared dependency-health probe infrastructure package used by the optional observability dependency probe companions.

## What it owns

- the cached dependency-health store used by provider probe packages
- the bounded hosted-service refresh loop for configured dependency definitions
- shared timeout, exception, sorting, and `DependencyHealthReport` normalization behavior
- the reusable configuration base types for probe definitions and options

## Main surfaces

- `Configuration/DependencyDefinitionBase.cs`
- `Configuration/DependencyHealthOptionsBase.cs`
- `Services/DependencyHealthContributor.cs`
- `Services/DependencyHealthProbeHostedServiceBase.cs`
- `Services/DependencyHealthStore.cs`

## Source structure

- `Configuration`
- `Services`

## How it fits

This package is shipped so companion packs such as `Cephalon.Observability.HttpDependencies` can be consumed from local or remote NuGet feeds without needing a source-only project reference. It is intentionally not a host adapter and not a provider-specific probe package: hosts opt into concrete dependency checks through packages such as `Cephalon.Observability.HttpDependencies`, while this package keeps the shared scheduling and cached report behavior consistent across the dependency-health family.

The microservice multi-transport adoption proof (`scripts/validate-microservice-multi-transport-adoption.ps1`) now packages this core dependency alongside `Cephalon.Observability.HttpDependencies`, generates an app outside the repository, runs the generated host, and verifies the configured HTTP self-probe through `/engine/dependencies`.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.HttpDependencies](observability-http-dependencies.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
