# Cephalon.AspNetCore.JsonRpc

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore.JsonRpc` adds JSON-RPC transport support to the ASP.NET Core host.

## What it owns

- JSON-RPC service registration
- JSON-RPC module contract for package and in-repo modules
- JSON-RPC route mapping under the host transport surface
- Cephalon endpoint rate-limiting participation for the public JSON-RPC route group

## Main surfaces

- `Hosting/JsonRpcTransportServiceCollectionExtensions.cs`
- `Modules/IJsonRpcModule.cs`
- `Routing/JsonRpcTransportRouteMapper.cs`

## Source structure

- `Hosting`
- `Modules`
- `Routing`

## How it fits

Use this package when a host selects the `JsonRpc` transport. The engine still owns transport selection; this package just provides the ASP.NET Core adapter that makes that selection executable.

When `Engine:Resilience:RateLimiting:Enabled=true`, the adapter applies the effective Cephalon ASP.NET Core endpoint policy to the configured JSON-RPC prefix. `/engine/rate-limiting` reports the covered `json-rpc` transport id with request-response semantics, and rejected JSON-RPC calls receive a JSON-RPC 2.0 error envelope (`code: -32029`, `message: "Too many requests"`, `id: null`) over HTTP `200 OK` instead of a JSON ProblemDetails or result-envelope payload. The same envelope is emitted for behavior-dispatch `http.jsonrpc` endpoints when the shared host limiter rejects before behavior-execution resilience runs.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
