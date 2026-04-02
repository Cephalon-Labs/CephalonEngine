# Cephalon.Observability.Neo4jDependencies

`Cephalon.Observability.Neo4jDependencies` adds a supported Neo4j dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:Neo4j` configuration binding
- Neo4j endpoint configuration through either a full `Uri` or discrete `Host`/`Port`/`Scheme` settings
- optional database selection, credential support, and configurable Cypher health-query execution through `Neo4j.Driver`
- reusable `IDependencyHealthContributor` integration for Neo4j-backed readiness and diagnostics

## Main surfaces

- `Configuration/Neo4jDependencyDefinition.cs`
- `Configuration/Neo4jDependencyHealthOptions.cs`
- `Hosting/Neo4jDependencyHealthServiceCollectionExtensions.cs`
- `Services/Neo4jDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Neo4j driver, endpoint-scheme, and database-selection details out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need graph-database readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without rewriting driver/session probes per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
