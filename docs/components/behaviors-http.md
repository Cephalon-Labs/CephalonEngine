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
  with explicit `.ApiVersion(...)` and `.WithOpenApiDocumentName(...)` override support, best-effort XML comment
  summaries/descriptions for module-owned REST endpoints, and separation between public REST docs
  and generic adapter endpoints
- **REST governance diagnostics** — stable `/engine/diagnostics` convention plus information-level
  startup log events for shorthand suppression, precedence suppression, applied override, no-op
  override, and preserved binding-fallback outcomes
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
  `GetRestProfiles()` hints, including explicit binding descriptors when they are declared, and
  `GetRestProfileBehaviorTypes()` hints for generated module-owned shorthand
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
  route placeholders and body-capable request bodies can still fill remaining object properties
- when no explicit binding plan is present, shorthand candidates still use the implicit
  query-plus-route merge baseline, and bounded placeholder additions can promote from that original
  implicit query-fallback surface; once explicit bindings exist, the stricter explicit-binding path
  remains in force
- when a host adds only partial explicit bindings to a shorthand candidate that originally had no
  explicit binding plan, the remaining unbound query properties now continue to follow that
  original implicit query-fallback surface instead of disappearing silently
- when a body-capable shorthand candidate keeps explicit bindings but still leaves deterministic
  request-body-bindable properties unbound, runtime truth now also keeps that preserved remaining
  request-body fallback visible through typed
  `BindingFallbackMode = PreserveRemainingBodyFallback`, while additive
  `metadata.bindingFallbackMode = preserve-remaining-body-fallback` remains compatibility-only
  metadata
- the canonical runtime answer for preserved fallback modes now lives on the typed transport
  contracts through `RestEndpointCandidateProjectionDescriptor.BindingFallbackMode` and
  `RestEndpointRuntimeDescriptor.BindingFallbackMode` using
  `RestEndpointBindingFallbackMode`, and
  `RestEndpointBindingFallbackModeExtensions.GetWireName()` /
  `TryParseWireName(...)` now provide the canonical compatibility bridge for metadata and JSON wire
  names; additive
  `metadata.bindingFallbackMode = preserve-source-implicit-fallback` and
  `metadata.bindingFallbackMode = preserve-remaining-body-fallback` remain compatibility-only
  metadata
- a JSON body that tries to overwrite a property reserved by an explicit non-body binding fails
  fast instead of silently winning or losing
- profile API-version metadata is still only a candidate endpoint version; host publication remains
  governed by `OpenApi:EnabledVersions`, `OpenApi:DefaultVersion`, and the legacy document
  allow-list settings

## REST governance diagnostics

When `Cephalon.Behaviors.Http` is active, `/engine/diagnostics` now also publishes source
`Cephalon.Behaviors.Http` with stable event ids `5200-5204`:

- `5200` — a shorthand candidate was suppressed by a configured governance rule
- `5201` — a shorthand candidate lost publication because a higher-precedence authoring style won
- `5202` — a matched shorthand override materially changed the published runtime answer
- `5203` — a matched shorthand override became a runtime no-op after truth reconciliation
- `5204` — a shorthand candidate preserved a typed binding fallback mode while partial explicit
  override reconciliation ran, including both preserved source implicit-query fallback and
  preserved remaining request-body fallback

Those events are emitted during startup/materialization when information-level logging is enabled.
For full hosted `MapCephalon()` paths, published-candidate logging reconciles against the actual
post-materialization endpoint answer first, so metadata-only or reorder-only no-op overrides keep
`MatchedOverrideIds` visible without falsely logging an applied override.

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

When the module wants to publish every profiled behavior beneath one owned public prefix, it can
opt into the generated shorthand explicitly:

```csharp
public sealed class CartModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/showcase/cart")
            .WithTagName("Cart API")
            .MapGeneratedProfiles();

        behaviors.Internal<RepriceCartBehavior>();
    }
}
```

`MapGeneratedProfiles()` derives the behavior-id prefix from the route-group prefix, so the example
above selects behaviors whose ids start with `showcase.cart`. If a module wants a different
selection rule, use `MapGeneratedProfiles("custom.prefix")` explicitly.

