# Cephalon Module Authoring

Cephalon now ships a first-class module authoring path alongside app scaffolding.

See also: [Engineering standards](engineering-standards.md) for the engine quality baseline that module authors must respect (compatibility, packaging, code-quality gates, naming, XML doc, testing), and [Long-range engine direction](long-range-direction.md) for the multi-decade horizon framing that explains *why* module authoring stays host-agnostic and additive instead of being collapsed into one fixed app shell.

## Starting points

Choose the starter that matches the package you want to author:

- `dotnet new cephalon-module`
  - host-agnostic module package
  - capability registration
  - lifecycle hooks
  - package-owned localization resources
  - generated `cephalon.package.json` copied to the output folder
- `dotnet new cephalon-rest-module`
  - everything in `cephalon-module`
  - plus `IRestModule` and a localized REST endpoint
  - best for generic REST modules that do not dispatch into Cephalon behaviors
  - generated `cephalon.package.json` copied to the output folder
- `dotnet new cephalon-rest-behavior-module`
  - everything needed for a behavior-backed REST module starter
  - uses `RestBehaviorModuleBase` plus a metadata-profiled starter behavior mapped through `MapProfile<TBehavior>()`
  - best for modules that own Cephalon behaviors and expose them publicly over REST
  - generated `cephalon.package.json` copied to the output folder

For behavior-owning modules, add `Cephalon.Behaviors` or `Cephalon.Behaviors.Http` and prefer
`BehaviorModuleBase` or `RestBehaviorModuleBase` instead of implementing
`IBehaviorOwnerModule`/`IRestModule` directly in normal authoring code.

For module packages that should be discoverable by assembly, package, or configuration-driven
discovery, keep the generated starter's `Cephalon.Engine.SourceGen` analyzer reference. It emits
module descriptors during build so the engine can discover modules without scanning assembly
types at runtime. Modules that require custom construction should be registered explicitly through
`AddModule(...)` or package registration code.

For concrete reference implementations, use:

- `samples/Cephalon.ReferenceModule.Operations`
- `samples/Cephalon.Sample.Showcase/Modules/*.cs`

## Recommended package shape

Keep module packages small and explicit:

- `Application/`
  - package-owned services and state
- `Contracts/`
  - payloads or envelopes exposed by the package
- `Registration/`
  - the module entry point and transport contribution interfaces
- `cephalon.package.json`
  - package manifest used by `Engine:Discovery:Packages:ManifestPath` and `Engine:Discovery:PackageDirectories`
  - version, compatibility, and optional integrity metadata for independently shipped modules

That keeps the authoring path close to the same module-first ideas used by Cephalon apps.

## Authoring checklist

