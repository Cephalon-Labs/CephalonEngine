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
- **Shared behavior API surface** — `BehaviorApiSurfaceDescriptor` plus `BehaviorApiSurfaceRouteResolver` for canonical route-shaped behavior HTTP endpoints
- **Behavior-aware REST helpers** — `MapBehaviorRestGroup(...)` plus `BehaviorRestEndpointGroup.MapBehaviorGet/Post/Put/Patch/Delete(...)` for Minimal API-style route groups that dispatch into behaviors
- **OpenAPI enrichment** — module tag names and descriptions, module-major API-version defaults with explicit `.ApiVersion(...)` override support, best-effort XML comment summaries/descriptions for behavior-driven REST endpoints, and default exclusion of generic behavior adapter endpoints from REST OpenAPI docs
- **Hosting** — `IBehaviorCollectionBuilder.AddHttpBehaviorBindings()` extension registering all bindings in DI

## Transport bindings

| Transport ID | Binding class | Route |
|---|---|---|
| `http.rest` | `RestHttpBehaviorBinding` | Conventional `POST/GET {RestPrefix}/{document}/{group}/{operation}` by default, or a configured single-method route when `ViaHttpRest(rest => ...)` supplies an explicit contract |
| `http.jsonrpc` | `JsonRpcHttpBehaviorBinding` | Canonical `POST {JsonRpcPrefix}/{document}/{group}/{operation}` |
| `http.graphql` | `GraphqlHttpBehaviorBinding` | Canonical `POST {GraphQLPrefix}/{document}/{group}/{operation}` |
| `http.graphql-sse` | `GraphqlSseBehaviorBinding` | Canonical `POST {GraphQLSsePrefix}/{document}/{group}/{operation}` |
| `http.graphql-ws` | `GraphqlWsBehaviorBinding` | Canonical `GET {GraphQLWsPrefix}/{document}/{group}/{operation}` |
| `http.sse` | `SseBehaviorBinding` | Canonical `GET {SsePrefix}/{document}/{group}/{operation}` |
| `http.ws` | `WebSocketBehaviorBinding` | Canonical `GET {WsPrefix}/{document}/{group}/{operation}` |

Cephalon now uses a shared `BehaviorApiSurfaceDescriptor` for the generic route-shaped behavior transports.
By default the API surface is derived from the behavior id, so `cart.get` becomes logical group `cart`
plus operation `get`, which the HTTP bindings project into canonical versioned routes such as
`/api/v1/cart/get`, `/json-rpc/v1/cart/get`, `/graphql/v1/cart/get`, `/graphql-sse/v1/cart/get`,
`/graphql-ws/v1/cart/get`, `/sse/v1/cart/get`, and `/ws/v1/cart/get`.

The host controls those canonical prefixes through the canonical `ApiRoutes:Prefixes` contract:
`ApiRoutes:Prefixes:Rest`,
`ApiRoutes:Prefixes:GraphQL`, `ApiRoutes:Prefixes:JsonRpc`, `ApiRoutes:Prefixes:Sse`,
`ApiRoutes:Prefixes:Ws`, `ApiRoutes:Prefixes:GraphQLWs`, and `ApiRoutes:Prefixes:GraphQLSse`,
while the resolved default version/document segment comes from `OpenApi:DefaultVersion` or
`ApiRoutes:DefaultBehaviorDocumentName`. The older `/behaviors/{id}` aliases are no longer part of
the generated behavior HTTP surface. For REST specifically, `ApiRoutes:Prefixes:Rest = ""` is valid
and means "mount the versioned REST surface at the root," while `null` still falls back to `/api`.

## REST declaration styles

Cephalon now treats generic REST authoring as three distinct levels:

- **Attribute-only generic behavior baseline**: `[BehaviorAllowedPatterns(...)]` plus
  `[BehaviorAllowedTransports(...)]` can synthesize the runtime baseline when no explicit topology
  exists and the pattern choice is unambiguous
- **Annotation-driven generic REST activation**: `[BehaviorAllowedTransports("http.rest")]`
  turns on the conventional generic REST adapter route for that behavior
- **Topology-driven generic REST contract**: `ConfigureTopology(...)` plus
  `ViaHttpRest(rest => ...)` keeps the behavior on the generic REST adapter surface, but lets the
  behavior choose one HTTP method, an optional explicit route template, and optional route/query
  member remapping
- **Module-owned public REST endpoints**: `MapBehaviorRestGroup(...)` plus
  `MapBehaviorGet/Post/Put/Patch/Delete(...)` gives the module a full Minimal API-style public REST
  surface with OpenAPI tags, XML comments, and route-group control

For generic REST declarations, use one REST activation style per behavior:

- if a behavior only declares `[BehaviorAllowedPatterns(...)]` plus
  `[BehaviorAllowedTransports(...)]`, the runtime uses that attribute-only baseline when exactly one
  pattern is declared; if multiple patterns are declared, startup fails fast until another topology
  source selects one
- if `[BehaviorAllowedTransports("http.rest")]` is present, do not also call `ViaHttpRest()` or
  `ViaHttpRest(rest => ...)` for the same behavior