If the module wants to start from the behavior-id prefix instead, use
`behaviors.GroupFromBehaviorIdPrefix("showcase.cart").MapGeneratedProfiles();`. Cephalon derives
the public route-group prefix as `/showcase/cart` and still keeps generated publication explicit
and module-owned.

When a project wants the same module-owned REST behavior without creating a dedicated module class,
the host can still register an inline module explicitly:

```csharp
engine.AddRestBehaviorModule<GetCartBehavior>(
    new ModuleDescriptor(
        "showcase.cart",
        "Cart Module",
        "Publishes the cart public REST surface without a dedicated module class.",
        version: "1.0.0"),
    behaviors => behaviors.Group("/showcase/cart")
        .WithTagName("Cart API")
        .MapGeneratedProfiles("showcase.cart"));
```

`AddRestBehaviorModule<TMarker>(...)` remains module-owned: it materializes a real Cephalon module,
feeds the same `RestBehaviorModuleBuilder` projection pipeline, and still never publishes public
REST from `[AppBehavior]` alone. The marker type should come from the same behavior assembly that
owns the published behaviors, especially when the inline module uses `MapGeneratedProfiles(...)`,
because Cephalon resolves generated REST profile hints from that marker assembly. Use one stable
marker type per inline module; if a module needs richer lifecycle hooks, extra services, or more
advanced manual endpoints, prefer a dedicated `RestBehaviorModuleBase` subclass instead.

For the common generated-profile case where the route group should mirror the behavior-id prefix,
the host can now use the lower-ceremony inline helper:

```csharp
engine.AddGeneratedRestBehaviorModule<GetCartBehavior>(
    new ModuleDescriptor(
        "showcase.cart",
        "Cart Module",
        "Publishes the cart public REST surface through the generated inline helper.",
        version: "1.0.0"),
    "showcase.cart",
    group => group.WithTagName("Cart API"));
```

`AddGeneratedRestBehaviorModule<TMarker>(...)` still creates a real module, still maps through the
same generated-profile projection and runtime-catalog pipeline, and still never publishes public
REST from `[AppBehavior]` alone. Keep `AddRestBehaviorModule<TMarker>(...)` when the route group
should not mirror the behavior-id prefix or when the inline module needs more than one group.

Current helper behavior:

- keeps REST route shape in the host-adapter layer instead of overloading behavior attributes with
  HTTP-specific concerns
- lets behaviors carry candidate REST projection metadata without turning that metadata into a
  public route by itself
- gives behavior-owning REST modules both a dedicated base class and a low-code
  `AddRestBehaviorModule<TMarker>(...)` host helper instead of forcing authors to implement
  `IBehaviorOwnerModule` plus `IRestModule` manually
- adds `GroupFromBehaviorIdPrefix(...)` and `AddGeneratedRestBehaviorModule<TMarker>(...)` for the
  common generated-profile path where the route-group prefix should mirror the behavior-id prefix
- treats the REST DSL as the primary authoring path, so public routes also imply module ownership
- compiles author-facing REST group and endpoint declarations into a reusable internal projection
  model before the ASP.NET Core adapter materializes route groups and handlers
- keeps `Internal<TBehavior>()` available for internal-only behaviors or behaviors that will be exposed
  through custom/manual endpoints
- lets straightforward hosts register an explicit module-owned REST surface inline without creating
  a dedicated module class, while still reusing the same normalized projection and runtime-catalog
  path
- adds `MapProfile<TBehavior>()` as an explicit module-owned shorthand that consumes the behavior
  profile's method, relative pattern, optional candidate API version, and any explicit binding
  descriptors
- adds `MapGeneratedProfiles()` and `MapGeneratedProfiles(string)` as explicit module-owned
  low-code shorthands that publish every matching profiled behavior beneath one owned route group
- derives the default generated-selection prefix from the route-group path by trimming slashes and
  replacing `/` separators with `.`, while still allowing an explicit behavior-id prefix override
- derives the common generated route-group path from a dot-separated behavior-id prefix when
  authors use `GroupFromBehaviorIdPrefix(...)` or `AddGeneratedRestBehaviorModule<TMarker>(...)`
- prefers source-generated profile hints and falls back only to the explicitly targeted behavior
  type instead of broad assembly reflection