1. Define a `ModuleDescriptor` with a stable id, display name, description, tags, and version.
2. Register package-owned services in `ConfigureServices(...)`.
3. Register explicit capabilities in `RegisterCapabilities(...)`.
4. Implement lifecycle hooks only when the package owns startup/runtime behavior.
5. Use `ILocalizedResourceContributor` for package-owned text instead of hardcoding strings in hosts.
6. Keep `Cephalon.Engine.SourceGen` referenced as an analyzer when the module should be discovered from an assembly or package manifest; generated discovery supports non-abstract, non-generic module classes with an accessible parameterless constructor.
7. Use `ITechnologyContributor` when the package introduces a future-tech profile, workload convention, or package hint that the host should be able to select through `Engine:Technologies`.
8. Use `ITechnologyServiceContributor` or `ITechnologyCapabilityContributor` when package services or capabilities should only activate for specific technology profiles.
9. If the package extends a shipped technology pack, register the pack-specific contributor service in `ConfigureServices(...)` such as `IAgentToolContributor`, `IKnowledgeCollectionContributor`, `IKnowledgeDocumentProvider`, `IEventChannelContributor`, or `IEdgeNodeContributor`; use abstraction-level catalogs and command seams such as `IAgentToolRunCatalog`, `Cephalon.Abstractions.Retrieval.IKnowledgeIndexCatalog`, and `Cephalon.Abstractions.Retrieval.IKnowledgeIndexer` when modules need runtime posture or bounded operator actions without depending on implementation packages.
10. Use `ITechnologyRuntimeContributor` when the package or pack needs to expose an operator-facing runtime snapshot through `/engine/technology-surfaces`.
11. Use `ICellBoundaryContributor`, `ICellRouteContributor`, and `ICellHealthIsolationContributor` when the package owns explicit cell topology, governed cell-to-cell paths, or cell health-isolation posture that operators should be able to inspect through `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, `/engine/technology-surfaces/cell-based-architecture`, and `/engine/snapshot`; keep automation overlays in `Engine:Cells:TrafficAutomation` so route and module ownership stay authoritative.
12. Use `IExecutionGraphContributor` when the package needs to publish operator-facing workflow or execution-graph descriptors through `/engine/execution-graphs` and `/engine/snapshot`.
13. Use `IHostedExecutionContributor` when the package needs to publish operator-facing hosted or background execution descriptors through `/engine/hosted-executions`, `/engine/runtime-story`, and `/engine/snapshot`.
14. Use `IDataProductContributor` when the package needs to publish operator-facing data product descriptors through `/engine/data-products` and `/engine/snapshot`.
15. Use `ICdcCaptureContributor` when the package needs to publish operator-facing CDC capture descriptors through `/engine/cdc-captures` and `/engine/snapshot`.
16. Use `IProjectionContributor` when the package needs to publish operator-facing projection descriptors through `/engine/projections` and `/engine/snapshot`.
17. Use `IInboxContributor` when the package needs to publish operator-facing inbox descriptors through `/engine/inboxes` and `/engine/snapshot`.
18. Use `IOutboxContributor` when the package needs to publish operator-facing outbox descriptors through `/engine/outboxes` and `/engine/snapshot`.
19. Use `IAuthorizationPolicyContributor` when the package needs to publish operator-facing authorization-policy descriptors through `/engine/authorization-policies` and `/engine/snapshot`.
20. Add transport contribution interfaces only when the package really owns an external surface.
21. When a module explicitly owns Cephalon behaviors, prefer `BehaviorModuleBase` so ownership stays
    host-agnostic and deterministic.
22. When a behavior-owning module exposes REST endpoints, prefer `RestBehaviorModuleBase` so the same
    module can own internal-only behaviors and public REST-backed behaviors without splitting the
    bounded context across multiple module classes.
23. When a module exposes REST endpoints backed by behaviors, author that REST surface in
    `ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)` and keep REST out of behavior
    topology.

## Behavior-owning modules

Cephalon now has a first-class module-owned behavior authoring model:

- `BehaviorModuleBase` keeps behavior ownership host-agnostic through `ConfigureBehaviors(...)`
- `RestBehaviorModuleBase` adds one REST-specific authoring DSL through
  `ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)`
- one module can own both internal/process-only behaviors and public REST-backed behaviors
- generic `IRestModule` remains the right fit for REST modules that do not dispatch into Cephalon
  behaviors
- `Engine:Behaviors:AutoRegister` is now an opt-in generated-hint lookup path rather than the
  default ownership path

Use `BehaviorModuleBase` when the module owns behaviors but does not need to expose a public REST
surface:

```csharp
public sealed class CartModule : BehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        behaviors.Add<RepriceCartBehavior, RepriceCartInput, RepriceCartOutput>();
        behaviors.Add<CheckoutWorkflowBehavior, CheckoutWorkflowInput, CheckoutWorkflowOutput>(topology => topology
            .AsProcessManager()
            .ViaKafka());
    }
}
```

That shape keeps ownership explicit even when the behaviors only run through messaging, generic HTTP
transports, or background orchestration.

Use the typed `Add<TBehavior, TInput, TOutput>(...)` overload when a module-owned behavior should be
dispatch-ready without source generation. The type-only `Add(Type)` / `Add<TBehavior>(...)` overloads
remain useful for metadata-only ownership or source-generated registrations, but a behavior that is
actually dispatched must have a source-generated or explicitly registered closed execution slot.
Descriptor-backed authors can use `Add(Type behaviorType, Type inputType, Type outputType, ...)`
when generated metadata has already resolved the closed contract; this is the path used by the REST
behavior-module DSL so `MapProfile(...)`, `MapGeneratedProfiles(...)`, and grouped generated
shorthand endpoints dispatch through closed slots without asking project code to repeat generic
type arguments.

## Behavior-first REST authoring

Modules that expose Cephalon behaviors over REST can keep one module as the single owner of the
bounded context. `RestBehaviorModuleBase` keeps public REST exposure and module ownership in one
authoring surface while still materializing the actual Minimal API routes inside the ASP.NET Core
adapter layer.

Behavior declaration stays focused on the interaction pattern plus non-REST transports:

```csharp
[AppBehavior("cart.add-item")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.ws", "http.graphql", "http.sse")]
public sealed class AddToCartBehavior : IAppBehavior<AddToCartInput, Result<AddToCartOutput>>
{
    public Task<Result<AddToCartOutput>> HandleAsync(
        AddToCartInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        var faults = Validate(input); // validation helper omitted for brevity
        if (faults.Count > 0)
        {
            return Task.FromResult<Result<AddToCartOutput>>(Result.Invalid(
                "cart.add_item.invalid",
                "Cart add-item request is invalid.",
                new BehaviorFault
                {
                    Code = "cart.add_item.invalid",
                    Message = "Cart add-item request is invalid.",
                    Severity = BehaviorFaultSeverity.Error,
                    InnerFaults = faults
                }));
        }

        return Task.FromResult(Result.Ok(
            new AddToCartOutput(input.CartId, itemCount: 1, totalInCents: input.PriceInCents),
            message: "Item added to cart."));
    }
}
```

That shape gives the runtime an attribute-only baseline: the single allowed pattern (`cqrs`) plus
the declared transports become the resolved behavior topology when no explicit topology override
exists. Public REST is not part of that baseline; modules own REST explicitly.

If a behavior wants to carry candidate REST shorthand metadata for the shipped module-owned
profile/generated projection paths, keep that metadata in the HTTP package instead of overloading
behavior topology:

```csharp
using Cephalon.Behaviors.Http.Abstractions;

[AppBehavior("cart.get")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/{cartId}", ApiVersionMajor = 2)]
public sealed class GetCartBehavior : IAppBehavior<GetCartInput, Result<GetCartOutput>>
{
    // handler omitted
}
```

Current `BehaviorRestProfileAttribute` behavior:

- it is metadata only and does not publish a public REST route by itself
- `Cephalon.Behaviors.SourceGen` validates the method, relative pattern, and optional API version
  at build time, now also rejects malformed placeholder syntax such as unbalanced `{...}` route
  segments, and emits `GetRestProfiles()` hints that now preserve explicit binding descriptors when
  they are declared, descriptor-backed input contracts for explicit binding validation, plus
  `GetRestProfileBehaviorTypes()` hints for generated module-owned shorthand
- repeated `BehaviorRestBindingAttribute` declarations can describe explicit `route`, `query`,
  `header`, and `body` sources for object inputs when the module-owned shorthand needs deterministic
  input sourcing
- `BehaviorRestBindingSource` remains the code-authoring enum surface, but
  `BehaviorRestBindingSourceExtensions` now exposes the same stable `route`, `query`, `header`, and
  `body` wire names that JSON serialization uses; `Cephalon.Behaviors.SourceGen` validates against
  that canonical vocabulary while still emitting resolved enum member names into generated
  `GetRestProfiles()` hints so future enum-member renames can stay source-compatible by preserving
  those wire names
- runtime descriptor consumption and explicit binding-plan normalization now also point errors at
  those same canonical binding-source wire names, so troubleshooting unsupported profile bindings
  uses the same vocabulary as JSON and source generation without inspecting runtime input shape
- `BehaviorRestMethod` remains the code-authoring enum surface, but `BehaviorRestMethodExtensions`
  now exposes the same stable `get`, `post`, `put`, `patch`, and `delete` wire names that JSON
  serialization uses; `Cephalon.Behaviors.SourceGen` validates against that canonical vocabulary
  while still emitting resolved enum member names into generated `GetRestProfiles()` hints so
  future enum-member renames can stay source-compatible by preserving those wire names
- runtime profile normalization now also points unsupported or missing method guidance at those
  same canonical wire names, so build-time validation and runtime troubleshooting stay aligned
- the remaining runtime method guards now stay aligned too: non-body method body-binding rejections
  and unsupported REST method parser failures now also point at canonical `get`, `post`, `put`,
  `patch`, and `delete` wire names instead of mixing in enum/member-name wording
- the owning module still decides whether the behavior becomes public REST through
  `ConfigureRestBehaviors(...)`
- `IRestBehaviorEndpointGroupBuilder.MapProfile<TBehavior>()` is now the shipped low-ceremony
  module-owned shorthand that consumes generated or explicitly registered profile descriptors
  through the same normalized REST projection pipeline, including explicit binding descriptors
- `IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles()` and
  `MapGeneratedProfiles(string behaviorIdPrefix)` are now the shipped generated module-owned
  shorthands when one module-owned route group wants to publish every matching profiled behavior
- when explicit profile bindings are present, they override the implicit merge baseline while
  leaving unbound route placeholders and request-body fields free to fill the remaining object
  properties deterministically
- when a shorthand profile has no explicit binding plan, bounded placeholder additions can also
  promote from the original implicit query-fallback surface; once explicit bindings are present,
  the stricter explicit-binding rules still apply
- when a host adds only partial explicit bindings to that kind of shorthand profile, the remaining
  unbound query properties now continue to follow the original implicit query-fallback surface
- a JSON body that tries to overwrite a property reserved by an explicit non-body binding fails fast
- the optional `ApiVersionMajor` remains only a candidate endpoint version; the host still decides
  which OpenAPI documents are published through `OpenApi:EnabledVersions` or the legacy document
  allow-list settings
- when `BehaviorRestBindingAttribute` uses `Route`, keep the declared binding name aligned with a
  placeholder that actually exists in `BehaviorRestProfileAttribute.RelativePattern`; the build now
  rejects route-binding placeholder mismatches before generated profile descriptors are emitted
- runtime shorthand consumption still re-checks the final pattern with ASP.NET Core route parsing,
  so stale generated or explicitly registered profile descriptors cannot publish an invalid route
  shape silently

When a behavior needs to communicate expected branches without throwing exceptions for normal domain
flow, prefer `Result<T>` over a transport-specific envelope:

```csharp
[AppBehavior("cart.get")]
[BehaviorAllowedPatterns("cqrs")]
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

        return Result.Ok(new GetCartOutput(cart), message: "Cart resolved.");
    }
}
```

For no-payload branches such as `Invalid`, `NotFound`, `Conflict`, `Forbidden`, and `NoContent`,
Cephalon now lets the behavior use the shorter `Result.Invalid(...)` /
`Result.NotFound(...)` / `Result.Conflict(...)` shape because the enclosing
`Result<T>` return type supplies the target payload type automatically. If a synchronous
implementation uses `Task.FromResult(...)`, keep the wrapper target-typed as
`Task.FromResult<Result<TOut>>(...)` so the compiler does not stop at the intermediate
descriptor.

That keeps the behavior contract transport-neutral. REST can still project `Result<T>` to
HTTP status codes, and hosts can turn on the Cephalon REST envelope with
`ApiRoutes:ResultEnvelope:Enabled = true` when they want `ResultModel<T>` / `ResultModelError` on
the wire, including problem-style `type`, HTTP `status`, and an `errors` collection for validation
or multi-reason failures such as the `AddToCartBehavior` example above. When the flag is disabled,
REST expected failures and ASP.NET Core exception-handler responses stay on the native payload shape.
GraphQL and JSON-RPC keep their own protocol-native response shapes.

`BehaviorResult<T>` remains available as a compatibility alias when older code has not migrated to
the shorter `Result<T>` name yet.

If a behavior declares multiple allowed patterns, keep the attributes as an allowlist and add a
source-generator-supported `ConfigureTopology(...)` fluent chain or explicit module/fluent
registration so the engine does not need to guess which pattern should execute. For authoring
convenience, `[BehaviorAllowedTransports("http.grpc")]` is accepted and normalized to canonical
`grpc`.

If the generic route-shaped transports should expose a different logical public path than the default
`behavior-id -> group/operation` split, override it in the same topology declaration:

```csharp
public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
{
    builder.AsCqrs()
        .ViaHttpJsonRpc()
        .ViaHttpGraphQl()
        .ViaHttpSse()
        .ViaWebSocket()
        .WithApiSurface("cart", "get");
}
```

That shared API surface feeds the generic JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and
WebSocket behavior bindings, so they can all project canonical versioned routes such as
`/json-rpc/v1/cart/get`, `/graphql/v1/cart/get`, `/graphql-sse/v1/cart/get`,
`/graphql-ws/v1/cart/get`, `/sse/v1/cart/get`, and `/ws/v1/cart/get`. Hosts can move those
canonical prefixes with `ApiRoutes:Prefixes:GraphQL`, `ApiRoutes:Prefixes:JsonRpc`,
`ApiRoutes:Prefixes:Sse`, `ApiRoutes:Prefixes:Ws`, `ApiRoutes:Prefixes:GraphQLWs`, and
`ApiRoutes:Prefixes:GraphQLSse`. The generic adapter route segment itself comes from
`ApiRoutes:DefaultBehaviorDocumentName` or, when that override is unset, the raw configured
`OpenApi:DefaultVersion`.

The owning module then keeps both ownership and public REST mapping together:

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

When the behavior already declares `BehaviorRestProfileAttribute`, the module can keep the same
ownership model but avoid restating the HTTP method and relative pattern:

```csharp
public sealed class CartModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        var group = behaviors.Group("/showcase/cart");

        group.MapProfile<GetCartBehavior>();
        group.MapProfile<AddToCartBehavior>();
        group.MapProfile<RemoveFromCartBehavior>();
        group.MapProfile<CheckoutCartBehavior>();

        behaviors.Internal<RepriceCartBehavior>();
    }
}
```

When a module wants the lowest-ceremony explicit publication path for one owned public prefix, it
can opt into generated profile-backed mapping for all matching behaviors:

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

`MapGeneratedProfiles()` derives the behavior-id prefix from the route-group path, so the example
above selects ids such as `showcase.cart.get` and `showcase.cart.add-item`. Use
`MapGeneratedProfiles("custom.prefix")` when the module wants a different generated-selection
prefix.

When the module wants to start from the behavior-id prefix instead, use
`behaviors.GroupFromBehaviorIdPrefix("showcase.cart").MapGeneratedProfiles();`. Cephalon derives
the public group path as `/showcase/cart` and still keeps generated publication explicit and
module-owned.

When one module wants to fan one generated root prefix out into several owned public route groups,
use `MapGeneratedProfileGroups(...)` on the module builder:

```csharp
public sealed class GeneratedCatalogModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        "showcase.generated",
        "Generated Catalog Module",
        "Publishes generated REST profiles across several derived route groups.",
        version: "1.0.0");

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.MapGeneratedProfileGroups(
            "showcase.generated",
            group => group
                .WithTagName("Generated Catalog API")
                .WithHostGovernanceScope("generated-catalog"));
    }
}
```

Cephalon derives one public route group per parent behavior-id prefix, so
`showcase.generated.orders.lookup` and `showcase.generated.orders.create` share
`/showcase/generated/orders`, while `showcase.generated.inventory.lookup` lands beneath
`/showcase/generated/inventory`. The generated endpoints stay on the same
`behavior-module-generated` shorthand/runtime path and still never publish public REST from
`[AppBehavior]` alone. Hosts can now also target those same generated grouped branches by exact
behavior id or by subtree `BehaviorIdPrefixes`, so one rule can govern one derived group without
giving up module ownership or enumerating every exact generated candidate id.

When each derived route group needs different conventions, use the derived-prefix-aware overload:

```csharp
behaviors.MapGeneratedProfileGroups(
    "showcase.generated",
    (derivedBehaviorIdPrefix, group) =>
    {
        if (string.Equals(derivedBehaviorIdPrefix, "showcase.generated.orders", StringComparison.Ordinal))
        {
            group.ApiVersion(2)
                .WithTagName("Generated Orders API")
                .WithHostGovernanceScope("generated-orders");
        }
        else if (string.Equals(derivedBehaviorIdPrefix, "showcase.generated.inventory", StringComparison.Ordinal))
        {
            group.WithTagName("Generated Inventory API")
                .WithHostGovernanceScope("generated-inventory");
        }
    });
