# Cephalon.Behaviors

`Cephalon.Behaviors` is the Adaptive Behavior Topology (ABT) runtime baseline for Cephalon.

## What it owns

- **BehaviorDispatcher** — O(1) `FrozenDictionary` dispatch table; zero reflection on the hot path
- **BehaviorAttributeTopologyResolver** — resolves explicit topology first, then synthesizes an
  attribute-only baseline when the pattern choice is unambiguous
- **CompatibilityMatrix** — startup-time validation of resolved topologies against
  `IBehaviorCompatibilityRule` implementations
- **BehaviorExecutionSlot** — compiled `Expression.Lambda` invoker built once at startup
- **IBehaviorCatalog / IBehaviorRegistry** — populated by `IBehaviorContributor` implementations
- **Hosting** — `IEngineBuilder.AddBehaviors(configure?)` extension + `BehaviorModule`
- **Configuration** — `Engine:Behaviors` auto-registration controls
- **Module-backed capability publication** — `BehaviorModule` publishes
  `behaviors.saga-choreography.runtime-catalog` and
  `behaviors.saga-choreography.publication-state` only when the shared choreography pattern
  services are actually active
- **Built-in compatibility rules** — startup guardrails covering saga, saga choreography,
  durable execution, process-manager, and CQRS constraints

## Key contracts (from `Cephalon.Abstractions.Behaviors`)