- prefers source-generated `GetRestProfiles()` plus `GetRestProfileBehaviorTypes()` hints for
  generated shorthand and falls back only to a bounded scan of the explicit owning module assembly
  when generated type hints are unavailable
- lets the inline helper point generated-profile discovery at the correct behavior assembly through
  its `TMarker` type instead of assuming the reusable helper implementation assembly owns those
  behaviors
- keeps explicit route bindings honest by requiring the declared binding name to match a
  placeholder present in the profile route template
- lets explicit group `.ApiVersion(...)` override profile-declared candidate versions, while
  conflicting profile-declared versions in the same group fail fast until the module resolves them
- keeps that explicit group `.ApiVersion(...)` authoritative for host-level version rewrites even
  when the same shorthand candidate later matches `RestApi:Overrides`, while shorthand method,
  bounded route-group-prefix, and constrained pattern overrides can still apply
- keeps runtime publication on the same module-owned path with `sourceKind = module-dsl`, while
  `/engine/rest-endpoints` exposes first-class `AuthoringStyle = behavior-module-profile` for the
  profile shorthand path, `behavior-module-generated` for the generated shorthand path,
  `behavior-module-dsl` for the fully explicit path, exposes first-class `RouteGroupPrefix` plus
  `RelativePattern` for the resolved published group boundary, exposes first-class nullable
  `BehaviorType` for the concrete behavior implementation identity, exposes first-class nullable
  `SourceId` for published source identity, keeps additive `metadata.authoringStyle`,
  `metadata.routeGroupPrefix`, `metadata.relativePattern`, `metadata.behaviorType`, and
  `metadata.sourceId` only for compatibility, now lets shorthand ASP.NET Core materialization read
  `ProjectedEndpoint.RouteGroupPrefix` directly instead of parsing compatibility metadata, and
  publishes first-class `BindingDescriptors` data for profile-driven explicit binding plans
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
- lets modules pin a published OpenAPI document name independently from the `/v{major}` route
  segment through `.WithOpenApiDocumentName(...)` when a stable named document such as `public`
  should remain authoritative
- prefixes the mapped REST route group with `/v{major}` for the resolved API major version, so
  hosts expose paths such as `/api/v1/showcase/cart/{cartId}`
- uses the resolved API major version as the operation-name version segment, falling back to the
  owning module descriptor major version before the default `v1` document name
- reads XML comments from the module and behavior assemblies when available so ASP.NET Core
  OpenAPI + Scalar can show summaries and descriptions without extra boilerplate
- maps behavior `<summary>` to the OpenAPI operation summary and behavior `<remarks>` to the
  OpenAPI operation description so Scalar does not repeat the same text twice
- lets modules declare candidate OpenAPI document membership through `.ApiVersion(...)`,
  `.WithOpenApiDocumentName(...)`, or the owning module major version, while the host-level
  `OpenApi:EnabledVersions` list still decides which versioned docs are actually published and
  legacy `OpenApi:Documents` remains the named-document publication contract
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
- if the same behavior is mapped through both explicit module DSL and shorthand publication, the
  explicit DSL route now wins by default and the lower-precedence shorthand candidate is suppressed
  instead of publishing side by side
- if the same behavior is mapped through both `MapProfile<TBehavior>()` and
  `MapGeneratedProfiles(...)`, the explicit per-behavior `MapProfile<TBehavior>()` route wins by
  default and the generated candidate is suppressed
- ASP.NET Core hosts can now also suppress descriptor-backed shorthand candidates through
  `RestApi:Suppressions`, which runs before precedence resolution, records the governing rule id on
  the suppressed candidate through `SuppressedBySuppressionId`, and intentionally leaves explicit
  module DSL or manual module-owned REST endpoints untouched
