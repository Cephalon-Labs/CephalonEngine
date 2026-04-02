# Cephalon.Observability.ElasticsearchDependencies

`Cephalon.Observability.ElasticsearchDependencies` adds a supported Elasticsearch dependency-health path for Cephalon hosts.

## What it owns

- configuration binding for `Engine:Observability:DependencyHealth:Elasticsearch`
- background refresh of configured Elasticsearch cluster-health probes
- `IDependencyHealthContributor` registration over cached probe results so runtime health stays introspectable

## Main surfaces

- `Configuration/ElasticsearchDependencyDefinition.cs`
- `Configuration/ElasticsearchDependencyHealthOptions.cs`
- `Hosting/ElasticsearchDependencyHealthServiceCollectionExtensions.cs`
- `Services/ElasticsearchDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Elasticsearch-specific cluster-health checks out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need Elasticsearch clusters to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing auth, endpoint, and status mapping rules per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