| Type | Description |
|------|-------------|
| `IAppBehavior<TIn, TOut>` | Single behavior interface — `HandleAsync` + optional `static virtual ConfigureTopology` |
| `IBehaviorContext` | Transport-neutral ambient API: `BehaviorId`, `CorrelationId`, `Metadata`, optional `EventStore`, and `ReplyAsync(...)` |
| `IBehaviorTopologyBuilder` | Fluent builder: `AsCqrs()`, `AsEventDriven()`, `AsSagaChoreography()`, `AsDurableExecution()`, `ViaHttpJsonRpc()`, `ViaRabbitMq()`, `RequireFeatureFlag(...)`, etc. |
| `IBehaviorModuleBuilder` | Host-agnostic builder that lets a module declare which behaviors it owns |
| `IBehaviorOwnerModule` | Module contract for explicit behavior ownership through `ConfigureBehaviors(...)` |
| `OwnedBehaviorRegistration` | Runtime composition record describing one module-owned behavior registration |
| `BehaviorApiSurfaceDescriptor` | Shared logical route surface for route-shaped generic HTTP transports; defaulted from the behavior id and overrideable through `WithApiSurface(...)` |
| `Result<T>` / `IBehaviorResult` | Preferred concise transport-neutral structured outcome for expected non-success branches without forcing HTTP or RPC envelopes into the core behavior contract |
| `BehaviorTopologyDescriptor` | Resolved per-behavior config: pattern, transports, ordered required feature flags, shared API surface, and optional owning module id |
| `BehaviorFeatureDisabledException` | Transport-neutral rejection raised when the active feature-toggle context does not satisfy a behavior-owned feature gate |
| `[AppBehavior("id")]` | Declares a class as a named behavior |
| `[BehaviorAllowedPatterns]` | Pattern allowlist; when no explicit topology exists, exactly one declared pattern also becomes the attribute-only runtime baseline |
| `[BehaviorAllowedTransports]` | Transport allowlist; when no explicit topology exists, declared transports also become the attribute-only runtime transport baseline. Public REST is module-owned and must not appear here; `http.grpc` is accepted as an alias for canonical `grpc` |
| `IBehaviorCompatibilityRule` | Author extension point for custom topology validation |

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .Register<PlaceOrderBehavior>()
        .Register<GetOrderBehavior>(b => b.AsCqrs().ViaHttpJsonRpc())
    )
);
```

## Module-owned behaviors

Cephalon now supports an explicit ownership model for behaviors:

- `BehaviorModuleBase` is the recommended base class when a module owns behaviors but does not need
  to expose a public REST surface
- `RestBehaviorModuleBase` lives in `Cephalon.Behaviors.Http` and adds
  `ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)` on top of the same ownership model
  for REST-backed modules
- one module can own both internal-only behaviors and public REST-backed behaviors without splitting
  the bounded context into multiple module classes
- engine build validates duplicate ownership so the same behavior id or type cannot be claimed by
  multiple modules silently

```csharp
public sealed class CartModule : BehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        behaviors.Add<RepriceCartBehavior>();
        behaviors.Add<CheckoutWorkflowBehavior>(topology => topology
            .AsProcessManager()
            .ViaKafka()
            .RequireFeatureFlag("checkout-workflow"));
    }
}
```

Use that contract when ownership must be explicit even if the behavior still runs through generic
HTTP transports, messaging, or background orchestration rather than a module-owned REST API.

Owned registrations now also preserve the owning module id on the resolved
`BehaviorTopologyDescriptor`, which means runtime catalogs and transport faults can report both the
behavior id and the owning module when a shared feature gate blocks execution.

## Durable execution pattern

Use `AsDurableExecution()` when a behavior should replay aggregate-like state from the shared
event-sourcing baseline instead of only checkpointing one step at a time.

- implement `IDurableExecution<TInput, TState, TOutput>` from `Cephalon.Behaviors.Patterns`
- keep stream ownership explicit through `ResolveStreamId(...)` instead of relying on transport-only
  identifiers
- return `DurableExecutionStepResult<TOutput>` so the runtime can append deterministic continuation
  events, pending timers/signals, available compensation actions, and distinguish `200`, `202`,
  and `204` outcomes truthfully
- keep `EventSourcingEnabled = true`; built-in rule `ABT-006` fails fast when a behavior declares
  `durable-execution` without the event-sourcing baseline

That baseline intentionally reuses `IBehaviorContext.EventStore` rather than inventing a second
workflow journal, which keeps HTTP, messaging, and tests aligned on the same replay contract.

The operator surface now follows the same shared runtime truth: `IDurableExecutionRuntimeCatalog`
and `IDurableExecutionRuntimeStateCatalog` publish both the static durable workflow contract and
the latest per-stream runtime posture through `/engine/durable-executions`,
`/engine/durable-executions/runtime`, the pending timer/signal filters under
`/engine/durable-executions/runtime/timers*` plus `/engine/durable-executions/runtime/signals*`,
the compensation filters under `/engine/durable-executions/runtime/compensations*`, and the
matching snapshot fields. Pending timer/signal waits and operator-facing compensation actions now
stay part of that same runtime truth instead of requiring ad-hoc host-side workflow registries.

## Feature-gated execution

Behaviors can now declare rollout gates in the shared topology itself instead of depending only on
transport-specific middleware:

- use `RequireFeatureFlag(...)` or `RequireFeatureFlags(...)` in `ConfigureTopology(...)` or module
  registration when a behavior should execute only behind one or more Cephalon feature flags
- source-generated topology descriptors preserve those literal feature ids, so compile-time and
  fluent-runtime topology paths stay aligned
- resolved `BehaviorTopologyDescriptor` entries now keep both the ordered required feature ids and
  optional `SourceModuleId` ownership metadata
- the dispatcher pipeline evaluates those requirements through `IFeatureToggle` using behavior,
  module, transport, environment, tenant, subject, and tag context and throws
  `BehaviorFeatureDisabledException` when the active runtime context is not eligible
- when one of those referenced feature flags also declares `ProviderBindings`, the same shared
  behavior gate now honors the registered `IFeatureFlagProvider` answers without moving rollout
  ownership out of the behavior topology or the Cephalon feature catalog
- REST helper endpoints map that shared rejection to `404`, JSON-RPC maps it to `-32004`, and
  messaging bindings log and skip the disabled execution with transport-appropriate acknowledgement
  semantics instead of treating the rejection as a retryable failure

## Transport-neutral outcomes

Cephalon behaviors should keep their return contract transport-neutral by default:

- return raw `TOut` when the success path is straightforward
- return `Result<TOut>` when the behavior needs to communicate an expected branch such as
  `NotFound`, `Invalid`, `Conflict`, `Forbidden`, or `NoContent` without throwing exceptions for
  normal domain flow
- use the shorter no-payload factories such as `Result.NotFound(...)` or
  `Result.Invalid(...)` when the target `Result<TOut>` return type is already
  known from the method signature
- if a synchronous implementation returns `Task.FromResult(...)`, keep that wrapper explicit as
  `Task.FromResult<Result<TOut>>(...)` so the compiler does not stop at the intermediate
  no-payload descriptor
- keep REST, GraphQL, JSON-RPC, and messaging envelopes in the adapter layer instead of making
  every behavior return an HTTP-shaped wrapper
- keep `BehaviorResult<T>` available as a compatibility alias when older code or samples have not
  migrated to the shorter `Result<T>` name yet

Recommended baseline:

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
                $"Cart '{input.CartId}' was not found.",
                new BehaviorFault
                {
                    Code = "cart.not_found",
                    Message = $"Cart '{input.CartId}' was not found.",
                    Severity = BehaviorFaultSeverity.Warning
                });
        }

        return Result.Ok(new GetCartOutput(cart), message: "Cart resolved.");
    }
}
```