- ASP.NET Core hosts can now also override the effective API major version, HTTP method, or
  bounded published route-group prefix, constrained relative route pattern, required capability
  boundary, or explicit binding
  plan for descriptor-backed shorthand candidates through `RestApi:Overrides`, which now supports
  `ApiVersionMajor`, `Method`, `RouteGroupPrefix`, `Pattern`, `RequiredCapabilityKey`,
  `ClearRequiredCapability`, `Bindings`, `RemovedBindingProperties`, shorthand binding resets
  through `ClearBindings`, typed `BindingMode`,
  shorthand endpoint metadata `EndpointName`, `Summary`, and `Description`, OpenAPI tag-name
  rewrites through `TagName`, plus metadata clears `ClearEndpointName`, `ClearSummary`, and
  `ClearDescription`, records the applied rule id through
  `AppliedOverrideId` when the selected rule materially changes the effective answer, rewrites the
  shorthand candidate's `/v{major}` route segment and OpenAPI document name together when version
  changes, keeps the mapped endpoint method aligned when method changes, keeps the published
  route-group boundary aligned when `RouteGroupPrefix` changes, keeps the mapped endpoint route
  aligned when pattern changes, keeps actual ASP.NET Core endpoint metadata plus
  `/engine/rest-endpoints` aligned when capability-boundary or endpoint-metadata values change or
  clear, keeps effective tag truth aligned when `TagName` changes,
  applies explicit binding
  overrides in either default
  `ReplaceExplicit` mode or `MergeExplicit` property-patch-and-withdraw mode while leaving
  unbound route placeholders and remaining request-body fields available for deterministic
  fallback, now allows
  placeholder renames when the effective explicit route-binding plan covers the renamed placeholder
  set exactly, now also allows placeholder removals when the original projection already exposes
  explicit route-binding coverage for the original placeholder set and the effective explicit
  binding plan keeps every affected original route-bound property explicitly bound, now also allows
  placeholder additions when the effective explicit route-binding plan covers the full final
  placeholder set and every newly route-bound property was either already explicitly bound in the
  original projection, for `POST`/`PUT`/`PATCH` already part of the original deterministic
  remaining-body fallback surface, or for shorthand candidates with no explicit binding plan
  already part of the original implicit query-fallback surface; shorthand candidates with explicit
  bindings remain on the stricter explicit-binding path, `RouteGroupPrefix` stays bounded beneath
  the active REST root with no placeholders and no implicit API-version drift, materialization now
  splits effective shorthand route groups when only some candidates in one authored group are
  remapped or retagged, using the effective route-group prefix plus tag to keep actual endpoint
  tag metadata aligned with runtime truth, merge-time removals must still target properties the
  source shorthand already bound explicitly, no-explicit-plan shorthand candidates now also
  preserve their remaining implicit
  query-fallback surface when a host adds only partial explicit bindings, the typed runtime and
  projection descriptors now keep that preserved mode visible through `BindingFallbackMode` values
  backed by `RestEndpointBindingFallbackMode`, additive
  `metadata.bindingFallbackMode = preserve-source-implicit-fallback` and
  `metadata.bindingFallbackMode = preserve-remaining-body-fallback` remain compatibility-only
  metadata, `ClearBindings = true` can now also discard the source shorthand explicit binding plan
  entirely and return the candidate to the implicit route/query/body baseline, but that clear
  fails fast if the effective route placeholders would only remain satisfiable through removed
  explicit route-binding aliases, and explicit module DSL/manual routes plus shorthand groups with explicit
  `.ApiVersion(...)` remain authoritative for version selection

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
when an explicit profile-driven plan exists, first-class `AuthoringStyle`, first-class
`RouteGroupPrefix` plus `RelativePattern`, first-class nullable `BehaviorType`, first-class
nullable `SourceId`, nullable `CandidateId`, nullable `OriginalProjection`, and nullable
`OriginalEndpointName` / `OriginalSummary` / `OriginalDescription` when the published endpoint
comes from the module-owned behavior projection pipeline instead of a manual or behavior-helper
route. That published `OriginalProjection` keeps the original shorthand method, route,
document-version, tag name, and binding-plan truth visible directly on the final runtime endpoint,
while the original-metadata trio keeps the original shorthand endpoint name plus summary/description
visible beside the final effective endpoint metadata, so operators no longer need a
candidate-catalog join or behavior-doc fallback just to compare original-versus-effective
publication. When a host override clears one of those endpoint-metadata fields, the effective
ASP.NET Core endpoint metadata and runtime-catalog answer now stay intentionally empty while the
original-metadata trio still preserves the source shorthand lineage. When a host override retags one
candidate, the effective tag now flows through actual ASP.NET Core endpoint tag metadata plus the
candidate and published runtime catalogs while `OriginalProjection.TagName` preserves the source
shorthand tag; if only some candidates in one authored group are retagged, materialization splits
the effective published groups so runtime truth and actual tag metadata stay aligned.