```

The callback now receives each derived generated behavior-id prefix before its profiles are mapped,
so one owning module can stamp different version, tag, or governance-scope conventions per branch
without enumerating every derived group manually. Generated shorthand already participates in host
governance by default, so this overload is mainly about per-branch projection conventions and
stable original-projection scope truth rather than opt-in governance.

When a project wants to stay explicit and module-owned without creating a dedicated module class,
the host can still register the full group manually:

```csharp
engine.AddRestBehaviorModule<GetCartBehavior>(
    new ModuleDescriptor(
        "showcase.cart",
        "Cart Module",
        "Publishes the cart REST surface without a dedicated module class.",
        version: "1.0.0"),
    behaviors => behaviors.Group("/showcase/cart")
        .WithTagName("Cart API")
        .MapGeneratedProfiles("showcase.cart"));
```

`AddRestBehaviorModule<TMarker>(...)` still creates a real module and still drives the same
normalized `RestBehaviorModuleBuilder` pipeline. It is a low-code authoring helper, not a return
to behavior-owned REST. Use a marker type from the same behavior assembly that owns the published
behaviors, especially when the inline module uses `MapGeneratedProfiles(...)`, because Cephalon
resolves generated REST profile hints from that marker assembly. Use one stable marker type per
inline module. When a module needs richer lifecycle hooks, extra services, or advanced manual
endpoints, prefer a dedicated `RestBehaviorModuleBase` subclass instead.

For the common generated-profile case where the route path should mirror the behavior-id prefix,
the host can now use the shorter helper:

```csharp
engine.AddGeneratedRestBehaviorModule<GetCartBehavior>(
    new ModuleDescriptor(
        "showcase.cart",
        "Cart Module",
        "Publishes the cart REST surface through the generated inline helper.",
        version: "1.0.0"),
    group => group.WithTagName("Cart API"));
```

`AddGeneratedRestBehaviorModule<TMarker>(descriptor, configureGroup?)` still creates a real module,
still maps through the same generated-profile projection and runtime-catalog pipeline, and still
never publishes public REST from `[AppBehavior]` alone. It derives the generated behavior-id prefix
from `ModuleDescriptor.Id`, so keep the explicit
`AddGeneratedRestBehaviorModule<TMarker>(descriptor, "prefix", ...)` overload when the inline
module id and generated behavior-id prefix should differ. Keep
`AddRestBehaviorModule<TMarker>(...)` when the module needs a non-default route group, a mix of
generated and manual endpoints, or more than one group.

When one inline module wants one generated root prefix to fan out into several owned public route
groups, the host can now use the grouped inline helper instead of creating a dedicated module class:

```csharp
engine.AddGeneratedRestBehaviorModuleGroups<GetCatalogOrderBehavior>(
    "showcase.generated",
    "Generated Catalog Module",
    "Publishes generated REST profiles across several derived route groups through the inline helper.",
    group => group
        .WithTagName("Generated Catalog API")
        .WithHostGovernanceScope("generated-catalog"),
    version: "1.0.0");
```

`AddGeneratedRestBehaviorModuleGroups<TMarker>(...)` still creates a real module, still delegates
to `MapGeneratedProfileGroups(...)`, still keeps grouped generated publication on the same
`behavior-module-generated` candidate/publication-group/governance/runtime-catalog path, and still
never publishes public REST from `[AppBehavior]` alone. The common overload derives the grouped
generated behavior-id root prefix from the inline module id, while the explicit `behaviorIdPrefix`
and `ModuleDescriptor` overloads remain available when inline module identity, grouped generated
ownership, or inline metadata should differ. Grouped generated prefix validation now also fails
fast consistently for both dedicated-module and inline-helper paths when the supplied dot-separated
prefix contains empty segments.

When the inline helper also needs different conventions per derived group, use the matching
derived-prefix-aware overload:

```csharp
engine.AddGeneratedRestBehaviorModuleGroups<GetCatalogOrderBehavior>(
    "showcase.generated",
    "Generated Catalog Module",
    "Publishes generated REST profiles across several derived route groups through the inline helper.",
    (derivedBehaviorIdPrefix, group) =>
    {
        if (string.Equals(derivedBehaviorIdPrefix, "showcase.generated.orders", StringComparison.Ordinal))
        {
            group.ApiVersion(2)
                .WithTagName("Generated Orders API")
                .WithHostGovernanceScope("generated-orders");
        }
        else if (string.Equals(derivedBehaviorIdPrefix, "showcase.generated.inventory", StringComparison.Ordinal))
        {
            group.WithTagName("Generated Inventory API")
                .WithHostGovernanceScope("generated-inventory");
        }
    },
    version: "1.0.0");
