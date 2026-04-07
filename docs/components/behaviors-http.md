# Cephalon.Behaviors.Http

`Cephalon.Behaviors.Http` provides the HTTP Transport Pack for the Adaptive Behavior Topology (ABT) system.
It wires behavior topology descriptors to HTTP transports via 7 concrete `IHttpBehaviorBinding` implementations, a lazy-init registry, and Minimal API helpers for behavior-shaped REST endpoints.

## What it owns

- **IHttpBehaviorBinding** — thin adapter contract between HTTP endpoints and `BehaviorDispatcher`
- **IHttpBehaviorBindingRegistry** — frozen lookup registry for all registered transport bindings
- **HttpBehaviorBindingRegistry** — default `FrozenDictionary`-backed registry implementation
- **LazyTransportBinding** — deferred-init wrapper; `MapAsync` is called exactly once on first request, keeping pod startup under 100 ms
- **DefaultBehaviorContext** — `IBehaviorContext` implementation built from `HttpContext` (correlation, tenant, user, trace from headers, optional `IEventStore` from DI)
- **7 transport bindings** — REST, JSON-RPC 2.0, GraphQL (HTTP), GraphQL-SSE, GraphQL-WS, SSE, WebSocket
- **Behavior-aware REST helpers** — `MapBehaviorRestGroup(...)` plus `BehaviorRestEndpointGroup.MapBehaviorGet/Post/Put/Patch/Delete(...)` for Minimal API-style route groups that dispatch into behaviors
- **OpenAPI enrichment** — module tags, module-major versioned operation names, and best-effort XML comment summaries/descriptions for behavior-driven REST endpoints
- **Hosting** — `IBehaviorCollectionBuilder.AddHttpBehaviorBindings()` extension registering all bindings in DI

## Transport bindings

| Transport ID | Binding class | Route |
|---|---|---|
| `http.rest` | `RestHttpBehaviorBinding` | `POST /behaviors/{id}`, `GET /behaviors/{id}` |
| `http.jsonrpc` | `JsonRpcHttpBehaviorBinding` | `POST /behaviors/{id}/jsonrpc` |
| `http.graphql` | `GraphqlHttpBehaviorBinding` | `POST /behaviors/{id}/graphql` |
| `http.graphql-sse` | `GraphqlSseBehaviorBinding` | `POST /behaviors/{id}/graphql/sse` |
| `http.graphql-ws` | `GraphqlWsBehaviorBinding` | `GET /behaviors/{id}/graphql/ws` |
| `http.sse` | `SseBehaviorBinding` | `GET /behaviors/{id}/events` |
| `http.ws` | `WebSocketBehaviorBinding` | `GET /behaviors/{id}/ws` |

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .Register<PlaceOrderBehavior>(b => b.AsCqrs().ViaHttpRest().ViaHttpJsonRpc())
        .AddHttpBehaviorBindings()
    )
);
```

## Behavior-aware REST endpoints

`[BehaviorAllowedTransports("http.rest")]` stays an activation and validation allowlist.
It does **not** own HTTP method, route template, route grouping, or OpenAPI metadata.

When a module wants shaped REST endpoints instead of the generic `/behaviors/{id}` surface, map them explicitly through the Minimal API helper layer:

```csharp
public void MapEndpoints(IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapBehaviorRestGroup(this, "/showcase/cart");

    group.MapBehaviorGet<GetCartBehavior>("/{cartId}");
    group.MapBehaviorPost<AddToCartBehavior>("/{cartId}/items");
    group.MapBehaviorDelete<RemoveFromCartBehavior>("/{cartId}/items/{productId}");
    group.MapBehaviorPost<CheckoutCartBehavior>("/{cartId}/checkout");
}
```

Current helper behavior:

- keeps REST route shape in the host-adapter layer instead of overloading behavior attributes with HTTP-specific concerns
- dispatches through `BehaviorDispatcher` using Minimal API handlers
- composes route values, query-string values, and JSON request bodies into the behavior input payload
- uses the owning module display name as the OpenAPI tag
- derives the default operation-name version segment from the module descriptor major version
- reads XML comments from the module and behavior assemblies when available so ASP.NET Core OpenAPI + Scalar can show summaries and descriptions without extra boilerplate

## DefaultBehaviorContext header conventions

| Header | Maps to |
|---|---|
| `X-Correlation-Id` | `Metadata["CorrelationId"]` |
| `X-Tenant-Id` | `Metadata["TenantId"]` |
| `Authorization` (sub claim) | `Metadata["UserId"]` |
| `X-Meta-*` | `Metadata[key-without-prefix]` |

> Status: ✅ Shipped — commit c957966 · 516/516 tests

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver, compatibility rules (required dependency)
- `Cephalon.Abstractions` — behavior contracts
- `Cephalon.AspNetCore` — host-level OpenAPI + Scalar surface for REST endpoints