That shape lets REST project `404`, `409`, `400`, and similar responses without making the core
behavior contract depend on HTTP concerns such as `status`, `success`, or transport-specific
error envelopes.

## Configuration

```json
{
  "Engine": {
    "Behaviors": {
      "AutoRegister": false,
      "AutoRegisterAssemblies": [
        "Acme.Store.Service"
      ],
      "AutoRegisterExcludeAssemblyPrefixes": [
        "Acme.Store.Legacy."
      ]
    }
  }
}
```

`Engine:Behaviors` now controls discovery and auto-registration only. It no longer acts as a
per-behavior topology override surface, and explicit module ownership is now the preferred default
path. Turn `AutoRegister` on only when a host deliberately wants ambient assembly scanning as a
fallback.

## Resolution model

Resolved topology follows a small, explicit model:

1. explicit topology from `static ConfigureTopology(...)` or `Register<T>(b => ...)`
2. attribute-only baseline synthesis when no explicit topology exists and the behavior declares
   exactly one allowed pattern plus one or more allowed transports
3. fail fast when multiple patterns are declared and no topology source chooses one explicitly

## Transport identifiers

`http.jsonrpc` · `http.graphql` · `http.graphql-sse` · `http.graphql-ws` · `http.sse` · `http.ws` · `rabbitmq` · `kafka` · `in-memory` · `grpc`

For author-facing allowlists, `http.grpc` is accepted as an alias and normalizes to canonical
`grpc` at runtime.

## HTTP route-shape follow-through

Behavior metadata stays transport-neutral on purpose.

- use `[BehaviorAllowedPatterns]` plus `[BehaviorAllowedTransports]` alone when the behavior should
  use the attribute-only baseline and the pattern choice is unambiguous