```

That keeps the same inline-module ownership story, but now the callback can branch on
`showcase.generated.orders` versus `showcase.generated.inventory` before the generated profiles are
materialized.

If the profile also needs an explicit binding plan, keep that detail on the behavior metadata
instead of moving it into the module:

```csharp
[AppBehavior("cart.add-item")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorRestProfile(BehaviorRestMethod.Post, "/{cartId}/items", ApiVersionMajor = 2)]
[BehaviorRestBinding(nameof(AddToCartInput.CartId), BehaviorRestBindingSource.Route, Name = "cartId")]
[BehaviorRestBinding(nameof(AddToCartInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
[BehaviorRestBinding(nameof(AddToCartInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
[BehaviorRestBinding(nameof(AddToCartInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
public sealed class AddToCartBehavior : IAppBehavior<AddToCartInput, Result<AddToCartOutput>>
{
    // handler omitted
}
```

For a `GET`-style profile that needs one explicit route alias but should keep the remaining unbound
query-string properties on the original implicit fallback surface, set
`BehaviorRestProfile(PreserveImplicitQueryFallback = true)` alongside at least one explicit
`BehaviorRestBindingAttribute`. `Cephalon.Behaviors.SourceGen` now rejects the flag through
`ABT0027` when no explicit bindings are present, and runtime normalization re-checks the same rule
when generated or explicitly registered descriptors are consumed. When that preserved source query
surface is part of the module's intentional contract, prefer declaring it here in the module-owned
profile; a host can now also opt the same preservation back in later through
`RestApi:Overrides:*:PreserveImplicitQueryFallback`, but that path is best reserved for
environment- or host-governed publication policy rather than source-owned route semantics.

Current helper behavior:

- gives behavior authors a base class instead of forcing modules to implement multiple interfaces
  directly
- keeps one module as the owner of both internal-only and REST-exposed behaviors without making
  authors declare the same public behavior twice
- treats `behaviors.Group(...).MapGet/MapPost/...` as the primary public REST DSL
- adds `engine.AddRestBehaviorModule<TMarker>(...)` as the lowest-ceremony explicit
  module-registration path when a host wants module-owned REST without a dedicated module class
- adds `behaviors.GroupFromBehaviorIdPrefix(...)` and
  `engine.AddGeneratedRestBehaviorModule<TMarker>(...)` as the lower-ceremony generated-path
  helpers when the route group should mirror the behavior-id prefix
- adds `behaviors.Group(...).MapProfile<TBehavior>()` as the lower-ceremony module-owned shorthand
  when the behavior already carries `BehaviorRestProfileAttribute`
- adds `behaviors.Group(...).MapGeneratedProfiles()` and `MapGeneratedProfiles(string)` as the
  explicit low-code module-owned shorthand when a whole owned route group should publish every
  matching profiled behavior
- adds `behaviors.MapGeneratedProfileGroups(string)`, the shared-group-configuration overload, the
  derived-prefix-aware overload, plus `engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(...)`
  when one generated root prefix should fan out into several derived owned route groups without
  inventing a second generated publication model
- lets ASP.NET Core hosts suppress those shorthand candidates through `RestApi:Suppressions`
  without taking away module ownership, while explicit `MapGet/MapPost/...` routes and manual
  module-owned endpoints remain authoritative
- lets ASP.NET Core hosts move shorthand candidates to another effective API major version,
  HTTP method, bounded published route-group prefix, constrained relative route pattern, or
  explicit binding plan through
  `RestApi:Overrides` without taking away module ownership; the suppression/override surfaces can
  both target exact `CandidateIds`, exact `Behaviors`, subtree `BehaviorIdPrefixes`, and refine
  `Modules` targeting with `ApiVersionMajors`, `Methods`, `RelativePatterns`,
  `RouteGroupPrefixes`, `OpenApiDocumentNames`, `TagNames`, `BindingFallbackModes`, and exact
  original explicit `TargetBindings`, those selector refiners match the original shorthand
  candidate shape before override actions are applied, and the override surface itself now supports
  `ApiVersionMajor`,
  `Method`, `RouteGroupPrefix`, `Pattern`, `Bindings`, host-governed preserved query fallback
  through `PreserveImplicitQueryFallback`, and typed `BindingMode`, keeps the
  `/v{major}` route segment, OpenAPI document name, endpoint method, effective route, and
  effective binding plan aligned with the same projection truth, treats `BindingMode` as a wire-name-only config surface with `replace-explicit` or `merge-explicit`, preserves `bindingMode = unspecified` only for `ClearBindings` rules that omitted an explicit mode, uses stable `route` / `query` / `header` / `body` wire names for `Bindings` and `TargetBindings` sources, and defaults `Bindings` to full
  explicit-plan replacement but also allows `BindingMode = merge-explicit` to patch only the changed
  explicit bindings by property name while still letting unbound route placeholders and remaining
  request-body fields fill object properties deterministically, now allows placeholder renames when the
  effective explicit route-binding plan covers the renamed placeholder set exactly, now also allows
  placeholder removals when the original projection already exposes explicit route-binding coverage
  for the original placeholder set and the effective explicit binding plan keeps every affected
  original route-bound property explicitly bound, and now also allows placeholder additions when
  the effective explicit route-binding plan covers the full final placeholder set and every newly
  route-bound property was either already explicitly bound in the original projection, for
  `POST`/`PUT`/`PATCH` already part of the original deterministic remaining-body fallback surface,
  or for shorthand candidates with no explicit binding plan already part of the original implicit
  query-fallback surface, or for explicit shorthand candidates whose remaining source query surface
  is intentionally preserved either by the authored profile or by a winning
  `PreserveImplicitQueryFallback` host override; it still rejects broader implicit-property
  promotion beyond that bounded
  query-fallback slice, fails fast when the effective method-plus-binding plan is invalid or when a
  rename, removal, or addition would rely on inference, drop explicit binding coverage, or promote
  another implicit property into the public route, and still leaves explicit `MapGet/MapPost/...`
  routes, manual module-owned endpoints, and shorthand groups with explicit `.ApiVersion(...)`
  authoritative for version selection; when the source shorthand had no explicit binding plan,
  partial explicit overrides now also preserve the remaining implicit query-fallback surface and
  publish that runtime truth through typed `BindingFallbackMode` values backed by
  `RestEndpointBindingFallbackMode`, while
  `RestEndpointBindingFallbackModeExtensions.GetWireName()` and `TryParseWireName(...)` now give
  the canonical compatibility bridge for metadata/JSON wire names, and additive
  `metadata.bindingFallbackMode = preserve-source-implicit-fallback` plus
  `metadata.bindingFallbackMode = preserve-remaining-body-fallback` remain compatibility-only
  metadata; when more than one suppression or override rule matches the same shorthand
  candidate, `/engine/rest-endpoint-candidates` now keeps the full specificity-ordered match trace
  visible through `MatchedSuppressionIds` and `MatchedOverrideIds` before one rule wins
- treats `behaviors.Internal<TBehavior>()` as the explicit internal-only or custom/manual-route path
- validates that a module cannot map another module's explicitly owned behavior through the REST helper layer
- keeps route shape in the ASP.NET Core adapter layer while behavior attributes remain host-agnostic
- dispatches through `BehaviorDispatcher` and `DefaultBehaviorContext`
- keeps the implicit route/query/body merge baseline for DSL-authored routes without explicit
  profile bindings, while profile-driven explicit bindings switch to descriptor-aware source
  resolution over `BehaviorRestInputContractDescriptor` metadata with deterministic route/body
  fallback
- maps manual and profile-backed REST endpoints from generated or explicitly registered
  `BehaviorContractDescriptor` metadata, so route contracts do not need to infer
  `IAppBehavior<TInput,TOutput>` or structured result wrappers from runtime types
- validates explicit binding metadata at build time and re-checks the same route-placeholder truth
  plus preserved implicit-query fallback requirements when generated or explicitly registered
  descriptors are consumed so low-ceremony profile authoring stays deterministic
- derives the default generated-selection prefix from the route-group path by trimming slashes and
  replacing `/` separators with `.`, while still allowing an explicit behavior-id prefix override
- uses the module display name for OpenAPI tags
- lets the module override the published tag name and tag description through `.WithTagName(...)` and `.WithTagDescription(...)`
- defaults the tag description from the module XML `<summary>` plus `<remarks>` when XML docs exist, falling back to `ModuleDescriptor.Description`
- defaults newly mapped endpoints to the owning module descriptor major version when one is available, so a module declared as `1.0.0` automatically joins the `v1` document and gets a `/v1` route prefix without extra code
- keeps `.ApiVersion(major)` as the explicit override when the public API version should differ from the module package major
- lets profile-declared candidate API versions seed the group only when `.ApiVersion(...)` was not
  set explicitly, and fails fast when profiled behaviors in the same group disagree on that
  candidate version
- keeps that explicit group `.ApiVersion(...)` authoritative for host-level version rewrites even
  when an ASP.NET Core host later applies `RestApi:Overrides`, while shorthand method, bounded
  route-group-prefix, and constrained pattern overrides can still apply
- keeps profile-driven explicit binding plans visible through
  `RestEndpointRuntimeDescriptor.BindingDescriptors` and the matching `bindingDescriptors` JSON
  field on `/engine/rest-endpoints` and `snapshot.RestEndpoints`
- prefers source-generated `GetRestProfiles()` plus `GetRestProfileBehaviorTypes()` hints for
  generated shorthand; generated-profile group mapping now fails fast when the owning module
  assembly does not expose registry hints instead of scanning for attributed behavior types
- lets the inline helper keep generated-profile discovery truthful by using the supplied marker
  type as the source assembly for `MapGeneratedProfiles(...)`
- prefixes the mapped REST route group with `/v{major}` for the resolved API major version, so ASP.NET Core hosts expose routes such as `/api/v1/showcase/cart/{cartId}`
- uses the resolved API major version as the operation-name version segment, falling back to the owning module descriptor major version
- flows XML comments from the module and behavior assemblies into ASP.NET Core OpenAPI metadata when XML docs are available
- maps behavior `<summary>` to the operation header and behavior `<remarks>` to the operation description so Scalar/OpenAPI content stays non-duplicated
- keeps runtime publication on the same module-owned route with `sourceKind = module-dsl`, while
  `/engine/rest-endpoints` now distinguishes the shorthand path through first-class
  `RestEndpointRuntimeDescriptor.AuthoringStyle = behavior-module-profile` or
  `behavior-module-generated`, exposes first-class `RestEndpointRuntimeDescriptor.RouteGroupPrefix`
  plus `RelativePattern` for the resolved published group boundary, exposes first-class nullable
  `RestEndpointRuntimeDescriptor.BehaviorType` for the concrete behavior implementation identity,
  exposes first-class nullable `RestEndpointRuntimeDescriptor.SourceId` for published source
  identity, keeps additive `metadata.authoringStyle`, `metadata.routeGroupPrefix`,
  `metadata.relativePattern`, `metadata.behaviorType`, and `metadata.sourceId` only for
  compatibility, and published behavior-backed endpoints now also expose
  `RestEndpointRuntimeDescriptor.CandidateId` so operators can jump directly from the final
  published endpoint to `/engine/rest-endpoint-candidates/{candidateId}`
- when the same behavior is mapped through both explicit module DSL and shorthand publication, the
  explicit DSL route now wins by default while the lower-precedence shorthand candidate remains
  visible through `/engine/rest-endpoint-candidates`, `snapshot.RestEndpointCandidates`,
  `/engine/rest-endpoint-publication-groups`, and `snapshot.RestEndpointPublicationGroups`; those
  grouped publication answers now also expose `AuthoringStyleSummaries` plus `AuthoringPolicy` so
  module authors can see which source style published, which lower-precedence styles remained
  visible only as suppressed candidates, whether the behavior boundary is still on the implicit
  default single-winner policy or an explicit `RestApi:AuthoringPolicies:{behaviorId}` contract,
  and which candidates stayed outside host governance through grouped
  `HostGovernanceEligibleCandidateIds`, `HostGovernanceIneligibleCandidateIds`,
  `SkippedSuppressionIds`, and `SkippedOverrideIds`.
- when the same behavior is mapped through both `MapProfile<TBehavior>()` and
  generated shorthand through `MapGeneratedProfiles(...)` or `MapGeneratedProfileGroups(...)`, the
  explicit per-behavior `MapProfile<TBehavior>()` route wins by default while the generated
  candidate remains visible through the same candidate and grouped publication catalogs
- keeps `MapAdditionalEndpoints(...)` as the advanced escape hatch for manual Minimal API work that
  falls outside the default behavior REST DSL; custom endpoints should still declare ownership first
  through `behaviors.Internal<TBehavior>()`, and those manual module-owned routes now still join the
  shared `/engine/rest-endpoints` runtime catalog plus duplicate-route validation baseline

When a host wants to suppress shorthand publication without removing the module-owned route group,
use `RestApi:Suppressions`. That host-level governance surface can target exact `CandidateIds`,
exact `Behaviors`, subtree `BehaviorIdPrefixes`, `Modules`, optional `HostGovernanceScopes`, and
optional `AuthoringStyles`, can refine that target further with
`ApiVersionMajors`, `Methods`, `RelativePatterns`, `RouteGroupPrefixes`,
`OpenApiDocumentNames`, `TagNames`, `EndpointNames`, `BindingFallbackModes`, exact original
explicit `TargetBindings`, and additive `HostGovernanceScopes`, matches those selector refiners
against the original shorthand candidate shape before override actions are applied, defaults to
both shorthand styles when `AuthoringStyles` is omitted, fails fast only when all of
`CandidateIds`, `Behaviors`, `BehaviorIdPrefixes`, `Modules`, and `HostGovernanceScopes` are
missing, prefers the more specific matching rule deterministically by populated target dimensions
first, then by exact behavior-targeted scope, narrower behavior-subtree scope, narrower
authoring-style scope, fewer total selector values, and stable rule id ordering, and intentionally
suppresses only descriptor-backed shorthand
candidates rather than rewriting explicit module DSL/manual routes. When more than one suppression
rule matches, the suppressed candidate keeps the full ordered match set visible through
`MatchedSuppressionIds` while `SuppressedBySuppressionId` keeps identifying the winning rule.
`TargetBindings` matches the full original explicit descriptor set by property/source/name
equivalence, and `HostGovernanceScopes` matches the route group's preserved
`OriginalProjection.HostGovernanceScope`, so hosts can distinguish a route-only candidate from a
richer explicitly bound or explicitly scoped sibling even when later overrides rewrite the
published route.

If you want an explicit module-DSL route group to participate in that same host-governance
boundary, call `AllowHostGovernance()` on the group before mapping endpoints. That keeps explicit
DSL authoritative by default while making the route group's original projection publish
`AllowsHostGovernance = true` when you intentionally opt in. Host rules still do not affect that
group unless they also explicitly target `AuthoringStyles = [behavior-module-dsl]`; omitting
`AuthoringStyles` continues to mean shorthand-only targeting.
If you want that explicit route group to publish additive selector truth without entering host
governance yet, call `WithHostGovernanceScope("orders-read")` on the group. That stamps
`OriginalProjection.HostGovernanceScope` for runtime inspection and current host-rule targeting,
but still leaves the explicit group authoritative until you also call `AllowHostGovernance()`.
When you intentionally leave the group authoritative, runtime candidates and published behavior
endpoints now surface ordered `SkippedSuppressionIds` and `SkippedOverrideIds` for any host rules
that targeted the route but were skipped because the group never entered the governable surface.
The grouped publication answer now also surfaces that same story per behavior and per authoring
style through `HostGovernanceEligibleCandidateIds`, `HostGovernanceIneligibleCandidateIds`,
`SkippedSuppressionIds`, `SkippedOverrideIds`, `SkippedSuppressionSummaries`, and
`SkippedOverrideSummaries`, so module authors do not need to drill into each candidate to confirm
that explicit ownership stayed authoritative or which ineligible candidates each skipped host rule
targeted.

When a host wants to declare grouped authoring-policy intent for one behavior boundary, use
`RestApi:AuthoringPolicies:{behaviorId}`. That boundary-level contract now actively honors
`AllowMultiplePublishedCandidates = true` during shorthand candidate resolution, so
lower-precedence unsuppressed shorthand candidates can remain published together when the final
public route answers stay distinct. When those co-published candidates would otherwise reuse the
same effective endpoint name, Cephalon now disambiguates that name deterministically while
preserving `OriginalEndpointName` as source shorthand lineage. `PreferredAuthoringStyle`,
`AllowedAuthoringStyles`, and `DisallowedAuthoringStyles` now also enforce shorthand publication
policy for profile and generated shorthand candidates while leaving explicit module DSL
publication authoritative. Operator-facing candidate payloads now also keep
`SuppressedByAuthoringPolicyKind` on the same canonical
`disallowed-authoring-style`, `not-allowed-authoring-style`, and
`preferred-authoring-style-selected` wire names across `/engine/rest-endpoint-candidates`,
`/engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates`, so host
governance troubleshooting does not depend on raw enum serialization details.
That same grouped publication answer now also carries `AuthoringPolicySuppressionSummaries` at the
behavior-group level and inside each `AuthoringStyleSummaries` entry, so module authors can see
which shorthand candidates were filtered by a disallowed style, a not-allowed style, or a
preferred-style winner directly from `/engine/rest-endpoint-publication-groups` and
`snapshot.RestEndpointPublicationGroups` without reconstructing the breakdown from candidate-level
payloads.
That same grouped runtime surface now also carries `GovernanceSuppressionSummaries` and
`GovernanceOverrideSummaries` at both the behavior-group level and inside each
`AuthoringStyleSummaries` entry, so module authors can see which host rules matched, which
candidate ids they actually suppressed, and which override winners stayed selected-only versus
applied without treating those grouped summaries as additional authoring inputs. Those same grouped
summaries now also keep typed selection-basis buckets plus grouped declared-versus-effective
override-action buckets visible, so module authors can read why a winning host rule beat the
runner-up and which override dimensions only stayed declared versus materially applied without
dropping back to candidate-level payloads.

When a host wants to keep shorthand publication but retarget selected shorthand endpoints to a
different effective API major version, HTTP method, bounded published route-group prefix,
constrained relative route pattern, endpoint metadata, or explicit binding plan, use
`RestApi:Overrides`. That
host-level governance surface targets the same
descriptor-backed shorthand candidates, can target them through `Behaviors`, `Modules`, or a
deliberate primary `HostGovernanceScopes` selector, and can refine that target further with exact
`CandidateIds`, `ApiVersionMajors`, `Methods`, `RelativePatterns`,
`RouteGroupPrefixes`, `OpenApiDocumentNames`, `TagNames`, `EndpointNames`,
`BindingFallbackModes`, exact original explicit `TargetBindings`, and additive
`HostGovernanceScopes`, matches those selector refiners against the original shorthand candidate
shape before override actions are applied, now supports a positive `ApiVersionMajor`, a supported
HTTP `Method`, a valid bounded `RouteGroupPrefix`, a valid relative `Pattern`, endpoint-metadata
set actions `EndpointName`, `Summary`, and `Description`, endpoint-metadata clear actions
`ClearEndpointName`, `ClearSummary`, and `ClearDescription`, and/or explicit `Bindings`; the
winning rule is recorded through `AppliedOverrideId` in
`/engine/rest-endpoint-candidates` when it materially changes the effective answer, while no-op
winning rules still surface through `SelectedOverrideId` plus `OverrideSelectionBasis`,
while the runtime keeps the original shorthand source
shape visible there through `OriginalProjection` while `ProjectedEndpoint` carries the final
effective mapped answer, now also keeps original shorthand endpoint name plus summary/description
visible on that effective mapped answer through `ProjectedEndpoint.OriginalEndpointName`,
`ProjectedEndpoint.OriginalSummary`, and `ProjectedEndpoint.OriginalDescription`, keeps the grouped
behavior-level publication story visible through `/engine/rest-endpoint-publication-groups`,
including the per-style `AuthoringStyleSummaries` summary derived from the same grouped candidate
truth, now also keeps every matching override rule visible through `MatchedOverrideIds` in
specificity order before one winner is selected, and intentionally leaves explicit module
DSL/manual routes plus shorthand groups with explicit `.ApiVersion(...)` authoritative for version
selection by default. If a module deliberately opts an explicit DSL group into host governance
through `AllowHostGovernance()`, version and metadata overrides can govern that explicit route too,
but only when the matching host rule explicitly targets authoring style `behavior-module-dsl`.
If ASP.NET Core materialization proves that a matched shorthand override rule did not actually
change the published endpoint metadata or capability answer, the runtime keeps
`MatchedOverrideIds` visible, leaves `AppliedOverrideId = null`, and now still surfaces the
winning rule through `SelectedOverrideId` plus `OverrideSelectionBasis` on both the published
candidate and final `/engine/rest-endpoints` answer. When more than one suppression rule matches a
candidate and one wins, the same candidate answer also exposes `SuppressionSelectionBasis`.
The inverse rule-centric view now also stays available through `/engine/rest-endpoint-suppressions`
and `/engine/rest-endpoint-overrides`, where each rule surfaces its own matched/suppressed/selected/
applied/skipped candidate buckets plus the relevant selection-basis and override-action unions,
so module authors can inspect host governance from the rule side without reconstructing that answer
from the full candidate set.
When `Bindings` are supplied, the override
uses default `replace-explicit` mode unless `BindingMode = merge-explicit` is set explicitly. Replace
mode swaps the shorthand candidate's full explicit binding plan, while merge mode upserts only the
named explicit bindings by property name and keeps untouched explicit bindings intact; both modes
still leave unbound route placeholders and remaining request-body fields available for deterministic
fallback. Placeholder renames can now apply when the effective explicit route-binding plan covers
the renamed placeholder set exactly; placeholder removals can now also apply when the source
projection already exposed explicit route-binding coverage for the original placeholder set and the
effective explicit binding plan keeps every affected original route-bound property explicitly bound;
placeholder additions can now also apply when the effective explicit route-binding plan covers the
full final placeholder set and every newly route-bound property was either already explicitly bound
in the source projection, for `POST`/`PUT`/`PATCH` already part of the original deterministic
remaining-body fallback surface, or for shorthand candidates with no explicit binding plan already
part of the original implicit query-fallback surface. Explicit-binding shorthand candidates still
stay on the stricter explicit-binding path unless the authored profile or a winning
`PreserveImplicitQueryFallback` host override intentionally keeps that remaining source query
surface available. `RouteGroupPrefix` can now move the published shorthand
group to a different path such as `/api/v1/showcase/cart-admin`, but only when that group stays
beneath the active REST root, contains no placeholders, and does not silently change effective
API-version truth; when only some candidates in one authored shorthand group are remapped,
ASP.NET Core now splits materialization by the effective group prefix so actual HTTP routes match
`OriginalProjection`/`ProjectedEndpoint` runtime truth, published shorthand endpoints also expose
`OriginalEndpointName`, `OriginalSummary`, and `OriginalDescription` directly so the final runtime
answer keeps source-versus-effective endpoint metadata visible beside route/version truth even when
a host-level override intentionally clears the effective endpoint metadata, and `TargetBindings`
lets hosts pick the original exact explicit binding plan as a selector instead of an override
action so governance can distinguish authored binding shapes without rewriting them. A
single override rule cannot both set and clear the same endpoint-metadata field. Broader
implicit-property promotion beyond
that constrained body-fallback-plus-bounded-query-fallback path still fails fast; invalid effective
method-plus-binding combinations also fail fast during endpoint materialization; and explicit module
DSL/manual routes still stay authoritative.

Example:

```json
{
  "RestApi": {
    "Suppressions": {
      "hide-generated-cart": {
        "Modules": [ "showcase.cart" ],
        "AuthoringStyles": [ "behavior-module-generated" ]
      }
    }
  }
}
```

When a host needs more than the default `v1` document, prefer `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion`. Behaviors and modules still declare candidate document versions through `.ApiVersion(...)` or module-major defaults, but the host treats `EnabledVersions` as the allow-list for what actually gets published. For example, if modules carry `v1`, `v2`, and `v3` endpoint metadata while the host enables only `[2, 3]`, Cephalon registers only `/openapi/v2.json` plus `/openapi/v3.json`, redirects `/scalar` to the resolved default enabled document such as `/scalar/v3`, and injects only those enabled documents into Scalar's version selector. `/scalar/` still remains available for multi-document selection, and Cephalon normalizes hash-based Scalar selections such as `/scalar/#v2/` back into pinned versioned links. Hosts can also move the docs and REST entry points with `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and the canonical `ApiRoutes:Prefixes:*` settings. Legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` settings remain available when a host deliberately wants custom named documents instead of `v{major}` API-version documents, and those settings follow the same published-document allow-list semantics.

That published-document allow-list remains separate from shorthand candidate version selection. In
other words, `RestApi:Overrides:*:ApiVersionMajor` can move a shorthand endpoint from `v1` to
`v2`, `RestApi:Overrides:*:Method` can move the same shorthand endpoint from `GET` to `DELETE`,
`RestApi:Overrides:*:RouteGroupPrefix` can move the published shorthand group from
`/api/v1/showcase/cart` to `/api/v1/showcase/cart-admin` without changing the endpoint's relative
pattern, while `RestApi:Overrides:*:Pattern` can move it from `/{cartId}` to `/lookup/{cartId}` with the same
placeholder set, to `/lookup/{id}` when `RestApi:Overrides:*:Bindings` also makes the effective
route-binding plan explicit for `{id}`, or to `/lookup` when `Bindings` explicitly rebind the
removed route-bound value and the source projection already exposed an explicit route binding for
`{cartId}`, or to `/lookup/{cartId}/items/{quantity}` when `Bindings` explicitly promote
`Quantity` from its original explicit query/header/body binding into the route; `Bindings` can
either replace the whole explicit plan or, with `BindingMode = merge-explicit`, patch only the
affected explicit properties such as moving `Quantity` from query key `quantity` to route
placeholder `quantity`, changing `Quantity` from query key `quantity` to `qty`, or changing `Note`
from body key `note` to `memo`, while `ApiVersionMajors`, `Methods`,
`RelativePatterns`, and `RouteGroupPrefixes` let the same rule target only the original shorthand
shape it means to govern. Keep `RouteGroupPrefix` bounded under the active REST root with no
placeholders and no silent API-version drift, and remember that the host still decides whether `v2`
is actually
published through
`OpenApi:EnabledVersions` or legacy document config. Keep that distinction in mind when a module
can declare or inherit more candidate versions than one host chooses to publish.

That published-document allow-list is separate from the generic behavior adapter route segment. JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and WebSocket routes keep using `ApiRoutes:DefaultBehaviorDocumentName` or the raw configured `OpenApi:DefaultVersion`, because those adapter endpoints are not part of the published REST OpenAPI surface. If a host wants those generic adapter routes pinned to a different segment than the docs default, set `ApiRoutes:DefaultBehaviorDocumentName` explicitly.

This helper surface is REST-specific. The generic route-shaped behavior transports already share the
`BehaviorApiSurfaceDescriptor` contract for JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and
WebSocket routes. Module-owned REST helpers are the right choice when the module needs Minimal API
method selection, concrete REST templates, and OpenAPI metadata that read like a normal application
API, while `BehaviorModuleBase` remains the host-agnostic choice for internal or non-REST behavior
ownership.

## Workflow and orchestration descriptors

Packages that need to describe an execution flow can implement `IExecutionGraphContributor` and publish one or more `ExecutionGraphDescriptor` entries.
Packages that need to describe operator-facing host-managed background work can also implement `IHostedExecutionContributor` and publish one or more `HostedExecutionDescriptor` entries.
Packages that extend `Cephalon.Agentics` can keep agent tools grounded in those same runtime contracts by declaring `capabilityKeys`, `executionGraphId`, or `hostedExecutionId` on `AgentToolDescriptor` instead of inventing a separate AI-specific orchestration registry.
Packages that want a Cephalon-managed agent-tool loop can also register `IAgentToolExecutor` for the tool id, add `IAgentToolExecutionPolicy` for approval or denial decisions, and attach `IAgentToolExecutionObserver` for audit or telemetry without taking over the dispatcher itself.

Current baseline behavior:

- execution graphs are discovered only from active modules, so they stay additive to the existing module model
- graph nodes can point back to module ids and capability keys instead of inventing a parallel ownership model
- `/engine/execution-graphs` exposes the standalone catalog, and `/engine/snapshot` carries the same graph descriptors alongside status, diagnostics, and lifecycle data
- `/engine/runtime-story` now carries the operator-facing lifecycle state for each execution graph, including load, activate, and deactivate timestamps
- `/engine/hosted-executions` exposes the hosted/background catalog, `/engine/snapshot` carries the same hosted descriptors, and `/engine/runtime-story` now carries hosted-execution load, activate, and deactivate timestamps
- hosted executions can link back to one execution graph through `executionGraphId`, but they stay descriptive and operator-facing instead of introducing a separate Cephalon runner abstraction
- hosted executions can also declare `metadata.eventSubscriptionId` or `metadata.eventSubscriptionIds` when they own the application-managed execution path for declared event subscriptions, which lets `Cephalon.Eventing` project truthful linkage without inventing a second runtime registry; if the hosted execution also points at an `executionGraphId`, the eventing runtime surface can surface that orchestration link too
- modules that own application-managed event handling can also inject `IEventSubscriptionRuntimeReporter` and report `started`, `succeeded`, `failed`, `retry-scheduled`, or `skipped` observations plus operator-facing metadata such as retry windows or backoff hints so `Cephalon.Eventing` can project truthful live runtime state without claiming a pack-owned bus runner
- modules or adapter packages that own application-managed publication dispatch can inject `IEventDispatchRuntimeReporter` and report `started`, `succeeded`, `failed`, `retry-scheduled`, or `skipped` observations plus operator-facing metadata such as `outboxId`, `channelId`, retry windows, or backoff hints so `Cephalon.Eventing` can project truthful dispatch runtime state without claiming a broker-owned dispatch runtime
- `Cephalon.Agentics` can now project agent-tool links to capability keys, execution graphs, and hosted executions through `/engine/technology-surfaces`, `/engine/snapshot`, and the direct `/engine/agent-tool-runs*` run-state routes
- when agentic execution is enabled, `IAgentToolDispatcher` records `started` plus terminal or policy outcomes into the abstraction-level `IAgentToolRunCatalog`, and the same surface reports `cephalon-managed`, `awaiting-executor`, or `not-configured` ownership truth per tool
- modules can query `Cephalon.Abstractions.Agentics.IAgentToolRunCatalog` when they need in-process run posture, and host adapters can call `Cephalon.Abstractions.Agentics.IAgentToolDispatcher` or expose `POST /engine/agent-tools/{toolId}/runs` when operators need to trigger a bounded managed run without referencing `Cephalon.Agentics` implementation types
- modules should still report through the dispatcher or `IAgentToolRunReporter` instead of inventing host-local run dictionaries
- invalid agent-tool references to unknown capability keys, execution graphs, or hosted executions now fail when the agentic runtime catalog is resolved
- modules that extend `Cephalon.Retrieval` can contribute `IKnowledgeCollectionContributor` descriptors plus `IKnowledgeDocumentProvider` source material; when `KnowledgeRetrieval` is selected, the pack owns the current lexical index, bounded query execution, freshness state, manual reindex command seam, and query fingerprint reporting while `/engine/knowledge-indexes`, `/engine/knowledge-indexes/{collectionId}`, `POST /engine/knowledge-indexes/{collectionId}/reindex`, and `snapshot.KnowledgeIndexes` expose the abstraction-level operator seams; provider-specific vector search, embeddings, durable indexes, distributed search, or background reindex automation remain future companion work
- the engine validates hosted-execution ids, source modules, and referenced execution graphs at build time so invalid hosted descriptors fail fast
- the engine validates graph ids, entry nodes, edges, referenced modules, and referenced capability keys at build time so invalid descriptors fail fast

## Data and authorization descriptors

Packages that need to publish a module-owned query surface can implement `IDataProductContributor` and register one or more `DataProductDescriptor` entries.
Packages that need to publish operator-facing CDC posture can implement `ICdcCaptureContributor` and register one or more `CdcCaptureDescriptor` entries linked to an owned outbox.
Packages that need to publish read-model or projection shape can implement `IProjectionContributor` and register one or more `ProjectionDescriptor` entries.
Packages that need to publish durable processed-message or idempotency-store shape can implement `IInboxContributor` and register one or more `InboxDescriptor` entries.
Packages that need to publish durable outbound message staging shape can implement `IOutboxContributor` and register one or more `OutboxDescriptor` entries.
Packages that need to publish operator-facing authorization choices can implement `IAuthorizationPolicyContributor` and register one or more `AuthorizationPolicyDescriptor` entries.

Current baseline behavior:

- `/engine/data-products` exposes the merged data product catalog, and `/engine/snapshot` carries the same data product descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/cdc-captures` exposes the merged CDC capture catalog, and `/engine/snapshot` carries the same CDC descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/projections` exposes the merged projection catalog, and `/engine/snapshot` carries the same projection descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/inboxes` exposes the merged inbox catalog, and `/engine/snapshot` carries the same inbox descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/outboxes` exposes the merged outbox catalog, and `/engine/snapshot` carries the same outbox descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/authorization-policies` exposes the merged authorization-policy catalog, and `/engine/snapshot` carries the same policy descriptors in the broader runtime answer
- data product descriptors stay grounded in module ownership through `sourceModuleId`, domain ids, contract ids, mode, and operator-facing metadata such as freshness or classification
- CDC capture descriptors stay grounded in module ownership through `sourceModuleId`, `provider`, `sourceId`, `outboxId`, `mode`, `eventFormat`, resource ids, and operator-facing metadata such as publication mode or freshness
- projection descriptors stay grounded in module ownership through `sourceModuleId`, target store ids, and optional source contract metadata
- inbox descriptors stay grounded in module ownership through `sourceModuleId`, provider, mode, optional channel ids, and operator-facing metadata such as idempotency scope
- outbox descriptors stay grounded in module ownership through `sourceModuleId`, provider, mode, optional channel ids, and operator-facing metadata such as dispatch ownership
- authorization-policy descriptors stay host-agnostic and can publish supported `RBAC`, `ABAC`, and `Policy` modes without leaking ASP.NET Core or identity-provider types into `Cephalon.Abstractions`
- the CDC baseline is intentionally descriptor-first: provider-specific WAL/change-stream execution still belongs to the owning module or a future companion pack, while the engine owns the catalog, outbox linkage, and validation; when a provider implementation must defer durable checkpoint advancement until outbox staging succeeds, the active `ICdcCapture` can also implement `ICdcCaptureAcknowledger` so the shared runtime delivers one post-stage `CdcCaptureExecutionAcknowledgement`
- invalid CDC capture source-module ownership or missing outbox references fail at build time instead of leaking broken operator metadata

## Package manifest contract

`cephalon.package.json` is now the recommended place to describe both where the package assembly lives and what runtime it expects.

Baseline example:

```json
{
  "id": "operations",
  "version": "1.0.0",
  "assembly": "Cephalon.ReferenceModule.Operations.dll",
  "publisher": {
    "id": "cephalon-labs",
    "displayName": "Cephalon Labs",
    "website": "https://example.invalid/cephalon-labs"
  },
  "distribution": {
    "channel": "stable",
    "manifestUri": "https://packages.example.invalid/cephalon/operations/1.0.0/cephalon.package.json",
    "packageUri": "https://packages.example.invalid/cephalon/operations/1.0.0/Cephalon.ReferenceModule.Operations.zip"
  },
  "provenance": {
    "sourceRepository": "https://github.com/Cephalon-Labs/CephalonEngine",
    "sourceRevision": "refs/tags/operations-v1.0.0",
    "buildUri": "https://builds.example.invalid/cephalon/operations/1.0.0",
    "statementUri": "https://packages.example.invalid/cephalon/operations/1.0.0/provenance.json"
  },
  "signature": {
    "type": "detached-signature",
    "signer": "Cephalon Labs Build",
    "keyId": "cephalon-labs-build",
    "fingerprint": "sha256:cephalon-labs-reference-operations",
    "algorithm": "RSA-SHA256",
    "value": "<base64-detached-signature>"
  },
  "compatibility": {
    "minimumEngineVersion": "1.0.0",
    "maximumEngineVersion": "2.0.0",
    "supportedTargetFrameworks": ["net10.0"]
  },
  "dependencies": [
    {
      "id": "shared-foundation",
      "minimumVersion": "1.0.0",
      "maximumVersion": "2.0.0"
    }
  ],
  "integrity": {
    "sha256": "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
  }
}
```

For a single signer, use the legacy `signature` object shown above. When a package needs more than one signer or attestation, it can declare `signatures` instead. The engine supports both shapes and exposes per-signer verification results through runtime introspection.

Current behavior:

- `id` should stay stable across releases so trust policy and operators have a predictable handle
- `version` is surfaced through `/engine/packages` and should match the distributed package version
- `compatibility.minimumEngineVersion` blocks older engine versions from loading the package
- `compatibility.maximumEngineVersion` is optional but useful when a package intentionally caps support
- `compatibility.supportedTargetFrameworks` blocks mismatched runtime target frameworks
- `dependencies` is optional, but when declared each entry should reference the stable `id` of another package and can add `minimumVersion` / `maximumVersion` bounds for versioned package dependencies
- version-bounded dependency entries require the referenced package to declare its own `version`
- `publisher.id` should stay stable across releases if operators or trust policy use publisher-level allow-lists
- `distribution.channel` should stay stable enough to distinguish release lanes such as `stable`, `preview`, or `internal`
- `distribution.manifestUri` and `distribution.packageUri` should point to the externally reachable locations operators actually use once the package leaves the repo
- `provenance.sourceRepository` and `provenance.sourceRevision` should line up with the release source you intend operators to audit
- `provenance.buildUri` and `provenance.statementUri` are optional, but they are the right place to point operators to CI evidence or an attestation document
- `signature.keyId` or `signatures[].keyId` should stay stable across releases if hosts map trusted public keys or trusted signing certificates by signing identity
- `signature.fingerprint` or `signatures[].fingerprint` identifies the signing key and is surfaced through diagnostics and trust snapshots
- `signature.value` or `signatures[].value` is optional until a host requires cryptographic verification, but when present it should be a detached signature over the resolved assembly SHA-256 hash
- hosts that want certificate-backed trust can pair `Engine:Trust:TrustedSignatureCertificates` with `Engine:Trust:TrustedSignatureCertificateAuthorities` so package signatures verify against an explicit signing certificate chain instead of only a raw public-key mapping
- `integrity.sha256` is optional, but when present the resolved assembly must match it exactly

For scaffolded modules and `dotnet new` starters, Cephalon now emits this manifest automatically with the current package version and target framework baseline.

## External distribution guidance

When a package is meant to leave the repository, keep the distribution and provenance hints in `cephalon.package.json` truthful enough for operators to answer three questions quickly:

- where should this package be fetched from now
- which source revision produced it
- where is the build or provenance evidence that backs this artifact

Recommended baseline:

- set `distribution.channel` to the release lane you actually publish, such as `stable`, `preview`, or `internal`
- set `distribution.manifestUri` to the externally reachable manifest location when you publish manifests alongside package artifacts
- set `distribution.packageUri` to the archive, NuGet-like feed entry, or artifact page operators use to fetch the package
- set `provenance.sourceRepository` to the canonical source repository URI
- set `provenance.sourceRevision` to a tag, commit SHA, or release ref that unambiguously identifies the shipped source
- set `provenance.buildUri` and `provenance.statementUri` when you have CI/build evidence or a provenance/attestation document to point at

## Compatibility checklist

Keep authored packages aligned with the broader Cephalon compatibility contract:

- `version` and `compatibility.minimumEngineVersion` should reflect the Cephalon package version you intend to support
- `compatibility.supportedTargetFrameworks` should reflect the actual target framework of the compiled module assembly
- if you intentionally cap support, set `compatibility.maximumEngineVersion` explicitly instead of relying on undocumented assumptions
- when you change package version or target framework expectations, update the project file, `cephalon.package.json`, packaging docs, and any starter/template copies together
- use `docs/compatibility.md` as the repository-wide matrix for what else must stay aligned across scaffolding, CLI, templates, and docs

## Staging a published package

Published `.nupkg` files keep their manifest under package content and their runtime assembly under `lib/<tfm>`, so stage them into a loadable directory before pointing host discovery at them:

```powershell
cephalon package stage `
  --package ./artifacts/reference-packages/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

That staged directory becomes the path you feed into `Engine:Discovery:PackageDirectories` or `Engine:Discovery:Packages:ManifestPath`.

For the full publish -> trust -> load -> inspect walkthrough, see [External package lifecycle](external-package-lifecycle.md).

## Loading a package

Package-path discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "Id": "operations",
          "Path": "plugins/Cephalon.ReferenceModule.Operations.dll"
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Package-manifest discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "ManifestPath": "plugins/reference-operations/cephalon.package.json"
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Package-directory discovery:

```json
{
  "Engine": {
    "Discovery": {
      "PackageDirectories": [
        {
          "Path": "plugins",
          "IncludeSubdirectories": true
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Assembly-name discovery is still available when the module assembly is already part of the app load context:

```json
{
  "Engine": {
    "Discovery": {
      "Assemblies": [ "Cephalon.ReferenceModule.Operations" ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Code-driven package path:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddPackageAssembly("plugins/Cephalon.ReferenceModule.Operations.dll", id: "operations");
    engine.AddPackageManifest("plugins/reference-operations/cephalon.package.json");
    engine.AddPackageDirectory("plugins");
});
```

Manual module registration path:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddModule(new OperationsModule());
});
```

`/engine/packages` exposes the package-loading snapshot the runtime resolved, including the package `kind`, resolved assembly `path`, original `sourcePath`, declared `version`, compatibility fields, declared package `dependencies`, external `distribution` metadata, `provenance` metadata, publisher/signature provenance metadata, the top-level signature summary fields kept for backward compatibility, the per-signer `signatures` collection, per-signer `verificationSource` and `certificateThumbprint` details when certificate-backed trust is used, cryptographic verification status, computed `checksumSha256`, and the current `trustReason`. `/engine/modules` continues to show the active module set after policy and ordering have been applied. `/engine/technology-catalog` shows the technology profiles available after built-in, package, and project contributions have been merged. `/engine/technology-surfaces` shows the active runtime surfaces exposed by installed technology packs after host options and module contributors have both been applied, while `/engine/technology-surfaces/{technologyId}` narrows that view to a single selected technology profile. `/engine/cells` now shows the active cell-boundary catalog when modules or hosts contribute explicit cell topology, `/engine/cell-routes` now shows the active governed cell-route catalog over that same shared phase-13 runtime, `/engine/cell-health-isolations` now shows the active cell health-isolation catalog over that same shared runtime truth, and `/engine/cell-traffic-automations` now shows the effective configuration-driven automation policy derived from those same route plus health-isolation answers. In code, the same merged technology surface set is available through `ITechnologyRuntimeCatalog`, the cell-boundary catalog is available through `ICellBoundaryCatalog`, the cell-route catalog is available through `ICellRouteCatalog`, the cell health-isolation catalog is available through `ICellHealthIsolationCatalog`, the automation catalog is available through `ICellTrafficAutomationRuntimeCatalog`, and the broader operator-facing runtime snapshot is available through `IRuntimeIntrospectionSnapshotProvider` or `GET /engine/snapshot`.

## What the reference package demonstrates

`Cephalon.ReferenceModule.Operations` shows:

- package-owned service registration
- capability registration visible in `/engine/capabilities`
- lifecycle-driven state transitions
- package localization merged into the runtime catalog
- package-contributed technology profiles through `ITechnologyContributor`
- technology-aware service and capability activation through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`
- REST endpoint contribution through `IRestModule`
- a distributable `cephalon.package.json` manifest copied to the package output

That makes it the baseline package to copy when authoring a new module package for Cephalon. The scaffold generator and `dotnet new` starters now emit the same manifest convention automatically.

## Trust and capability policy

Module packages should assume capability access may be governed by `Engine:Trust`.

Current baseline:

- package-loaded assemblies may be required to come from manifest-driven discovery through `Engine:PackagePolicy`
- package manifests may be required to declare `version`, compatibility fields, publisher ids, signer fingerprints, signature key ids, signature values, cryptographic signature verification, or `integrity.sha256` through `Engine:PackagePolicy`
- package-loaded assemblies can be blocked unless the package is trusted
- package trust can come from package id, assembly name, publisher id, signer fingerprint, checksum allow-list, or any declared signature that verifies successfully under the active trust policy
- capability metadata can be allowed, trusted-only, or denied
- REST endpoints can opt into request-time enforcement with `RequireCapability("capability.key")`

For independently distributed packages, trust can combine signing keys and checksums:

```json
{
  "Engine": {
    "Trust": {
      "TrustedPublishers": [ "cephalon-labs" ],
      "TrustedSignerFingerprints": [
        "sha256:cephalon-labs-reference-operations"
      ],
      "TrustedSignaturePublicKeys": {
        "cephalon-labs-build": "keys/cephalon-labs-build.public.pem"
      },
      "TrustedSignatureCertificates": {
        "cephalon-labs-signing-cert": "keys/cephalon-labs-signing-cert.pem"
      },
      "TrustedSignatureCertificateAuthorities": [
        "keys/cephalon-labs-root.pem"
      },
      "AllowedPackageChecksums": {
        "operations": [
          "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
        ]
      }
    }
  }
}
```

That means package authors should give capabilities stable keys and use those keys consistently when binding runtime policies.

## Companion packs vs modules

If a package is mainly about a future-tech workload and provides reusable runtime primitives across many apps, prefer the technology-pack pattern documented in `docs/technology-packs.md`.

Use a module package when the package primarily owns domain behavior.
Use a technology pack when the package primarily owns reusable workload services or capability activation for a technology profile.
If a domain module only needs to add descriptors into an existing technology pack, prefer the pack's contributor services instead of creating a new companion package.

