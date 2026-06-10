# Cephalon.Observability.ClickHouseDependencies

> **Maturity:** `M0` · **Ownership:** taxonomy-only · **Family:** `observability` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.Observability.ClickHouseDependencies` adds a supported ClickHouse dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:ClickHouse` configuration binding
- ClickHouse connection configuration through either a full connection string or discrete host/protocol/port/database settings
- optional username/password auth and configurable SQL health-query execution through `ClickHouse.Driver`
- reusable `IDependencyHealthContributor` integration for ClickHouse-backed readiness and diagnostics

## Main surfaces

- `Configuration/ClickHouseDependencyDefinition.cs`
- `Configuration/ClickHouseDependencyHealthOptions.cs`
- `Hosting/ClickHouseDependencyHealthServiceCollectionExtensions.cs`
- `Services/ClickHouseDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps ClickHouse transport, connection-string, and query semantics out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need analytics or warehouse readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without rewriting ADO probe logic per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
