# Cephalon.Observability.MongoDbDependencies

> **Maturity:** `M2` · **Ownership:** `provider-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.MongoDbDependencies` adds a supported MongoDB dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:MongoDb` configuration binding
- connection-string or discrete host/database probe settings for MongoDB endpoints
- lightweight `ping`-style database command execution through `MongoDB.Driver`
- reusable `IDependencyHealthContributor` integration for document-database readiness and diagnostics

## Main surfaces

- `Configuration/MongoDbDependencyDefinition.cs`
- `Configuration/MongoDbDependencyHealthOptions.cs`
- `Hosting/MongoDbDependencyHealthServiceCollectionExtensions.cs`
- `Services/MongoDbDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps MongoDB-specific connection handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need managed MongoDB readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing connection-string parsing, TLS flags, or command execution per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
