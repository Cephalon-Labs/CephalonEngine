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
  `IRestBehaviorEndpointGroupBuilder` for one-place public REST and internal behavior ownership,
  compiled internally into a normalized REST projection contract before Minimal API materialization
- **Metadata-only REST profile contract** — `BehaviorRestProfileAttribute`,
  `BehaviorRestBindingAttribute`, `BehaviorRestMethod`, `BehaviorRestProfileDescriptor`,
  `BehaviorRestBindingDescriptor`, and `BehaviorRestBindingSource` for behavior-authored candidate
  REST method, relative route, optional API-version hints, and explicit route/query/header/body
  binding plans that explicit module-owned shorthand such as `MapProfile<TBehavior>()` can consume
  without publishing public REST directly from behaviors
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
version/document segment comes from `ApiRoutes:DefaultBehaviorDocumentName` or, when that override
is unset, the raw configured `OpenApi:DefaultVersion`. `OpenApi:EnabledVersions` and legacy
`OpenApi:Documents` still govern only which OpenAPI + Scalar documents get published; they do not
trim the generic behavior transport route segment.
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
- if a behavior wants to describe a future low-ceremony REST projection, use
  `BehaviorRestProfileAttribute` only as metadata; it does not publish public REST by itself

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

## Metadata-only REST profiles

When a team wants lower-ceremony REST authoring later, the current shipped path is metadata first,
not direct public route activation. `BehaviorRestProfileAttribute` lets a behavior declare a
candidate REST method, relative pattern, and optional API major version for future module-owned
generated projections:

```csharp
using Cephalon.Behaviors.Http.Abstractions;

[AppBehavior("cart.get")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 2)]
public sealed class GetCartBehavior : IAppBehavior<GetCartInput, Result<GetCartOutput>>
{
    public Task<Result<GetCartOutput>> HandleAsync(
        GetCartInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        // behavior logic omitted
    }
}
```

Profiles can also carry explicit HTTP input-binding metadata when a module-owned shorthand route
needs deterministic source selection:

```csharp
[AppBehavior("cart.add-item")]
[BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 2)]
[BehaviorRestBinding(nameof(AddToCartInput.CartId), BehaviorRestBindingSource.Route, Name = "cartId")]
[BehaviorRestBinding(nameof(AddToCartInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
[BehaviorRestBinding(nameof(AddToCartInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
[BehaviorRestBinding(nameof(AddToCartInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
public sealed class AddToCartBehavior : IAppBehavior<AddToCartInput, Result<AddToCartOutput>>
{
    // behavior logic omitted
}
```

Current profile behavior:

- the attribute is metadata only and does not publish a public REST route
- the owning module still chooses whether that behavior becomes public REST through
  `ConfigureRestBehaviors(...)`
- `Cephalon.Behaviors.SourceGen` validates the core profile shape at build time and emits
  `GetRestProfiles()` hints, including explicit binding descriptors when they are declared
- `IRestBehaviorEndpointGroupBuilder.MapProfile<TBehavior>()` is now the shipped low-ceremony
  module-owned shorthand that consumes those hints through the existing REST projection pipeline
- profile consumption prefers source-generated `GetRestProfiles()` hints first and falls back to
  the explicitly targeted behavior type's attribute only when generated hints are unavailable
- valid profiles currently require a supported REST method, a non-empty leading-slash relative
  pattern such as `"/{cartId}"`, and a positive `ApiVersionMajor` when one is specified
- explicit profile bindings currently support `route`, `query`, `header`, and `body` sources for
  object inputs only; build-time diagnostics now reject invalid property names, duplicate property
  bindings, unsupported sources, route-placeholder mismatches, and body bindings on `GET` or
  `DELETE`, while module-owned profile consumption still re-checks the same contract when runtime
  falls back to direct attribute metadata
- when explicit bindings are present, they override the implicit merge baseline, while unbound
  route placeholders and request bodies can still fill remaining object properties
- a JSON body that tries to overwrite a property reserved by an explicit non-body binding fails
  fast instead of silently winning or losing
- profile API-version metadata is still only a candidate endpoint version; host publication remains
  governed by `OpenApi:EnabledVersions`, `OpenApi:DefaultVersion`, and the legacy document
  allow-list settings

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

Behaviors no longer activate REST through annotations or topology alone. Instead, modules map REST
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

When the behavior already carries a REST profile and the module wants the lower-ceremony path, the
same module-owned DSL can consume that metadata explicitly:

```csharp
public sealed class CartModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        var group = behaviors.Group("/showcase/cart")
            .WithTagName("Cart API");

        group.MapProfile<GetCartBehavior>();
        group.MapProfile<AddToCartBehavior>();
        group.MapProfile<RemoveFromCartBehavior>();
        group.MapProfile<CheckoutCartBehavior>();

        behaviors.Internal<RepriceCartBehavior>();
    }
}
```

Current helper behavior:

- keeps REST route shape in the host-adapter layer instead of overloading behavior attributes with
  HTTP-specific concerns
- lets behaviors carry candidate REST projection metadata without turning that metadata into a
  public route by itself
