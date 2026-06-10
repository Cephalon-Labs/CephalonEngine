# Cephalon.Observability.OracleDependencies

> **Maturity:** `M0` · **Ownership:** taxonomy-only · **Family:** `observability` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.Observability.OracleDependencies` adds a supported Oracle Database dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:Oracle` configuration binding
- connection-string or discrete host/service-name probe settings for Oracle Database endpoints
- lightweight `SELECT 1 FROM DUAL`-style health-query execution through `Oracle.ManagedDataAccess.Core`
- reusable `IDependencyHealthContributor` integration for Oracle-backed readiness and diagnostics

## Main surfaces

- `Configuration/OracleDependencyDefinition.cs`
- `Configuration/OracleDependencyHealthOptions.cs`
- `Hosting/OracleDependencyHealthServiceCollectionExtensions.cs`
- `Services/OracleDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Oracle-specific connection handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need managed Oracle readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing Easy Connect shaping or Oracle query probes per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
