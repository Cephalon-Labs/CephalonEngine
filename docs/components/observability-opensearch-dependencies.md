# Cephalon.Observability.OpenSearchDependencies

`Cephalon.Observability.OpenSearchDependencies` adds a supported OpenSearch dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:OpenSearch` configuration binding
- OpenSearch cluster-health endpoint shaping from a base URL plus optional index-specific checks
- optional bearer-token or basic-auth support for secured OpenSearch REST endpoints
- reusable `IDependencyHealthContributor` integration for OpenSearch-backed readiness and diagnostics

## Main surfaces

- `Configuration/OpenSearchDependencyDefinition.cs`
- `Configuration/OpenSearchDependencyHealthOptions.cs`
- `Hosting/OpenSearchDependencyHealthServiceCollectionExtensions.cs`
- `Services/OpenSearchDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps OpenSearch cluster-health endpoints, auth headers, and response mapping details out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need search-cluster readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without rewriting REST probe logic per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
