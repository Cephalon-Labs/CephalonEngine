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
- **Built-in compatibility rules** — startup guardrails covering saga, process-manager, and CQRS
  constraints

## Key contracts (from `Cephalon.Abstractions.Behaviors`)

| Type | Description |
|------|-------------|
| `IAppBehavior<TIn, TOut>` | Single behavior interface — `HandleAsync` + optional `static virtual ConfigureTopology` |
| `IBehaviorContext` | Transport-neutral ambient API: `PublishAsync`, `SendAsync`, `ReplyAsync`, saga state, correlation |
| `IBehaviorTopologyBuilder` | Fluent builder: `AsCqrs()`, `AsEventDriven()`, `ViaHttpJsonRpc()`, `ViaRabbitMq()`, etc. |
| `BehaviorApiSurfaceDescriptor` | Shared logical route surface for route-shaped generic HTTP transports; defaulted from the behavior id and overrideable through `WithApiSurface(...)` |
| `BehaviorTopologyDescriptor` | Resolved per-behavior config: pattern, transports, feature flags, and shared API surface |
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

## Configuration

```json
{
  "Engine": {
    "Behaviors": {
      "AutoRegister": true,
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
per-behavior topology override surface.

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
  through `MapEndpoints(...)` plus `MapBehaviorRestGroup(...)`
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
- Pattern distribution (cqrs / event-driven / saga-step / process-manager / direct)
- Transport distribution across all registered behaviors

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
- `KafkaBehaviorContext` — resolves from DI
- `RabbitMqBehaviorContext` — resolves from DI
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
