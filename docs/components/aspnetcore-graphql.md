# Cephalon.AspNetCore.GraphQL

`Cephalon.AspNetCore.GraphQL` adds GraphQL transport support to the ASP.NET Core host.

## What it owns

- GraphQL server registration through Hot Chocolate
- module marker contract for GraphQL-capable Cephalon modules
- GraphQL schema contribution helper for module `ConfigureServices(...)`
- GraphQL endpoint mapping under the ASP.NET Core transport surface

## Main surfaces

- `Hosting/GraphQLTransportServiceCollectionExtensions.cs`
- `Modules/IGraphQLModule.cs`
- `Routing/GraphQLTransportRouteMapper.cs`

## Source structure

- `Hosting`
- `Modules`
- `Routing`

## How it fits

Use this package when a host selects the `GraphQL` transport. The engine still owns transport selection and runtime introspection; this package only makes that choice executable on ASP.NET Core and gives modules a transport-specific place to contribute schema types.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
