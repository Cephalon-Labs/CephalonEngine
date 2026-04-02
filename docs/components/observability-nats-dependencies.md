# Cephalon.Observability.NatsDependencies

`Cephalon.Observability.NatsDependencies` is the reusable dependency-health companion package for NATS brokers and clusters.

## What it owns

- `Engine:Observability:DependencyHealth:Nats` option binding
- reusable NATS `CONNECT` plus `PING`/`PONG` probe flow
- token or username/password authentication wiring for broker reachability checks
- diagnostics conventions and runtime dependency-health contributions for NATS endpoints

## Main surfaces

- `Configuration/NatsDependencyDefinition.cs`
- `Configuration/NatsDependencyHealthOptions.cs`
- `Hosting/NatsDependencyHealthServiceCollectionExtensions.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package stays outside `Cephalon.Engine` and turns configured NATS endpoints into `IDependencyHealthContributor` data. Hosts keep the shared runtime health and readiness semantics from the engine, while this companion handles the provider-specific wire exchange: read the initial `INFO` line, send `CONNECT`, and verify a successful `PING`/`PONG` round-trip. Pair it with `Cephalon.Eventing` when a host wants broker-aware operational checks without pushing NATS socket details into the engine core.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Eventing](eventing.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
