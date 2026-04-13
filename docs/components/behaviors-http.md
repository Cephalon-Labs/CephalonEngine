# Cephalon.Behaviors.Http

`Cephalon.Behaviors.Http` provides the HTTP Transport Pack for the Adaptive Behavior Topology
(ABT) system. It wires behavior topology descriptors to generic HTTP transports through 6 concrete
`IHttpBehaviorBinding` implementations, a lazy-init registry, and Minimal API helpers for
module-owned REST endpoints.

## What it owns

- **IHttpBehaviorBinding** — thin adapter contract between generic HTTP endpoints and
  `BehaviorDispatcher`
- **IHttpBehaviorBindingRegistry** — frozen lookup registry for all registered transport bindings
- **HttpBehaviorBindingRegistry** — default `FrozenDictionary`-backed registry implementation
- **LazyTransportBinding** — deferred-init wrapper; `MapAsync` is called exactly once on first
  request, keeping pod startup under 100 ms
- **DefaultBehaviorContext** — `IBehaviorContext` implementation built from `HttpContext`
  (correlation, tenant, user, trace from headers, optional `IEventStore` from DI)
- **6 generic transport bindings** — JSON-RPC 2.0, GraphQL (HTTP), GraphQL-SSE, GraphQL-WS, SSE,
  WebSocket
- **Shared behavior API surface** — `BehaviorApiSurfaceDescriptor` plus
  `BehaviorApiSurfaceRouteResolver` for canonical route-shaped generic behavior HTTP endpoints
- **Behavior-aware REST helpers** — `MapBehaviorRestGroup(...)` plus
  `BehaviorRestEndpointGroup.MapBehaviorGet/Post/Put/Patch/Delete(...)` for Minimal API-style
  module route groups that dispatch into behaviors
- **REST behavior result mapping** — `BehaviorRestResponseMapper` projects raw behavior outputs and
  transport-neutral `Result<T>` outcomes into REST responses without teaching the core
  behavior contract about HTTP envelopes
- **REST behavior module base class** — `RestBehaviorModuleBase` so behavior-owning REST modules can
  expose public endpoints without implementing multiple author-facing interfaces directly
- **REST behavior-module DSL** — `IRestBehaviorModuleBuilder` plus
  `IRestBehaviorEndpointGroupBuilder` for one-place public REST and internal behavior ownership
- **OpenAPI enrichment** — module tag names and descriptions, module-major API-version defaults
  with explicit `.ApiVersion(...)` override support, best-effort XML comment
  summaries/descriptions for module-owned REST endpoints, and separation between public REST docs
  and generic adapter endpoints
- **Optional REST response envelope** — `ApiRoutes:ResultEnvelope:Enabled` projects REST success
  and error responses through `ResultModel<T>` / `ResultModelError` with an `errors` collection
  while leaving GraphQL,
  JSON-RPC, SSE, and WebSocket bindings on their native protocol envelopes
- **Hosting** — `IBehaviorCollectionBuilder.AddHttpBehaviorBindings()` extension registering the
  generic HTTP bindings in DI

## Transport bindings

| Transport ID | Binding class | Route |
|---|---|---|
| `http.jsonrpc` | `JsonRpcHttpBehaviorBinding` | Canonical `POST {JsonRpcPrefix}/{document}/{group}/{operation}` |
| `http.graphql` | `GraphqlHttpBehaviorBinding` | Canonical `POST {GraphQLPrefix}/{document}/{group}/{operation}` |
| `http.graphql-sse` | `GraphqlSseBehaviorBinding` | Canonical `POST {GraphQLSsePrefix}/{document}/{group}/{operation}` |
| `http.graphql-ws` | `GraphqlWsBehaviorBinding` | Canonical `GET {GraphQLWsPrefix}/{document}/{group}/{operation}` |
| `http.sse` | `SseBehaviorBinding` | Canonical `GET {SsePrefix}/{document}/{group}/{operation}` |
| `http.ws` | `WebSocketBehaviorBinding` | Canonical `GET {WsPrefix}/{document}/{group}/{operation}` |

