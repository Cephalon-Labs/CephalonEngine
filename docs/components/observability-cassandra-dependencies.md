# Cephalon.Observability.CassandraDependencies

> **Maturity:** `M0` · **Ownership:** taxonomy-only · **Family:** `observability` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.Observability.CassandraDependencies` adds a supported Cassandra dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:Cassandra` configuration binding
- contact-point based Cassandra probe settings with optional keyspace and credentials
- lightweight CQL health-query execution through `CassandraCSharpDriver`
- reusable `IDependencyHealthContributor` integration for Cassandra-backed readiness and diagnostics

## Main surfaces

- `Configuration/CassandraDependencyDefinition.cs`
- `Configuration/CassandraDependencyHealthOptions.cs`
- `Hosting/CassandraDependencyHealthServiceCollectionExtensions.cs`
- `Services/CassandraDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Cassandra-native contact-point, keyspace, and query handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need Cassandra readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without rewriting cluster-session probes per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
