# Cephalon.Observability.KafkaDependencies

> **Maturity:** `M0` · **Ownership:** `taxonomy-only` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.KafkaDependencies` adds a supported Kafka dependency-health path for Cephalon hosts.

## What it owns

- `Engine:Observability:DependencyHealth:Kafka` configuration binding
- bootstrap-server, topic, and broker-authentication probe settings for Kafka clusters
- reusable metadata probes through `Confluent.Kafka`
- reusable `IDependencyHealthContributor` integration for broker-backed readiness and diagnostics

## Main surfaces

- `Configuration/KafkaDependencyDefinition.cs`
- `Configuration/KafkaDependencyHealthOptions.cs`
- `Hosting/KafkaDependencyHealthServiceCollectionExtensions.cs`
- `Services/KafkaDependencyHealthProbeHostedService.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package keeps Kafka-specific metadata retrieval and broker-auth configuration out of `Cephalon.Engine` while still feeding the existing dependency-health contract. Hosts can opt into it when they need Kafka readiness to surface through `/engine/dependencies`, `/health/live`, `/health/ready`, and `/engine/diagnostics` without re-implementing bootstrap-server parsing, topic presence checks, or SASL/TLS settings per host. When active, it also publishes its probe event ids through the shared runtime diagnostics catalog.

## Related docs

- [Cephalon.Observability](observability.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
