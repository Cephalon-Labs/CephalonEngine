# Cephalon.Observability.PostgresDependencies

> **Maturity:** `M0` · **Ownership:** taxonomy-only · **Family:** `observability` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.Observability.PostgresDependencies` adds a supported Postgres dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:Postgres` configuration binding
- connection-string or discrete host/database probe settings for Postgres endpoints
- lightweight `SELECT 1`-style health-query execution through `Npgsql`
- reusable `IDependencyHealthContributor` integration for Postgres-backed readiness and diagnostics

## Main surfaces

- `Configuration/PostgresDependencyDefinition.cs`
- `Configuration/PostgresDependencyHealthOptions.cs`
- `Hosting/PostgresDependencyHealthServiceCollectionExtensions.cs`
- `Services/PostgresDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Postgres-specific connection handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need managed database readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing connection-string parsing, SSL-mode handling, or health-query loops per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