The same runtime answer now has a companion candidate catalog for precedence visibility:

- `IRestEndpointCandidateRuntimeCatalog`
- `GET /engine/rest-endpoint-candidates`
- `GET /engine/rest-endpoint-candidates/{candidateId}`
- `RuntimeIntrospectionSnapshot.RestEndpointCandidates`

The same host now also publishes the grouped publication answer for those same behavior-backed
candidates:

- `IRestEndpointPublicationGroupRuntimeCatalog`
- `GET /engine/rest-endpoint-publication-groups`
- `GET /engine/rest-endpoint-publication-groups/{behaviorId}`
- `RuntimeIntrospectionSnapshot.RestEndpointPublicationGroups`

The host now also publishes the configured shorthand-suppression rules themselves through:

- `IRestEndpointSuppressionRuntimeCatalog`
- `GET /engine/rest-endpoint-suppressions`
- `GET /engine/rest-endpoint-suppressions/{suppressionId}`
- `RuntimeIntrospectionSnapshot.RestEndpointSuppressions`

The host now also publishes the configured shorthand-override rules themselves through:

- `IRestEndpointOverrideRuntimeCatalog`
- `GET /engine/rest-endpoint-overrides`
- `GET /engine/rest-endpoint-overrides/{overrideId}`
- `RuntimeIntrospectionSnapshot.RestEndpointOverrides`

Candidate entries answer the original shorthand projection shape through
`RestEndpointCandidateRuntimeDescriptor.OriginalProjection`, the final effective mapped answer
through `ProjectedEndpoint`, that same effective answer's original shorthand endpoint metadata
through `ProjectedEndpoint.OriginalEndpointName` / `OriginalSummary` / `OriginalDescription`,
authoring style, precedence rank, published versus suppressed status, and when suppression occurs
the winning candidate id plus an operator-facing suppression reason.
Candidate ids now resolve from that original shorthand projection before host-level overrides are
applied, while `ProjectedEndpoint.Id` continues to identify the final effective mapped endpoint
shape, and published behavior-backed endpoints now also keep that same join visible directly through
`RestEndpointRuntimeDescriptor.CandidateId`, while published endpoints of every authoring path now
also expose first-class `RestEndpointRuntimeDescriptor.AuthoringStyle` so consumers no longer have
to parse `metadata.authoringStyle` to recover published-endpoint authorship. Projected shorthand
endpoints now also keep endpoint names plus summary/description metadata aligned with the final
published runtime endpoint conventions, including XML-derived behavior docs when they exist and
module-description fallback when they do not, and published candidate registration now reconciles
capability-only, endpoint-metadata-only, and tag-only no-op matches against the actual mapped
endpoint so `MatchedOverrideIds` can still show the winning rule without forcing `AppliedOverrideId`
to pretend a same-key rewrite, no-op clear, or same-value metadata or tag rewrite changed the
published boundary. The final published `/engine/rest-endpoints` plus
`snapshot.RestEndpoints` answer now also keeps that same ordered `MatchedOverrideIds` set visible
directly, so operators no longer need a candidate join to see matched shorthand override rules on a
live endpoint. When more than one suppression
or override rule matches the same shorthand candidate, that same runtime entry now also keeps the
full ordered match set visible through `MatchedSuppressionIds` and `MatchedOverrideIds` before
`SuppressedBySuppressionId` or `AppliedOverrideId` identifies the selected winner. Today that
surface covers the normalized module-owned behavior projection path, including explicit module DSL
mappings,
`MapProfile<TBehavior>()` shorthand consumption, and
`MapGeneratedProfiles(...)` shorthand consumption.

Publication-group entries now answer that same runtime truth one behavior at a time: the ordered
candidate set, the published candidate ids that survived with the winning precedence rank, the
precedence-suppressed candidate ids, and the governance-suppressed candidate ids. That grouped
surface keeps multi-source shorthand stories readable even when one behavior has explicit DSL,
profile shorthand, generated shorthand, or selector-scoped governance interacting at once.