- if both declaration styles appear, the runtime fails fast and the source generator reports
  `ABT0014`
- for authoring convenience, `[BehaviorAllowedTransports("http.grpc")]` is accepted and normalized
  to canonical `grpc`

## Shared behavior API surface

When the default `behavior-id -> group/operation` split is not the public contract you want, override
it explicitly in `ConfigureTopology(...)`:

```csharp
public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
{
    builder.AsCqrs()
        .ViaHttpRest()
        .ViaHttpJsonRpc()
        .ViaHttpSse()
        .ViaWebSocket()
        .WithApiSurface("catalog/items", "lookup");
}
```

That one transport-agnostic descriptor is then reused by the generic REST, JSON-RPC, GraphQL,
GraphQL-SSE, GraphQL-WS, SSE, and WebSocket behavior bindings. Source-generated topology descriptors
honor the same `WithApiSurface(...)` contract, so the compile-time and fluent-runtime paths stay
aligned.

When no explicit API surface is supplied, Cephalon derives the public path deterministically from the
behavior id:

- `cart.add-item` becomes group `cart` plus operation `add-item`, which projects to `/api/v1/cart/add-item`
- `cart.add-item.draft` becomes group `cart/add-item` plus operation `draft`, which projects to `/api/v1/cart/add-item/draft`

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .Register<PlaceOrderBehavior>(b => b.AsCqrs().ViaHttpRest().ViaHttpJsonRpc())
        .AddHttpBehaviorBindings()
    )
);
```

When the generic REST adapter needs a more REST-shaped contract without moving to a module-owned
Minimal API surface, configure the REST contract inside `ConfigureTopology(...)`:

```csharp
public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
{
    builder.AsDirect()
        .ViaHttpRest(rest => rest
            .MapPost("catalog/create-product/{productId}")
            .BindRoute("productId", nameof(CreateProductInput.ProductId))
            .BindQuery("draftMode", nameof(CreateProductInput.IsDraft)));
}
```

That explicit generic REST contract keeps the route on the generic behavior transport surface while
letting the behavior choose a single HTTP method plus a route template. Route-token names and
query-string keys bind by name by default; `BindRoute(...)` and `BindQuery(...)` only exist for
wire-name mismatches. JSON request bodies fill the remaining input members.

## Behavior-aware REST endpoints

`[BehaviorAllowedPatterns(...)]` plus `[BehaviorAllowedTransports(...)]` can now be enough to make a
behavior runnable when the baseline is obvious: one declared pattern plus declared transports means
the runtime can synthesize the descriptor without `ConfigureTopology(...)`. That attribute-only path
still stays transport-agnostic except for the generic REST activation rule.

`[BehaviorAllowedTransports("http.rest")]` stays the annotation-driven generic REST activation path.
It does **not** own HTTP method, route template, route grouping, or OpenAPI metadata. Use it when
the default conventional generic REST route is enough; move to `ViaHttpRest(rest => ...)` for a
single explicit generic REST contract, or to `MapBehaviorRestGroup(...)` when the module owns the
public REST API.

When a module wants shaped REST endpoints instead of the generic behavior transport surface, map them
explicitly through the Minimal API helper layer:

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
- lets the module override the published tag name and tag description through `.WithTagName(...)` and `.WithTagDescription(...)`
- defaults the tag description from the module XML `<summary>` plus `<remarks>` when XML docs exist, falling back to `ModuleDescriptor.Description`
- defaults newly mapped endpoints to the owning module descriptor major version when one is available, so a module declared as `1.0.0` automatically joins the `v1` document and gets a `/v1` route prefix even without `.ApiVersion(1)`
- keeps `.ApiVersion(major)` as the explicit override when a module needs a public API version that differs from the module package major
- prefixes the mapped REST route group with `/v{major}` for the resolved API major version, so hosts expose paths such as `/api/v1/showcase/cart/{cartId}`
- uses the resolved API major version as the operation-name version segment, falling back to the owning module descriptor major version before the default `v1` document name
- reads XML comments from the module and behavior assemblies when available so ASP.NET Core OpenAPI + Scalar can show summaries and descriptions without extra boilerplate
- maps behavior `<summary>` to the OpenAPI operation summary and behavior `<remarks>` to the OpenAPI operation description so Scalar does not repeat the same text twice
- keeps generic route-shaped behavior HTTP endpoints runnable while excluding them from REST OpenAPI + Scalar descriptions by default, so public REST docs stay focused on module-owned REST groups
- relies on host-level `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion` when modules need additional versioned docs beyond the default `v1`
- expects `/scalar` to redirect to the default canonical document such as `/scalar/v1`, while `/scalar/` remains available for multi-document flows and hash-based selections are normalized back into pinned versioned links
- lets hosts move the OpenAPI JSON endpoint, Scalar UI base path, and REST host prefix through `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and `ApiRoutes:Prefixes:Rest`
- still interoperates with legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` settings when a host needs custom named docs instead of major-version documents
- keeps module-owned REST routing distinct from the generic behavior transport surface; side-by-side major-version behavior identities still require a later behavior-identity and transport-surface rework

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
