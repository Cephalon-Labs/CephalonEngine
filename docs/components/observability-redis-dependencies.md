# Cephalon.Observability.RedisDependencies

`Cephalon.Observability.RedisDependencies` adds a supported Redis and cache dependency-health path for Cephalon hosts.

## What it owns

- configuration binding for `Engine:Observability:DependencyHealth:Redis`
- background refresh of configured Redis dependency probes
- optional ACL/password authentication plus logical database selection before health pings
- `IDependencyHealthContributor` registration over cached probe results so runtime health stays introspectable

## Main surfaces

- `Configuration/RedisDependencyDefinition.cs`
- `Configuration/RedisDependencyHealthOptions.cs`
- `Hosting/RedisDependencyHealthServiceCollectionExtensions.cs`
- `Services/RedisDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Redis-specific socket probing out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need Redis or cache readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing probe loops, auth handling, or `PING` checks per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