- do not declare `http.rest` in behavior allowlists or topology; public REST is mapped by modules
  through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)`
- prefer `BehaviorModuleBase` or `RestBehaviorModuleBase` when a module should explicitly own the
  behaviors it ships instead of relying only on assembly scanning
- if a behavior declares multiple allowed patterns, add `ConfigureTopology(...)` or fluent
  registration so the runtime does not need to guess
- use `WithApiSurface(groupPath, operationPath)` when route-shaped generic transports should project
  a public path that differs from the default `behavior-id -> group/operation` split
- expect JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and WebSocket behavior bindings to reuse
  that shared API surface for canonical versioned routes
- keep GraphQL schema ownership focused on payload and protocol semantics even though its Cephalon
  behavior endpoint now participates in the shared prefix/version policy
- use `Cephalon.Behaviors.Http` route helpers such as `MapBehaviorRestGroup(...)` when a module
  needs a concrete REST method, route template, and OpenAPI surface
- expect generic behavior HTTP routes to stay runnable transport-adapter endpoints while REST
  OpenAPI + Scalar descriptions stay focused on module-owned REST helper endpoints by default
- keep HTTP-specific route shape in the adapter/helper layer so `Cephalon.Abstractions` and the
  core ABT contracts remain host-agnostic

## Performance characteristics

- Dispatch table built once at `EngineBuilder.Build()` into a `FrozenDictionary`
- Zero reflection on the hot dispatch path — `Expression.Lambda` compiled once per behavior type
- Transport bindings deferred to first request (`LazyTransportBinding`) — zero startup overhead per transport
- Compatibility matrix runs at startup only — no runtime overhead

## Related components

- Transport bindings: `Cephalon.Behaviors.Http` (M2 — shipped), `Cephalon.Behaviors.Messaging`
  (M3 — shipped)
- Pattern execution strategies: `Cephalon.Behaviors.Patterns` (M4 — shipped)
- Source generator: `Cephalon.Behaviors.SourceGen` (M5 — shipped)
- Runtime integration: `BehaviorRuntimeContributor`, `IBehaviorAdvisory`, `BehaviorDiagnostics`
  (M6 — shipped)
- Resilience runtime: [`Cephalon.Resilience`](resilience.md) ships the engine-managed policy
  resolver, circuit-breaker state registry, default exception classifier, and shared resilience
  execution context keys consumed by `Microsoft.Extensions.Resilience` pipelines. The
  behavior-coupled `BehaviorIdempotencyResolver`, `BehaviorResilienceRuntimeCatalog`, and
  `BehaviorResilienceExecutionMiddleware` remain inside `Cephalon.Behaviors` because they
  implement or consume the internal behavior dispatch contract plus the public
  `IBehaviorTypeRegistry` lookup. Consumers that need the resilience runtime without the full
  behavior dispatch substrate can take a direct dependency on `Cephalon.Resilience`.

## M2 HTTP Transport Pack (`Cephalon.Behaviors.Http`)

> Status: ✅ Shipped — commit c957966 · 516/516 tests

Adds generic HTTP transport bindings. Each binding implements `IHttpBehaviorBinding` and is lazily
initialized on first request.

| Transport ID | Binding | Route pattern |
|---|---|---|
| `http.jsonrpc` | `JsonRpcHttpBehaviorBinding` | Canonical `POST {JsonRpcPrefix}/{document}/{group}/{operation}` |
| `http.graphql` | `GraphqlHttpBehaviorBinding` | Canonical `POST {GraphQLPrefix}/{document}/{group}/{operation}` |
| `http.graphql-sse` | `GraphqlSseBehaviorBinding` | Canonical `POST {GraphQLSsePrefix}/{document}/{group}/{operation}` |
| `http.graphql-ws` | `GraphqlWsBehaviorBinding` | Canonical `GET {GraphQLWsPrefix}/{document}/{group}/{operation}` |
| `http.sse` | `SseBehaviorBinding` | Canonical `GET {SsePrefix}/{document}/{group}/{operation}` |
| `http.ws` | `WebSocketBehaviorBinding` | Canonical `GET {WsPrefix}/{document}/{group}/{operation}` |

## M6 Runtime Integration

> Status: Shipped — commit `62d386c` · 592/592 tests

Adds runtime observability, advisory system, EventStore wiring, and structured diagnostics to the
ABT stack.

### BehaviorRuntimeContributor

Implements `ITechnologyRuntimeContributor` and reports the behavior subsystem surface to
`/engine/snapshot`:

- Total registered behavior count
- Pattern distribution (cqrs / event-driven / saga-step / saga-choreography / process-manager / durable-execution / direct)
- Transport distribution across all registered behaviors
- Feature-gated behavior count across the active catalog
- Per-behavior required feature ids and owning module id when the resolved topology declares them

### IBehaviorAdvisory system

| Type | Description |
|------|-------------|
| `IBehaviorAdvisory` | Immutable advisory record: behavior ID, message, severity, optional exception |
| `IBehaviorAdvisoryContributor` | Extension point — implement to emit advisories at startup or runtime |
| `IBehaviorAdvisoryCatalog` | Read surface — enumerate all advisories raised across contributors |
| `BehaviorAdvisorySeverity` | `Info` / `Warning` / `Error` severity enum |
| `BehaviorAdvisoryCatalog` | Default implementation aggregating all registered `IBehaviorAdvisoryContributor` instances |

### IBehaviorContext.EventStore

`IBehaviorContext` gains an `EventStore` property (`IEventStore?`). Wired automatically when
`IEventStore` is registered:

- `DefaultBehaviorContext` — resolves from DI
- `KafkaBehaviorContext` — resolves from a per-message DI scope
- `RabbitMqBehaviorContext` — resolves from a per-delivery DI scope
- `TestBehaviorContext` — accepts injected `IEventStore?` for test scenarios

`BehaviorExecutionSlot` now also deserializes `JsonElement` payloads with
`JsonSerializerDefaults.Web`, so camelCase HTTP inputs bind cleanly into typical C# DTOs without
per-behavior casing workarounds.

### BehaviorDiagnostics EventId constants (5100-5109)

| Constant | EventId | Meaning |
|----------|---------|---------|
| `Dispatching` | 5100 | Behavior dispatch starting |
| `Dispatched` | 5101 | Behavior dispatch completed |
| `DispatchFailed` | 5102 | Behavior dispatch threw |
| `CompatibilityViolation` | 5103 | Compatibility rule triggered |
| `TopologyResolved` | 5104 | Per-behavior topology resolved |
| `BehaviorRegistered` | 5105 | Behavior added to catalog |
| `TransportBound` | 5106 | Transport binding succeeded |
| `TransportBindFailed` | 5107 | Transport binding failed |
| `AdvisoryRaised` | 5108 | Advisory emitted by a contributor |
| `SlotCompiled` | 5109 | `BehaviorExecutionSlot` compiled |
