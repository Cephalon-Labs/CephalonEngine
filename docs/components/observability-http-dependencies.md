# Cephalon.Observability.HttpDependencies

`Cephalon.Observability.HttpDependencies` adds a supported external HTTP/API dependency-health path for Cephalon hosts.

## What it owns

- configuration binding for `Engine:Observability:DependencyHealth:Http`
- background refresh of configured HTTP dependency probes
- `IDependencyHealthContributor` registration over cached probe results so runtime health stays introspectable

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

This package keeps provider-specific upstream checks out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need external HTTP or API dependencies to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing probe loops per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
