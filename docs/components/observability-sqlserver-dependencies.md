# Cephalon.Observability.SqlServerDependencies

> **Maturity:** `M2` · **Ownership:** `provider-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.SqlServerDependencies` adds a supported SQL Server and Azure SQL dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:SqlServer` configuration binding
- connection-string or discrete host/database probe settings for SQL Server and Azure SQL endpoints
- lightweight `SELECT 1`-style health-query execution through `Microsoft.Data.SqlClient`
- reusable `IDependencyHealthContributor` integration for SQL-backed readiness and diagnostics

## Main surfaces

- `Configuration/SqlServerDependencyDefinition.cs`
- `Configuration/SqlServerDependencyHealthOptions.cs`
- `Hosting/SqlServerDependencyHealthServiceCollectionExtensions.cs`
- `Services/SqlServerDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps SQL Server-specific connection handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need managed SQL readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing connection-string parsing, encryption-mode selection, or health-query loops per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