Cephalon uses a shared `BehaviorApiSurfaceDescriptor` for the generic route-shaped behavior
transports. By default the API surface is derived from the behavior id, so `cart.get` becomes
logical group `cart` plus operation `get`, which the generic HTTP bindings project into canonical
versioned routes such as `/json-rpc/v1/cart/get`, `/graphql/v1/cart/get`,
`/graphql-sse/v1/cart/get`, `/graphql-ws/v1/cart/get`, `/sse/v1/cart/get`, and `/ws/v1/cart/get`.

The host controls those canonical prefixes through `ApiRoutes:Prefixes:GraphQL`,
`ApiRoutes:Prefixes:JsonRpc`, `ApiRoutes:Prefixes:Sse`, `ApiRoutes:Prefixes:Ws`,
`ApiRoutes:Prefixes:GraphQLWs`, and `ApiRoutes:Prefixes:GraphQLSse`, while the resolved default
version/document segment comes from `OpenApi:DefaultVersion` or `ApiRoutes:DefaultBehaviorDocumentName`.
The older `/behaviors/{id}` aliases are no longer part of the generated behavior HTTP surface.

Public REST uses the separate `ApiRoutes:Prefixes:Rest` setting through the ASP.NET Core host
adapter and `MapBehaviorRestGroup(...)`; `""` is valid and means "mount the versioned REST surface
at the root," while `null` still falls back to `/api`.

## REST ownership

Cephalon keeps public REST module-owned:

- do not declare `http.rest` in `[BehaviorAllowedTransports(...)]`
- do not call `ViaHttpRest()` or `ViaHttpRest(rest => ...)` in `ConfigureTopology(...)`
- prefer `RestBehaviorModuleBase` when a module owns behaviors and exposes them publicly over REST
- prefer `BehaviorModuleBase` when a module owns behaviors but does not expose a public REST surface
- keep low-level `IRestModule` for REST modules that do not dispatch into Cephalon behaviors
- author public REST routes in the owning module through
  `ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)`
- keep behavior attributes and topology focused on interaction pattern plus non-REST transports
- keep `WithApiSurface(...)` for the shared generic HTTP route surface, not for REST

When a behavior declares exactly one allowed pattern plus one or more allowed transports, the
runtime can synthesize that attribute-only baseline without `ConfigureTopology(...)`. That baseline
applies to non-REST transports only. If multiple patterns are declared, startup fails fast until
another topology source selects one explicitly. For authoring convenience,
`[BehaviorAllowedTransports("http.grpc")]` is accepted and normalized to canonical `grpc`.

## Shared behavior API surface

When the default `behavior-id -> group/operation` split is not the public contract you want,
override it explicitly in `ConfigureTopology(...)`:

```csharp
public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
{
    builder.AsCqrs()
        .ViaHttpJsonRpc()
        .ViaHttpGraphQl()
        .ViaHttpSse()
        .ViaWebSocket()
        .WithApiSurface("catalog/items", "lookup");
}
```

That one transport-agnostic descriptor is then reused by the generic JSON-RPC, GraphQL,
GraphQL-SSE, GraphQL-WS, SSE, and WebSocket behavior bindings. Source-generated topology
descriptors honor the same `WithApiSurface(...)` contract, so the compile-time and
fluent-runtime paths stay aligned.

When no explicit API surface is supplied, Cephalon derives the public path deterministically from
the behavior id:

- `cart.add-item` becomes group `cart` plus operation `add-item`, which projects to
  `/json-rpc/v1/cart/add-item`