- gives behavior-owning REST modules a dedicated base class instead of requiring authors to
  implement `IBehaviorOwnerModule` plus `IRestModule` manually
- treats the REST DSL as the primary authoring path, so public routes also imply module ownership
- compiles author-facing REST group and endpoint declarations into a reusable internal projection
  model before the ASP.NET Core adapter materializes route groups and handlers
- keeps `Internal<TBehavior>()` available for internal-only behaviors or behaviors that will be exposed
  through custom/manual endpoints
- adds `MapProfile<TBehavior>()` as an explicit module-owned shorthand that consumes the behavior
  profile's method, relative pattern, optional candidate API version, and any explicit binding
  descriptors
- prefers source-generated profile hints and falls back only to the explicitly targeted behavior
  type instead of broad assembly reflection
- keeps explicit route bindings honest by requiring the declared binding name to match a
  placeholder present in the profile route template
- lets explicit group `.ApiVersion(...)` override profile-declared candidate versions, while
  conflicting profile-declared versions in the same group fail fast until the module resolves them
- keeps runtime publication on the same module-owned path with `sourceKind = module-dsl`, while
  `/engine/rest-endpoints` exposes `metadata.authoringStyle = behavior-module-profile` for the
  shorthand path, `behavior-module-dsl` for the fully explicit path, and first-class
  `BindingDescriptors` data for profile-driven explicit binding plans
- dispatches through `BehaviorDispatcher` using Minimal API handlers
- lets behaviors return raw `TOutput` or transport-neutral `Result<TOutput>` values
- uses the implicit route/query/body merge baseline only when no explicit profile bindings are
  present; profile-driven bindings switch to the descriptor-aware override model instead
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
- lets modules declare candidate OpenAPI document versions through `.ApiVersion(...)` or the owning
  module major version, while the host-level `OpenApi:EnabledVersions` list decides which of those
  versioned docs are actually published
- expects `/scalar` to redirect to the default canonical document such as `/scalar/v1`, while
  `/scalar/` remains available for multi-document flows and hash-based selections are normalized
  back into pinned versioned links
- inherits the host-injected Scalar selector, so when more than one published document exists the
  UI offers a version dropdown driven by the enabled-document allow-list and default-document choice
- lets hosts move the OpenAPI JSON endpoint, Scalar UI base path, and REST host prefix through
  `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and `ApiRoutes:Prefixes:Rest`
- still interoperates with legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` settings when
  a host needs custom named docs instead of major-version documents
- keeps module-owned REST routing distinct from the generic behavior transport surface; side-by-side
  major-version behavior identities still require a later behavior-identity and transport-surface
  rework
- rejects module-owned REST mappings that target a behavior explicitly owned by another module
- keeps `MapAdditionalEndpoints(...)` as the advanced/manual Minimal API escape hatch for REST
  modules that need extra routes beyond the default behavior DSL, while still flowing those manual
  routes into `/engine/rest-endpoints` and the shared duplicate-route guard
- if the same behavior is mapped through both explicit module DSL and `MapProfile<TBehavior>()`,
  the explicit DSL route now wins by default and the lower-precedence profile candidate is
  suppressed instead of publishing side by side

## REST runtime catalog and collision guard

When Cephalon materializes module-owned REST onto ASP.NET Core, whether through the behavior DSL or
through explicit manual module-owned routes, the host now also publishes the resolved public REST
answer through:

- `IRestEndpointRuntimeCatalog`
- `GET /engine/rest-endpoints`
- `GET /engine/rest-endpoints/{restEndpointId}`
- `RuntimeIntrospectionSnapshot.RestEndpoints`

Each catalog entry now carries the resolved public route shape rather than only the authoring-time
DSL input, including the final `HTTP method`, final route pattern, source kind, owning module id and
version when known, behavior id when the route dispatches through a Cephalon behavior, published
OpenAPI document name, resolved API major version, tags, first-class request-binding descriptors
when an explicit profile-driven plan exists, and additive metadata such as the route group prefix
plus relative pattern.

The same runtime answer now has a companion candidate catalog for precedence visibility:

- `IRestEndpointCandidateRuntimeCatalog`
- `GET /engine/rest-endpoint-candidates`
- `GET /engine/rest-endpoint-candidates/{candidateId}`
- `RuntimeIntrospectionSnapshot.RestEndpointCandidates`

Candidate entries answer the projected endpoint shape, authoring style, precedence rank, published
versus suppressed status, and when suppression occurs the winning candidate id plus an
operator-facing suppression reason. Today that surface covers the normalized module-owned behavior
projection path, including explicit module DSL mappings and `MapProfile<TBehavior>()` shorthand
consumption.

The host also now fails fast when two resolved public REST endpoints collide on the same
`HTTP method + route pattern`.

That collision guard is distinct from behavior ownership validation:

- ownership validation rejects a module that tries to publish another module's explicitly owned
  behavior
- route-collision validation rejects any duplicate resolved public REST projection, even when the
  conflicting endpoints came from different modules or future authoring styles

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
