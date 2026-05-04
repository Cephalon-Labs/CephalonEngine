# Cephalon.Observability.MemcachedDependencies

> **Maturity:** `M0` · **Ownership:** `taxonomy-only` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.MemcachedDependencies` adds a supported Memcached dependency-health path for Cephalon hosts.

## What it owns

- configuration binding for `Engine:Observability:DependencyHealth:Memcached`
- background refresh of configured Memcached dependency probes
- Memcached text-protocol version probes over TCP
- `IDependencyHealthContributor` registration over cached probe results so runtime health stays introspectable

## Main surfaces

- `Configuration/MemcachedDependencyDefinition.cs`
- `Configuration/MemcachedDependencyHealthOptions.cs`
- `Hosting/MemcachedDependencyHealthServiceCollectionExtensions.cs`
- `Services/MemcachedDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Memcached-specific socket probing out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need Memcached-backed cache readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing probe loops or version checks per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