- `cart.add-item.draft` becomes group `cart/add-item` plus operation `draft`, which projects to
  `/json-rpc/v1/cart/add-item/draft`

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .Register<PlaceOrderBehavior>(b => b.AsCqrs().ViaHttpJsonRpc().ViaHttpSse())
        .AddHttpBehaviorBindings()
    )
);
```

## Behavior-aware REST endpoints

Behaviors no longer activate REST through annotations or topology. Instead, modules map REST
endpoints explicitly through the Minimal API helper layer while still dispatching through
`BehaviorDispatcher`.

When a module wants a public REST surface, keep ownership and REST mapping together explicitly:

```csharp
public sealed class CartModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        var group = behaviors.Group("/showcase/cart");
        group.MapGet<GetCartBehavior>("/{cartId}");
        group.MapPost<AddToCartBehavior>("/{cartId}/items");
        group.MapDelete<RemoveFromCartBehavior>("/{cartId}/items/{productId}");
        group.MapPost<CheckoutCartBehavior>("/{cartId}/checkout");

        behaviors.Internal<RepriceCartBehavior>();
    }
}
```

Current helper behavior:

- keeps REST route shape in the host-adapter layer instead of overloading behavior attributes with
  HTTP-specific concerns
- gives behavior-owning REST modules a dedicated base class instead of requiring authors to
  implement `IBehaviorOwnerModule` plus `IRestModule` manually
- treats the REST DSL as the primary authoring path, so public routes also imply module ownership
- keeps `Internal<TBehavior>()` available for internal-only behaviors or behaviors that will be exposed
  through custom/manual endpoints
- dispatches through `BehaviorDispatcher` using Minimal API handlers
- lets behaviors return raw `TOutput` or transport-neutral `Result<TOutput>` values
- composes route values, query-string values, and JSON request bodies into the behavior input payload
- uses the owning module display name as the OpenAPI tag
- lets the module override the published tag name and tag description through `.WithTagName(...)`
  and `.WithTagDescription(...)`
- defaults the tag description from the module XML `<summary>` plus `<remarks>` when XML docs
  exist, falling back to `ModuleDescriptor.Description`
- defaults newly mapped endpoints to the owning module descriptor major version when one is
  available, so a module declared as `1.0.0` automatically joins the `v1` document and gets a
  `/v1` route prefix even without `.ApiVersion(1)`
- keeps `.ApiVersion(major)` as the explicit override when a module needs a public API version
  that differs from the module package major
- prefixes the mapped REST route group with `/v{major}` for the resolved API major version, so
  hosts expose paths such as `/api/v1/showcase/cart/{cartId}`
- uses the resolved API major version as the operation-name version segment, falling back to the
  owning module descriptor major version before the default `v1` document name
- reads XML comments from the module and behavior assemblies when available so ASP.NET Core
  OpenAPI + Scalar can show summaries and descriptions without extra boilerplate
- maps behavior `<summary>` to the OpenAPI operation summary and behavior `<remarks>` to the
  OpenAPI operation description so Scalar does not repeat the same text twice
- relies on host-level `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion` when modules need
  additional versioned docs beyond the default `v1`
- expects `/scalar` to redirect to the default canonical document such as `/scalar/v1`, while
  `/scalar/` remains available for multi-document flows and hash-based selections are normalized
  back into pinned versioned links
- inherits the host-injected Scalar selector, so when more than one resolved document exists the
  UI offers a version dropdown driven by `OpenApi:EnabledVersions` / `OpenApi:DefaultVersion`
- lets hosts move the OpenAPI JSON endpoint, Scalar UI base path, and REST host prefix through
  `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and `ApiRoutes:Prefixes:Rest`
