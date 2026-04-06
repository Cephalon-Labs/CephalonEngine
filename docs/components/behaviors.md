# Cephalon.Behaviors

`Cephalon.Behaviors` is the Adaptive Behavior Topology (ABT) runtime baseline for Cephalon.

## What it owns

- **BehaviorDispatcher** — O(1) `FrozenDictionary` dispatch table; zero reflection on the hot path
- **BehaviorTopologyResolver** — four-layer priority merge (attribute → defaults config → per-behavior config → fluent DI)
- **CompatibilityMatrix** — startup-time validation of resolved topologies against `IBehaviorCompatibilityRule` implementations
- **BehaviorExecutionSlot** — compiled `Expression.Lambda` invoker built once at startup
- **IBehaviorCatalog / IBehaviorRegistry** — populated by `IBehaviorContributor` implementations
- **Hosting** — `IEngineBuilder.AddBehaviors(configure?)` extension + `BehaviorModule`
- **Configuration** — `Engine:BehaviorDefaults` section and per-behavior `Engine:Behaviors` overrides
- **Built-in compatibility rules** — ABT-001 through ABT-006 covering saga, event-driven, process-manager, and CQRS constraints

## Key contracts (from `Cephalon.Abstractions.Behaviors`)

| Type | Description |
|------|-------------|
| `IAppBehavior<TIn, TOut>` | Single behavior interface — `HandleAsync` + optional `static virtual ConfigureTopology` |
| `IBehaviorContext` | Transport-neutral ambient API: `PublishAsync`, `SendAsync`, `ReplyAsync`, saga state, correlation |
| `IBehaviorTopologyBuilder` | Fluent builder: `AsCqrs()`, `AsEventDriven()`, `ViaHttpRest()`, `ViaRabbitMq()`, etc. |
| `BehaviorTopologyDescriptor` | Resolved per-behavior config: pattern, transports, feature flags |
| `[AppBehavior("id")]` | Declares a class as a named behavior |
| `[BehaviorAllowedPatterns]` | Opt-in security allowlist restricting which patterns config can activate |
| `[BehaviorAllowedTransports]` | Opt-in security allowlist restricting which transports config can activate |
| `IBehaviorCompatibilityRule` | Author extension point for custom topology validation |

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .Register<PlaceOrderBehavior>()
        .Register<GetOrderBehavior>(b => b.AsCqrs().ViaHttpRest())
    )
);
```

## Config-driven topology

```json
{
  "Engine": {
    "BehaviorDefaults": {
      "Pattern": "cqrs",
      "Transport": ["http.rest"]
    },
    "Behaviors": {
      "order.place": {
        "pattern": "cqrs",
        "transport": ["http.rest", "rabbitmq"]
      }
    }
  }
}
```

## Priority chain

Resolved topology is the result of a four-layer merge (lowest → highest priority):

1. `[AppBehavior]` attribute + `static virtual ConfigureTopology` — author compile-time intent
2. `Engine:BehaviorDefaults` config section — project-level ops default
3. `Engine:Behaviors` per-behavior entry — per-behavior ops override
4. `Register<T>(b => ...)` fluent DI callback — runtime code override

## Transport identifiers

`http.rest` · `http.jsonrpc` · `http.graphql` · `http.graphql-sse` · `http.graphql-ws` · `http.sse` · `http.ws` · `rabbitmq` · `kafka` · `in-memory` · `grpc`

## Performance characteristics

- Dispatch table built once at `EngineBuilder.Build()` into a `FrozenDictionary`
- Zero reflection on the hot dispatch path — `Expression.Lambda` compiled once per behavior type
- Transport bindings deferred to first request (`LazyTransportBinding`) — zero startup overhead per transport
- Compatibility matrix runs at startup only — no runtime overhead

## Related components

- Transport bindings: `Cephalon.Behaviors.Http` (M2 — shipped), `Cephalon.Behaviors.Messaging` (M3 — shipped)
- Pattern execution strategies: `Cephalon.Behaviors.Patterns` (M4 — shipped)
- Source generator: `Cephalon.Behaviors.SourceGen` (M5 — shipped)
- Runtime integration: `BehaviorRuntimeContributor`, `IBehaviorAdvisory`, `BehaviorDiagnostics` (M6 — shipped)

## M2 HTTP Transport Pack (`Cephalon.Behaviors.Http`)

> Status: ✅ Shipped — commit c957966 · 516/516 tests

Adds HTTP transport bindings. Each binding implements `IHttpBehaviorBinding` and is lazily initialized on first request.

| Transport ID | Binding | Route pattern |
|---|---|---|
| `http.rest` | `RestHttpBehaviorBinding` | `POST/GET /behaviors/{id}` |
| `http.jsonrpc` | `JsonRpcHttpBehaviorBinding` | `POST /behaviors/{id}/jsonrpc` |
| `http.graphql` | `GraphqlHttpBehaviorBinding` | `POST /behaviors/{id}/graphql` |
| `http.graphql-sse` | `GraphqlSseBehaviorBinding` | `POST /behaviors/{id}/graphql/sse` |
| `http.graphql-ws` | `GraphqlWsBehaviorBinding` | `GET /behaviors/{id}/graphql/ws` |
| `http.sse` | `SseBehaviorBinding` | `GET /behaviors/{id}/events` |
| `http.ws` | `WebSocketBehaviorBinding` | `GET /behaviors/{id}/ws` |

## M6 Runtime Integration

> Status: Shipped — commit `62d386c` · 592/592 tests

Adds runtime observability, advisory system, EventStore wiring, and structured diagnostics to the ABT stack.

### BehaviorRuntimeContributor

Implements `ITechnologyRuntimeContributor` and reports the behavior subsystem surface to `/engine/snapshot`:

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

`IBehaviorContext` gains an `EventStore` property (`IEventStore?`). Wired automatically when `IEventStore` is registered:

- `DefaultBehaviorContext` — resolves from DI
- `KafkaBehaviorContext` — resolves from DI
- `RabbitMqBehaviorContext` — resolves from DI
- `TestBehaviorContext` — accepts injected `IEventStore?` for test scenarios

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
