# Cephalon.AspNetCore.JsonRpc

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore.JsonRpc` adds JSON-RPC transport support to the ASP.NET Core host.

## What it owns

- JSON-RPC service registration
- JSON-RPC module contract for package and in-repo modules
- JSON-RPC route mapping under the host transport surface

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

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