- still interoperates with legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` settings when
  a host needs custom named docs instead of major-version documents
- keeps module-owned REST routing distinct from the generic behavior transport surface; side-by-side
  major-version behavior identities still require a later behavior-identity and transport-surface
  rework
- rejects module-owned REST mappings that target a behavior explicitly owned by another module
- keeps `MapAdditionalEndpoints(...)` as the advanced/manual Minimal API escape hatch for REST
  modules that need extra routes beyond the default behavior DSL

## REST response envelopes

`Cephalon.Behaviors.Http` now treats structured behavior outcomes and wire-format envelopes as
separate concerns:

- `IAppBehavior<TIn, TOut>` can still return raw payload types for simple success paths
- `IAppBehavior<TIn, Result<TOut>>` can communicate expected non-success branches such as
  `NotFound`, `Invalid`, `Conflict`, `Forbidden`, and `NoContent` without throwing transport-shaped
  exceptions
- REST projects those outcomes into HTTP status codes automatically
- when `ApiRoutes:ResultEnvelope:Enabled = true`, REST also wraps the payload into
  `ResultModel<T>` / `ResultModelError`
- error envelopes use an `errors` collection so validation and multi-reason failures can return
  more than one error item cleanly
- the OpenAPI + Scalar response list for behavior-owned REST helpers is configurable through
  `OpenApi:BehaviorRest:DocumentedStatusCodes`
- the default documented status set is `200`, `201`, `202`, `204`, `400`, `401`, `403`, `404`,
  `409`, and `500`, so server-error responses stay visible in docs by default
- when ASP.NET Core rate limiting is enabled, `429` is documented per endpoint when the effective
  rate-limiting policy actually applies to that REST route, and behavior/transport overrides can
  suppress it again for specific endpoints through `Engine:Resilience:RateLimiting:Overrides`
- when shared behavior-execution bulkhead enforcement is active, behavior-owned REST helpers also
  document and return `429` for bulkhead saturation
- when shared behavior-execution timeout enforcement is active, behavior-owned REST helpers also
  document and return `503` for timed-out dispatches
- when shared behavior-execution circuit-breaker enforcement is active, behavior-owned REST helpers
  also document and return `503` for open-circuit rejections, including retry-after details when the
  runtime can compute them
- `Engine:Resilience:BehaviorExecution:Overrides` can narrow or disable inherited timeout,
  circuit-breaker, and bulkhead answers per behavior id, per transport id, or per behavior+transport pair with
  `behavior+transport > behavior > transport > default` precedence, and REST docs follow that
  resolved runtime answer per endpoint
- GraphQL and JSON-RPC keep their protocol-native response shapes and are intentionally not wrapped
  in `ResultModel`

Example host override:

```json
{
  "OpenApi": {
    "BehaviorRest": {
      "DocumentedStatusCodes": [200, 400, 404, 500]
    }
  }
}
```

Example:

```csharp
public sealed class GetCartBehavior : IAppBehavior<GetCartInput, Result<GetCartOutput>>
{
    public async Task<Result<GetCartOutput>> HandleAsync(
        GetCartInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        var cart = await LoadCartAsync(input.CartId, cancellationToken);
        if (cart is null)
        {
            return Result.NotFound(
                "cart.not_found",
                $"Cart '{input.CartId}' was not found.");
        }

        return Result.Ok(
            new GetCartOutput(cart),
            message: "Cart resolved.");
    }
}
```

With `ApiRoutes:ResultEnvelope:Enabled = true`, REST projects that contract into payloads such as:

```json
{
  "title": "Ok",
  "message": "Cart resolved.",
  "success": true,
  "status_code": 200,
  "data": {
    "cartId": "cart-123"
  }
}
```

and:

```json
{
  "title": "Not found",
  "message": "Cart 'cart-123' was not found.",
  "success": false,
  "status_code": 404,
  "data": null,
  "errors": [
    {
      "key": "cart.not_found",
      "message": "Cart 'cart-123' was not found.",
      "severity": "error",
      "details": null
    }
  ]
}
```

Multi-reason validation faults project cleanly too. The showcase `AddToCartBehavior` now returns
`Result.Invalid(...)` with nested `BehaviorFault.InnerFaults`, which REST
projects to payloads such as:

```json
{
  "title": "Invalid request",
  "message": "Cart add-item request is invalid.",
  "success": false,
  "status_code": 400,
  "data": null,
  "errors": [
    {
      "key": "showcase.cart.add_item.product_id.required",
      "message": "Product id is required.",
      "severity": "error",
      "details": null
    },
    {
      "key": "showcase.cart.add_item.quantity.invalid",
      "message": "Quantity must be greater than zero.",
      "severity": "error",
      "details": null
    }
  ]
}
```

Keep that envelope as a REST host policy only. Messaging, events, GraphQL, and JSON-RPC should not
reuse it as a universal engine contract.

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
