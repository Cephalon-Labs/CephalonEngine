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

- Transport bindings live in future `Cephalon.Behaviors.Http` (M2), `Cephalon.Behaviors.Messaging` (M3)
- Pattern execution strategies live in future `Cephalon.Behaviors.Patterns` (M4)
- Source generator lives in future `Cephalon.Behaviors.SourceGen` (M5)

## M2 HTTP Transport Pack (`Cephalon.Behaviors.Http`)

> Status: 🚧 In Progress (Sprint 21)

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