When suppression comes from host governance instead of precedence, the runtime now uses
`SuppressedBySuppressionId` rather than `SuppressedByCandidateId` so operators can see that a
configuration rule hid the candidate instead of another candidate winning. When more than one
suppression rule matched, the runtime also keeps the full specificity-ordered match set visible
through `MatchedSuppressionIds`.

Current governance baseline:

- configure shorthand suppression through `RestApi:Suppressions`
- configure shorthand API-version, HTTP-method, bounded route-group-prefix, constrained
  route-pattern, capability-boundary set-or-clear, explicit binding-plan replacement/merge/reset,
  and endpoint-metadata set-or-clear overrides through
  `RestApi:Overrides`
- target one or more `CandidateIds`, `Behaviors`, `Modules`, and optional `AuthoringStyles`, then
  optionally refine that match with `ApiVersionMajors`, `Methods`, `RelativePatterns`,
  `RouteGroupPrefixes`, `OpenApiDocumentNames`, and `TagNames`
- rules that omit all of `CandidateIds`, `Behaviors`, and `Modules` now fail fast instead of
  suppressing every shorthand candidate implicitly
- override rules must define at least one override action, require a positive `ApiVersionMajor`
  when that action is present, accept only `GET`, `POST`, `PUT`, `PATCH`, or `DELETE` for
  `Method`, require `Pattern` to be a valid relative ASP.NET Core route pattern, require
  `RouteGroupPrefix` to stay beneath the active REST root without placeholders or silent effective
  API-version changes, reject any rule that sets both `RequiredCapabilityKey` and
  `ClearRequiredCapability`, and fail fast if the effective binding plan becomes invalid for the
  effective HTTP method
- omit `AuthoringStyles` to suppress both shorthand styles by default:
  `behavior-module-profile` and `behavior-module-generated`
- exact `CandidateIds` reuse the stable ids published by `GET /engine/rest-endpoint-candidates`
- the optional selector refiners and exact candidate ids all match the original shorthand
  candidate shape before override actions are applied, including the original shorthand OpenAPI
  document name and primary tag name, so governance can pick one of several shorthand candidates
  that share the same behavior/module identity without relying on final rewritten route shape
- when more than one rule matches, Cephalon prefers candidate-targeted rules first, then fewer
  targeted candidate ids, then the more specific rule by populated target dimensions, behavior-
  targeted scope, narrower authoring-style scope, fewer total selector values, and finally stable
  rule id ordering; shorthand candidates keep the full ordered match trace visible through
  `MatchedSuppressionIds` and `MatchedOverrideIds` before one winning rule is selected
- shorthand groups that already declare `.ApiVersion(...)` explicitly remain authoritative over
  host-level version rewrites, while shorthand method and constrained pattern overrides can still
  apply
- the current override slice rewrites only the effective API major version, HTTP method,
  explicit OpenAPI document name, constrained relative route pattern, required capability
  boundary, capability-boundary clear, endpoint metadata set-or-clear actions, OpenAPI tag name,
  and/or explicit binding plan, including shorthand-only `ClearBindings` resets back to the
  implicit request-binding baseline, keeping the `/v{major}` route segment, OpenAPI document
  name, mapped endpoint, and runtime catalogs aligned; `ApiVersionMajor` only re-derives the
  document name when the authored shorthand group did not pin one explicitly
- pattern rewrites preserve the placeholder set by default and can now also rename placeholders
  when the effective explicit route-binding plan covers the renamed placeholder set exactly
- placeholder removals can now also apply when the original projection already exposes explicit
  route-binding coverage for the original placeholder set and the effective explicit binding plan
  keeps every affected original route-bound property explicitly bound
- placeholder additions can now also apply when the effective explicit route-binding plan covers
  the full final placeholder set and every newly route-bound property was either already explicitly
  bound in the original projection, for `POST`/`PUT`/`PATCH` already part of the original
  deterministic remaining-body fallback surface, or for shorthand candidates with no explicit
  binding plan already part of the original implicit query-fallback surface
- `BindingMode = MergeExplicit` can now upsert changed explicit bindings and withdraw selected
  original explicit bindings through `RemovedBindingProperties`, while failing fast if a removal
  targets a property the source shorthand never bound explicitly or if one merge rule both removes
  and overrides the same property
