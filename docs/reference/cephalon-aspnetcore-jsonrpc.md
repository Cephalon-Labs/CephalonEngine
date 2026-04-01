# Cephalon.AspNetCore.JsonRpc

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.AspNetCore.JsonRpc)
## Namespaces

- `Cephalon.AspNetCore.JsonRpc.Hosting`
- `Cephalon.AspNetCore.JsonRpc.Modules`

<a id="namespace-cephalon-aspnetcore-jsonrpc-hosting"></a>

## Namespace Cephalon.AspNetCore.JsonRpc.Hosting

<a id="type-cephalon-aspnetcore-jsonrpc-hosting-jsonrpctransportservicecollectionextensions"></a>

### `JsonRpcTransportServiceCollectionExtensions`

Registers the JSON-RPC ASP.NET Core transport adapter for Cephalon.

#### Declaration
```csharp
public static class JsonRpcTransportServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-jsonrpc-hosting-jsonrpctransportservicecollectionextensions-addjsonrpctransport-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddJsonRpcTransport`

```csharp
IServiceCollection AddJsonRpcTransport(this IServiceCollection services)
```

Adds the JSON-RPC transport mapper to the service collection.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.

<a id="member-m-cephalon-aspnetcore-jsonrpc-hosting-jsonrpctransportservicecollectionextensions-addjsonrpctransport-microsoft-aspnetcore-builder-webapplicationbuilder"></a>

##### `AddJsonRpcTransport`

```csharp
WebApplicationBuilder AddJsonRpcTransport(this WebApplicationBuilder builder)
```

Adds the JSON-RPC transport mapper to a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.

<a id="namespace-cephalon-aspnetcore-jsonrpc-modules"></a>

## Namespace Cephalon.AspNetCore.JsonRpc.Modules

<a id="type-cephalon-aspnetcore-jsonrpc-modules-ijsonrpcmodule"></a>

### `IJsonRpcModule`

Defines JSON-RPC endpoint contributions made by a Cephalon module on ASP.NET Core.

#### Declaration
```csharp
public interface IJsonRpcModule
```

#### Methods

<a id="member-m-cephalon-aspnetcore-jsonrpc-modules-ijsonrpcmodule-mapjsonrpcendpoints-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapJsonRpcEndpoints`

```csharp
void MapJsonRpcEndpoints(IEndpointRouteBuilder endpoints)
```

Maps the module's JSON-RPC endpoints onto the supplied endpoint route builder.

Parameters:
- `endpoints`: The endpoint route builder that receives the module routes.
