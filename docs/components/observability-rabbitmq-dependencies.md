# Cephalon.Observability.RabbitMqDependencies

> **Maturity:** `M0` · **Ownership:** `taxonomy-only` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.RabbitMqDependencies` adds a supported RabbitMQ dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:RabbitMq` configuration binding
- AMQP connection-string or discrete host/virtual-host probe settings for RabbitMQ endpoints
- reusable broker connection probes through `RabbitMQ.Client`
- reusable `IDependencyHealthContributor` integration for broker-backed readiness and diagnostics

## Main surfaces

- `Configuration/RabbitMqDependencyDefinition.cs`
- `Configuration/RabbitMqDependencyHealthOptions.cs`
- `Hosting/RabbitMqDependencyHealthServiceCollectionExtensions.cs`
- `Services/RabbitMqDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps RabbitMQ-specific connection handling out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need broker readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing AMQP connection setup, vhost handling, or TLS toggles per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
