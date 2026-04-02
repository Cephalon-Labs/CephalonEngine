# Cephalon.Observability.MySqlDependencies

`Cephalon.Observability.MySqlDependencies` adds a supported MySQL and MariaDB dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:MySql` configuration binding
- connection-string or discrete host/database probe settings for MySQL and MariaDB endpoints
- lightweight `SELECT 1`-style health-query execution through `MySqlConnector`
- reusable `IDependencyHealthContributor` integration for SQL-backed readiness and diagnostics

## Main surfaces

- `Configuration/MySqlDependencyDefinition.cs`
- `Configuration/MySqlDependencyHealthOptions.cs`
- `Hosting/MySqlDependencyHealthServiceCollectionExtensions.cs`
- `Services/MySqlDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps MySQL-specific connection handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need managed MySQL or MariaDB readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing connection-string parsing, SSL-mode selection, or public-key retrieval settings per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
