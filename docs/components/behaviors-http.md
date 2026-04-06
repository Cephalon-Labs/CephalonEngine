# Cephalon.Behaviors.Http

`Cephalon.Behaviors.Http` provides the HTTP Transport Pack for the Adaptive Behavior Topology (ABT) system.
It wires behavior topology descriptors to HTTP transports via 7 concrete `IHttpBehaviorBinding` implementations and a lazy-init registry.

## What it owns

- **IHttpBehaviorBinding** — thin adapter contract between HTTP endpoints and `BehaviorDispatcher`
- **IHttpBehaviorBindingRegistry** — frozen lookup registry for all registered transport bindings
- **HttpBehaviorBindingRegistry** — default `FrozenDictionary`-backed registry implementation
- **LazyTransportBinding** — deferred-init wrapper; `MapAsync` is called exactly once on first request, keeping pod startup under 100 ms
- **DefaultBehaviorContext** — `IBehaviorContext` implementation built from `HttpContext` (correlation, tenant, user, trace from headers)
- **7 transport bindings** — REST, JSON-RPC 2.0, GraphQL (HTTP), GraphQL-SSE, GraphQL-WS, SSE, WebSocket
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

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .Register<PlaceOrderBehavior>(b => b.AsCqrs().ViaHttpRest().ViaHttpJsonRpc())
        .AddHttpBehaviorBindings()
    )
);
```

## DefaultBehaviorContext header conventions

| Header | Maps to |
|---|---|
| `X-Correlation-Id` | `Metadata["CorrelationId"]` |
| `X-Tenant-Id` | `Metadata["TenantId"]` |
| `Authorization` (sub claim) | `Metadata["UserId"]` |
| `X-Meta-*` | `Metadata[key-without-prefix]` |

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver, compatibility rules (required dependency)
- `Cephalon.Abstractions` — behavior contracts