- `ClearBindings = true` can now discard the source shorthand explicit binding plan entirely, but
  it fails fast if the effective route would only stay valid through explicit placeholder aliases
  that the clear removed
- `ClearRequiredCapability = true` is now the explicit host answer for removing an inherited
  shorthand capability boundary; when that action wins, shorthand candidate projections, actual
  ASP.NET Core endpoint metadata, `/engine/rest-endpoints`, and `snapshot.RestEndpoints` all keep
  `RequiredCapabilityKey = null`, and `RequireCapability(...)` plus `ClearRequiredCapability()`
  both follow last-declaration-wins semantics so the earlier shorthand guard does not linger
- published endpoints now also expose nullable `OriginalRequiredCapabilityKey` plus
  `AppliedOverrideId`, so operators can read the source-versus-effective capability story directly
  from `/engine/rest-endpoints` when shorthand governance rewrites or clears a boundary; endpoint-
  level capability-only no-op clears now leave `AppliedOverrideId = null`
- published endpoints now also expose ordered `MatchedOverrideIds`, so the final runtime answer can
  keep shorthand override matches visible, including capability-only no-op matches, endpoint-
  metadata-only no-op matches, and reorder-only equivalent binding-plan rewrites that still leave
  `AppliedOverrideId = null`; that same semantic binding-set comparison now also drives projection
  reuse and post-materialization structural override checks
- `/engine/rest-endpoint-candidates` plus `RuntimeIntrospectionSnapshot.RestEndpointCandidates` now
  follow that same no-op governance truth for published candidates: capability-only clear matches
  against an already-empty boundary, same-key capability rewrites, same-value endpoint-metadata
  rewrites, endpoint-metadata clears against source metadata the module already removed, and
  reorder-only equivalent binding-plan rewrites keep `MatchedOverrideIds` visible but leave
  `AppliedOverrideId = null`; in that binding-order case the source explicit binding order remains
  the authoritative projected answer even when the equivalent reordered binding set comes from a
  later synthetic or future shorthand path
- broader implicit-property promotion beyond that constrained body-fallback-plus-bounded-query-
  fallback path plus broader binding-shape overrides beyond the current
  replace-plus-merge-explicit upsert-plus-withdraw model remain later work

Example:

```json
{
  "RestApi": {
    "Suppressions": {
      "prefer-generated-cart": {
        "Behaviors": [ "showcase.cart.get" ],
        "AuthoringStyles": [ "behavior-module-profile" ]
      }
    }
  }
}
```

Override example:

```json
{
  "RestApi": {
    "Overrides": {
      "cart-v2-profile": {
        "Behaviors": [ "showcase.cart.get" ],
        "AuthoringStyles": [ "behavior-module-profile" ],
        "ApiVersionMajors": [ 1 ],
        "Methods": [ "GET" ],
        "RelativePatterns": [ "/{cartId}" ],
        "RouteGroupPrefixes": [ "/api/v1/showcase/cart" ],
        "ApiVersionMajor": 2,
        "Method": "DELETE",
        "RouteGroupPrefix": "/api/v2/showcase/cart-admin",
        "Pattern": "/lookup/{cartId}",
        "RemovedBindingProperties": [ "Quantity" ],
        "Bindings": [
          {
            "PropertyName": "CorrelationId",
            "Source": "Header",
            "Name": "X-Trace-Id"
          },
          {
            "PropertyName": "Note",
            "Source": "Body",
            "Name": "memo"
          }
        ],
        "BindingMode": "MergeExplicit"
      }
    }
  }
}
```

Clear-bindings example:

```json
{
  "RestApi": {
    "Overrides": {
      "cart-profile-reset-bindings": {
        "Behaviors": [ "showcase.cart.get" ],
        "AuthoringStyles": [ "behavior-module-profile" ],
        "RelativePatterns": [ "/{cartId}" ],
        "ClearBindings": true
      }
    }
  }
}
```

That shorthand-only action removes the candidate's explicit binding descriptors and returns request
composition to the implicit route/query/body baseline. It is rejected when the final route would
need placeholder aliases that only existed in the removed explicit route bindings.

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
