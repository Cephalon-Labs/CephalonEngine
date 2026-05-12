# Cephalon.AspNetCore.JsonRpc

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore.JsonRpc` adds JSON-RPC transport support to the ASP.NET Core host.

## What it owns

- JSON-RPC service registration
- JSON-RPC module contract for package and in-repo modules
- JSON-RPC route mapping under the host transport surface
- direct JSON-RPC module resilience enforcement for configured `Engine:Resilience` timeout, circuit-breaker, and bulkhead policies
- JSON-RPC-native resilience error envelopes for direct module endpoints without Wolverine or consumer endpoint filter code

## Main surfaces

- `Hosting/JsonRpcTransportServiceCollectionExtensions.cs`
- `Hosting/JsonRpcDirectModuleResilienceFilter.cs`
- `Hosting/JsonRpcDirectModuleResilienceRuntimeContributor.cs`
- `Modules/IJsonRpcModule.cs`
- `Routing/JsonRpcTransportRouteMapper.cs`

## Source structure

- `Hosting`
- `Modules`
- `Routing`

## How it fits

Use this package when a host selects the `JsonRpc` transport. The engine still owns transport selection; this package just provides the ASP.NET Core adapter that makes that selection executable.

When `Engine:Resilience` enables `Timeout`, `CircuitBreaker`, or `Bulkhead`, the adapter applies those policies to direct `IJsonRpcModule` endpoints through a route-group endpoint filter. Consumers keep writing normal JSON-RPC module endpoints; they do not need Wolverine, Polly references, custom middleware, or per-endpoint catch blocks to get the direct-module resilience baseline.

Direct module timeout and open-circuit outcomes return HTTP `503` with JSON-RPC error code `-32053`; full bulkheads return HTTP `429` with JSON-RPC error code `-32029`. All three envelopes keep the standard `jsonRpc` / `result` / `error` shape, include `error.data.cephalonCode`, `error.data.fault = resilience`, and `error.data.statusCode`, and preserve the request `id` when the request body can be parsed. Open-circuit responses also include `retryAfterSeconds` in `error.data` plus a `Retry-After` header.

The adapter publishes its live posture through `/engine/technology-surfaces/json-rpc` as `json-rpc-direct-module-resilience`, including policy source, execution mode, timeout/circuit/bulkhead settings, live circuit state, retry-after posture, bulkhead active/queued/accepted/rejected counters, and `wolverineRequired=false` / `consumerCodeRequired=false`.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Runtime contract index](../runtime-contract-index.md)
