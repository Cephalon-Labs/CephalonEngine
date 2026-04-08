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
- **OpenAPI enrichment** — module tags, module-major API-version defaults with explicit `.ApiVersion(...)` override support, and best-effort XML comment summaries/descriptions for behavior-driven REST endpoints
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

These generic bindings still use behavior-id-driven routes. The REST helper layer does not automatically rewrite `http.graphql`, `http.graphql-sse`, `http.graphql-ws`, `http.sse`, or `http.ws` into module-owned versioned paths in this round.

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
- defaults newly mapped endpoints to the owning module descriptor major version when one is available, so a module declared as `1.0.0` automatically joins the `v1` document and gets a `/v1` route prefix even without `.ApiVersion(1)`
- keeps `.ApiVersion(major)` as the explicit override when a module needs a public API version that differs from the module package major
- prefixes the mapped REST route group with `/v{major}` for the resolved API major version, so hosts expose paths such as `/api/v1/showcase/cart/{cartId}`
- uses the resolved API major version as the operation-name version segment, falling back to the owning module descriptor major version before the default `v1` document name
- reads XML comments from the module and behavior assemblies when available so ASP.NET Core OpenAPI + Scalar can show summaries and descriptions without extra boilerplate
- maps behavior `<summary>` to the OpenAPI operation summary and behavior `<remarks>` to the OpenAPI operation description so Scalar does not repeat the same text twice
- relies on host-level `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion` when modules need additional versioned docs beyond the default `v1`
- expects `/scalar` to redirect to the default canonical document such as `/scalar/v1`, while `/scalar/` remains available for multi-document flows and hash-based selections are normalized back into pinned versioned links
- lets hosts move the OpenAPI JSON endpoint, Scalar UI base path, and REST host prefix through `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and `ApiRoutes:Prefixes:Rest`
- still interoperates with legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` settings when a host needs custom named docs instead of major-version documents
- does not yet provide a transport-agnostic route contract for the generic behavior bindings; side-by-side major-version transport surfaces still require a later behavior-identity and transport-surface rework

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
