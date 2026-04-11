# Cephalon.AspNetCore.GraphQL

`Cephalon.AspNetCore.GraphQL` adds GraphQL transport support to the ASP.NET Core host.

## What it owns

- GraphQL server registration through Hot Chocolate
- module marker contract for GraphQL-capable Cephalon modules
- GraphQL schema contribution helper for module `ConfigureServices(...)`
- separate built-in GraphQL HTTP, schema, GraphQL-over-SSE, and GraphQL-over-WebSocket endpoint mapping under the ASP.NET Core transport surface

## Main surfaces

- `Hosting/GraphQLTransportServiceCollectionExtensions.cs`
- `Modules/IGraphQLModule.cs`
- `Routing/GraphQLTransportRouteMapper.cs`

## Source structure

- `Hosting`
- `Modules`
- `Routing`

## How it fits

Use this package when a host selects the `GraphQL` transport. The engine still owns transport
selection and runtime introspection; this package only makes that choice executable on ASP.NET Core
and gives modules a transport-specific place to contribute schema types.

The built-in host surface now keeps the GraphQL protocols explicit while still following the shared
`ApiRoutes:Prefixes` contract. By default the package maps GraphQL HTTP at `/graphql`, the schema
document at `/graphql/schema`, GraphQL-over-SSE at `/graphql-sse`, and GraphQL-over-WebSocket at
`/graphql-ws`. Those routes also participate in the same ASP.NET Core rate-limiting catalog, so
`/engine/rate-limiting` can describe them truthfully as request-response, long-lived stream, or
long-lived connection surfaces instead of flattening them into one generic GraphQL label.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
