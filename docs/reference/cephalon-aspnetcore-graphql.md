# Cephalon.AspNetCore.GraphQL

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.AspNetCore.GraphQL)
## Namespaces

- `Cephalon.AspNetCore.GraphQL.Hosting`
- `Cephalon.AspNetCore.GraphQL.Modules`

<a id="namespace-cephalon-aspnetcore-graphql-hosting"></a>

## Namespace Cephalon.AspNetCore.GraphQL.Hosting

<a id="type-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions"></a>

### `GraphQLTransportServiceCollectionExtensions`

Registers the GraphQL ASP.NET Core transport adapter for Cephalon.

#### Declaration
```csharp
public static class GraphQLTransportServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-addgraphqltransport-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddGraphQLTransport`

```csharp
IServiceCollection AddGraphQLTransport(this IServiceCollection services)
```

Adds the GraphQL transport mapper and GraphQL server services to the service collection.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-addgraphqltransport-microsoft-aspnetcore-builder-webapplicationbuilder"></a>

##### `AddGraphQLTransport`

```csharp
WebApplicationBuilder AddGraphQLTransport(this WebApplicationBuilder builder)
```

Adds the GraphQL transport mapper and GraphQL server services to a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqlmutation-microsoft-extensions-dependencyinjection-iservicecollection-system-action-hotchocolate-types-iobjecttypedescriptor"></a>

##### `ConfigureGraphQLMutation`

```csharp
IServiceCollection ConfigureGraphQLMutation(this IServiceCollection services, Action<IObjectTypeDescriptor> configure)
```

Adds fields to the shared GraphQL mutation root used by Cephalon modules.

Returns: The same service collection for fluent composition.

Parameters:
- `services`: The service collection that owns the transport registration.
- `configure`: The callback that adds fields, arguments, and resolvers to `Mutation`.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqlmutation-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-hotchocolate-types-iobjecttypedescriptor"></a>

##### `ConfigureGraphQLMutation`

```csharp
WebApplicationBuilder ConfigureGraphQLMutation(this WebApplicationBuilder builder, Action<IObjectTypeDescriptor> configure)
```

Adds fields to the shared GraphQL mutation root on a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: The callback that adds fields, arguments, and resolvers to `Mutation`.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqlquery-microsoft-extensions-dependencyinjection-iservicecollection-system-action-hotchocolate-types-iobjecttypedescriptor"></a>

##### `ConfigureGraphQLQuery`

```csharp
IServiceCollection ConfigureGraphQLQuery(this IServiceCollection services, Action<IObjectTypeDescriptor> configure)
```

Adds fields to the shared GraphQL query root used by Cephalon modules.

Returns: The same service collection for fluent composition.

Parameters:
- `services`: The service collection that owns the transport registration.
- `configure`: The callback that adds fields, arguments, and resolvers to `Query`.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqlquery-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-hotchocolate-types-iobjecttypedescriptor"></a>

##### `ConfigureGraphQLQuery`

```csharp
WebApplicationBuilder ConfigureGraphQLQuery(this WebApplicationBuilder builder, Action<IObjectTypeDescriptor> configure)
```

Adds fields to the shared GraphQL query root on a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: The callback that adds fields, arguments, and resolvers to `Query`.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqlsubscription-microsoft-extensions-dependencyinjection-iservicecollection-system-action-hotchocolate-types-iobjecttypedescriptor"></a>

##### `ConfigureGraphQLSubscription`

```csharp
IServiceCollection ConfigureGraphQLSubscription(this IServiceCollection services, Action<IObjectTypeDescriptor> configure)
```

Adds fields to the shared GraphQL subscription root used by Cephalon modules.

Remarks: Subscription field registration only shapes the GraphQL schema. Modules or hosts still need to register a concrete Hot Chocolate subscription provider, such as `AddInMemorySubscriptions()`, through `ConfigureGraphQLTransport` when they want GraphQL-over-SSE or GraphQL-over-WebSocket operations to execute.

Returns: The same service collection for fluent composition.

Parameters:
- `services`: The service collection that owns the transport registration.
- `configure`: The callback that adds fields, arguments, and resolvers to `Subscription`.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqlsubscription-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-hotchocolate-types-iobjecttypedescriptor"></a>

##### `ConfigureGraphQLSubscription`

```csharp
WebApplicationBuilder ConfigureGraphQLSubscription(this WebApplicationBuilder builder, Action<IObjectTypeDescriptor> configure)
```

Adds fields to the shared GraphQL subscription root on a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: The callback that adds fields, arguments, and resolvers to `Subscription`.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqltransport-microsoft-extensions-dependencyinjection-iservicecollection-system-action-hotchocolate-execution-configuration-irequestexecutorbuilder"></a>

##### `ConfigureGraphQLTransport`

```csharp
IServiceCollection ConfigureGraphQLTransport(this IServiceCollection services, Action<IRequestExecutorBuilder> configure)
```

Applies GraphQL schema configuration for Cephalon modules and hosts.

Remarks: Hosts typically call `AddGraphQLTransport` once, while individual modules use this method from `ConfigureServices` to add query, mutation, subscription, scalar, or type-extension registrations without forcing the engine core to know anything about the GraphQL implementation.

Returns: The same service collection for fluent composition.

Parameters:
- `services`: The service collection that owns the transport registration.
- `configure`: The callback that extends the GraphQL request executor builder.

<a id="member-m-cephalon-aspnetcore-graphql-hosting-graphqltransportservicecollectionextensions-configuregraphqltransport-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-hotchocolate-execution-configuration-irequestexecutorbuilder"></a>

##### `ConfigureGraphQLTransport`

```csharp
WebApplicationBuilder ConfigureGraphQLTransport(this WebApplicationBuilder builder, Action<IRequestExecutorBuilder> configure)
```

Applies GraphQL schema configuration on a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: The callback that extends the GraphQL request executor builder.

<a id="namespace-cephalon-aspnetcore-graphql-modules"></a>

## Namespace Cephalon.AspNetCore.GraphQL.Modules

<a id="type-cephalon-aspnetcore-graphql-modules-igraphqlmodule"></a>

### `IGraphQLModule`

Marks a Cephalon module as contributing GraphQL schema or resolver behavior on ASP.NET Core.

Remarks: Implementing modules should also register their GraphQL query, mutation, subscription, or type-extension services from `ConfigureServices` by calling `ConfigureGraphQLQuery(...)`, `ConfigureGraphQLMutation(...)`, `ConfigureGraphQLSubscription(...)`, or `ConfigureGraphQLTransport(...)` on the shared service collection. Subscription fields still require a concrete Hot Chocolate subscription provider, such as `AddInMemorySubscriptions()`, to execute over the built-in GraphQL-over-SSE or GraphQL-over-WebSocket routes.

#### Declaration
```csharp
public interface IGraphQLModule
```
