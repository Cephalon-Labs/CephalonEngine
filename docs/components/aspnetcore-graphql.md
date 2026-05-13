# Cephalon.AspNetCore.GraphQL

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore.GraphQL` adds GraphQL transport support to the ASP.NET Core host.

## What it owns

- GraphQL server registration through Hot Chocolate
- module marker contract for GraphQL-capable Cephalon modules
- shared query, mutation, and subscription root contribution helpers for module `ConfigureServices(...)`
- separate built-in GraphQL HTTP, schema, GraphQL-over-SSE, and GraphQL-over-WebSocket endpoint mapping under the ASP.NET Core transport surface
- config-driven execution resilience for built-in GraphQL query, mutation, and subscription root fields

## Main surfaces

- `Hosting/GraphQLTransportServiceCollectionExtensions.cs`
- `Modules/IGraphQLModule.cs`
- `Resilience/GraphQLExecutionResilienceMiddleware.cs`
- `Resilience/GraphQLExecutionResilienceRuntimeContributor.cs`
- `Routing/GraphQLTransportRouteMapper.cs`

## Source structure

- `Hosting`
- `Modules`
- `Resilience`
- `Routing`

## How it fits

Use this package when a host selects the `GraphQL` transport. The engine still owns transport
selection and runtime introspection; this package only makes that choice executable on ASP.NET Core
and gives modules a transport-specific place to contribute schema types.

Modules can now stay on the shared Cephalon GraphQL authoring path for all three schema roots by
calling `ConfigureGraphQLQuery(...)`, `ConfigureGraphQLMutation(...)`, and
`ConfigureGraphQLSubscription(...)` from `ConfigureServices(...)`. Modules or hosts that expose
subscription fields still need to register a concrete Hot Chocolate subscription provider, such as
`AddInMemorySubscriptions()`, through `ConfigureGraphQLTransport(...)` so the built-in
GraphQL-over-SSE and GraphQL-over-WebSocket routes can execute real subscription traffic instead of
only shaping the schema.

The built-in host surface now keeps the GraphQL protocols explicit while still following the shared
`ApiRoutes:Prefixes` contract. By default the package maps GraphQL HTTP at `/graphql`, the schema
document at `/graphql/schema`, GraphQL-over-SSE at `/graphql-sse`, and GraphQL-over-WebSocket at
`/graphql-ws`. Those routes also participate in the same ASP.NET Core rate-limiting catalog, so
`/engine/rate-limiting` can describe them truthfully as request-response, long-lived stream, or
long-lived connection surfaces instead of flattening them into one generic GraphQL label.

Built-in GraphQL query, mutation, and subscription root fields also participate in the engine-owned
`Engine:Resilience` contract. When `Timeout`, `CircuitBreaker`, or `Bulkhead` is configured, the
adapter registers a Hot Chocolate field middleware that enforces the selected policy without
Wolverine, consumer field middleware, or package-specific project code. Rejections stay
GraphQL-native through `errors[].extensions` metadata with stable Cephalon codes:
`graphql_execution_timeout`, `graphql_circuit_breaker_open`, and `graphql_bulkhead_rejected`.
The live posture is introspectable through `/engine/technology-surfaces/graphql` as
`graphql-execution-resilience`, including policy source, operation scope, live circuit state,
bulkhead counters, and `wolverineRequired=false` / `consumerCodeRequired=false`.

The hosting coverage now also proves those routes through real protocol behavior: custom schema
prefixes resolve SDL downloads correctly, the SSE route produces `text/event-stream`, and the
WebSocket route executes the `graphql-transport-ws` handshake plus `next` / `complete` frames.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
