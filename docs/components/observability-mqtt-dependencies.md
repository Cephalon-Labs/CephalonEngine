# Cephalon.Observability.MqttDependencies

> **Maturity:** `M0` · **Ownership:** taxonomy-only · **Family:** `observability` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.Observability.MqttDependencies` is the reusable dependency-health companion package for MQTT brokers and edge messaging endpoints.

## What it owns

- `Engine:Observability:DependencyHealth:Mqtt` option binding
- reusable MQTT 3.1.1 `CONNECT` plus `PINGREQ`/`PINGRESP` probe flow
- explicit username/password and TLS wiring for broker reachability checks
- diagnostics conventions and runtime dependency-health contributions for MQTT endpoints

## Main surfaces

- `Configuration/MqttDependencyDefinition.cs`
- `Configuration/MqttDependencyHealthOptions.cs`
- `Hosting/MqttDependencyHealthServiceCollectionExtensions.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

This package stays outside `Cephalon.Engine` and turns configured MQTT endpoints into `IDependencyHealthContributor` data. Hosts keep the shared runtime health and readiness semantics from the engine, while this companion handles the provider-specific wire exchange: open the TCP or TLS connection, send `CONNECT`, validate the broker `CONNACK`, and verify a `PINGREQ` -> `PINGRESP` round-trip. Pair it with `Cephalon.Eventing` or `Cephalon.Edge` when a host wants broker-aware operational checks without pushing MQTT socket details into the engine core.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Eventing](eventing.md)
- [Cephalon.Edge](edge.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
