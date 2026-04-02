# Cephalon.AspNetCore.Grpc

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.AspNetCore.Grpc)
## Namespaces

- `Cephalon.AspNetCore.Grpc.Contracts.Discovery`
- `Cephalon.AspNetCore.Grpc.Hosting`
- `Cephalon.AspNetCore.Grpc.Modules`

<a id="namespace-cephalon-aspnetcore-grpc-contracts-discovery"></a>

## Namespace Cephalon.AspNetCore.Grpc.Contracts.Discovery

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-discoveryreflection"></a>

### `DiscoveryReflection`

Holder for reflection information generated from Protos/discovery.proto

#### Declaration
```csharp
public static class DiscoveryReflection
```

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-discoveryreflection-descriptor"></a>

##### `Descriptor`

```csharp
FileDescriptor Descriptor { get; }
```

File descriptor for Protos/discovery.proto

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice"></a>

### `DiscoveryService`

Demonstrates the baseline unary and streaming discovery endpoints exposed by the Cephalon ASP.NET Core gRPC adapter.

#### Declaration
```csharp
public static class DiscoveryService
```

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-descriptor"></a>

##### `Descriptor`

```csharp
ServiceDescriptor Descriptor { get; }
```

Service descriptor

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-bindservice-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase"></a>

##### `BindService`

```csharp
ServerServiceDefinition BindService(DiscoveryServiceBase serviceImpl)
```

Creates service definition that can be registered with a server

Parameters:
- `serviceImpl`: An object implementing the server-side handling logic.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-bindservice-grpc-core-servicebinderbase-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase"></a>

##### `BindService`

```csharp
void BindService(ServiceBinderBase serviceBinder, DiscoveryServiceBase serviceImpl)
```

Register service method with a service binder with or without implementation. Useful when customizing the service binding logic. Note: this method is part of an experimental API that can change or be removed without any prior notice.

Parameters:
- `serviceBinder`: Service methods will be bound by calling `AddMethod` on this object.
- `serviceImpl`: An object implementing the server-side handling logic.

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservicebase"></a>

### `DiscoveryServiceBase`

Base class for server-side implementations of DiscoveryService

#### Declaration
```csharp
public abstract class DiscoveryServiceBase
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase-exchangegreetings-grpc-core-iasyncstreamreader-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-iserverstreamwriter-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-grpc-core-servercallcontext"></a>

##### `ExchangeGreetings`

```csharp
Task ExchangeGreetings(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
```

Exchanges greeting messages bidirectionally to validate duplex streaming support.

Returns: A task indicating completion of the handler.

Parameters:
- `requestStream`: Used for reading requests from the client.
- `responseStream`: Used for sending responses back to the client.
- `context`: The context of the server-side call handler being invoked.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase-sayhello-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-servercallcontext"></a>

##### `SayHello`

```csharp
Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
```

Returns a single greeting for the requested caller.

Returns: The response to send back to the client (wrapped by a task).

Parameters:
- `request`: The request received from the client.
- `context`: The context of the server-side call handler being invoked.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase-streamprinciples-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-grpc-core-iserverstreamwriter-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-grpc-core-servercallcontext"></a>

##### `StreamPrinciples`

```csharp
Task StreamPrinciples(PrinciplesRequest request, IServerStreamWriter<PrincipleReply> responseStream, ServerCallContext context)
```

Streams the host principles that describe the Cephalon runtime shape.

Returns: A task indicating completion of the handler.

Parameters:
- `request`: The request received from the client.
- `responseStream`: Used for sending responses back to the client.
- `context`: The context of the server-side call handler being invoked.

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-discoveryserviceclient"></a>

### `DiscoveryServiceClient`

Client for DiscoveryService

#### Declaration
```csharp
public class DiscoveryServiceClient
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-ctor-grpc-core-channelbase"></a>

##### `DiscoveryServiceClient`

```csharp
DiscoveryServiceClient(ChannelBase channel)
```

Creates a new client for DiscoveryService

Parameters:
- `channel`: The channel to use to make remote calls.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-ctor-grpc-core-callinvoker"></a>

##### `DiscoveryServiceClient`

```csharp
DiscoveryServiceClient(CallInvoker callInvoker)
```

Creates a new client for DiscoveryService that uses a custom `CallInvoker`.

Parameters:
- `callInvoker`: The callInvoker to use to make remote calls.

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-exchangegreetings-grpc-core-calloptions"></a>

##### `ExchangeGreetings`

```csharp
AsyncDuplexStreamingCall<HelloRequest, HelloReply> ExchangeGreetings(CallOptions options)
```

Exchanges greeting messages bidirectionally to validate duplex streaming support.

Returns: The call object.

Parameters:
- `options`: The options for the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-exchangegreetings-grpc-core-metadata-system-nullable-system-datetime-system-threading-cancellationtoken"></a>

##### `ExchangeGreetings`

```csharp
AsyncDuplexStreamingCall<HelloRequest, HelloReply> ExchangeGreetings(Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

Exchanges greeting messages bidirectionally to validate duplex streaming support.

Returns: The call object.

Parameters:
- `headers`: The initial metadata to send with the call. This parameter is optional.
- `deadline`: An optional deadline for the call. The call will be cancelled if deadline is hit.
- `cancellationToken`: An optional token for canceling the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhello-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-calloptions"></a>

##### `SayHello`

```csharp
HelloReply SayHello(HelloRequest request, CallOptions options)
```

Returns a single greeting for the requested caller.

Returns: The response received from the server.

Parameters:
- `request`: The request to send to the server.
- `options`: The options for the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhello-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-metadata-system-nullable-system-datetime-system-threading-cancellationtoken"></a>

##### `SayHello`

```csharp
HelloReply SayHello(HelloRequest request, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

Returns a single greeting for the requested caller.

Returns: The response received from the server.

Parameters:
- `request`: The request to send to the server.
- `headers`: The initial metadata to send with the call. This parameter is optional.
- `deadline`: An optional deadline for the call. The call will be cancelled if deadline is hit.
- `cancellationToken`: An optional token for canceling the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhelloasync-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-calloptions"></a>

##### `SayHelloAsync`

```csharp
AsyncUnaryCall<HelloReply> SayHelloAsync(HelloRequest request, CallOptions options)
```

Returns a single greeting for the requested caller.

Returns: The call object.

Parameters:
- `request`: The request to send to the server.
- `options`: The options for the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhelloasync-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-metadata-system-nullable-system-datetime-system-threading-cancellationtoken"></a>

##### `SayHelloAsync`

```csharp
AsyncUnaryCall<HelloReply> SayHelloAsync(HelloRequest request, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

Returns a single greeting for the requested caller.

Returns: The call object.

Parameters:
- `request`: The request to send to the server.
- `headers`: The initial metadata to send with the call. This parameter is optional.
- `deadline`: An optional deadline for the call. The call will be cancelled if deadline is hit.
- `cancellationToken`: An optional token for canceling the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-streamprinciples-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-grpc-core-calloptions"></a>

##### `StreamPrinciples`

```csharp
AsyncServerStreamingCall<PrincipleReply> StreamPrinciples(PrinciplesRequest request, CallOptions options)
```

Streams the host principles that describe the Cephalon runtime shape.

Returns: The call object.

Parameters:
- `request`: The request to send to the server.
- `options`: The options for the call.

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-streamprinciples-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-grpc-core-metadata-system-nullable-system-datetime-system-threading-cancellationtoken"></a>

##### `StreamPrinciples`

```csharp
AsyncServerStreamingCall<PrincipleReply> StreamPrinciples(PrinciplesRequest request, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

Streams the host principles that describe the Cephalon runtime shape.

Returns: The call object.

Parameters:
- `request`: The request to send to the server.
- `headers`: The initial metadata to send with the call. This parameter is optional.
- `deadline`: An optional deadline for the call. The call will be cancelled if deadline is hit.
- `cancellationToken`: An optional token for canceling the call.

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-helloreply"></a>

### `HelloReply`

Returns the generated greeting and related metadata.

#### Declaration
```csharp
public sealed class HelloReply
```

#### Fields

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-generatedatutcfieldnumber"></a>

##### `GeneratedAtUtcFieldNumber`

```csharp
const int GeneratedAtUtcFieldNumber
```

Field number for the "generated_at_utc" field.

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-messagefieldnumber"></a>

##### `MessageFieldNumber`

```csharp
const int MessageFieldNumber
```

Field number for the "message" field.

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-traitsfieldnumber"></a>

##### `TraitsFieldNumber`

```csharp
const int TraitsFieldNumber
```

Field number for the "traits" field.

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
string GeneratedAtUtc { get; set; }
```

The UTC timestamp when the reply was generated.

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

The message rendered for the caller.

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-traits"></a>

##### `Traits`

```csharp
RepeatedField<string> Traits { get; }
```

Additional traits or descriptors associated with the generated greeting.

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest"></a>

### `HelloRequest`

Describes the caller that is requesting a greeting.

#### Declaration
```csharp
public sealed class HelloRequest
```

#### Fields

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-namefieldnumber"></a>

##### `NameFieldNumber`

```csharp
const int NameFieldNumber
```

Field number for the "name" field.

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-name"></a>

##### `Name`

```csharp
string Name { get; set; }
```

The display name to greet.

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-principlereply"></a>

### `PrincipleReply`

Returns one principle from the streamed discovery sequence.

#### Declaration
```csharp
public sealed class PrincipleReply
```

#### Fields

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-principlefieldnumber"></a>

##### `PrincipleFieldNumber`

```csharp
const int PrincipleFieldNumber
```

Field number for the "principle" field.

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-principle"></a>

##### `Principle`

```csharp
string Principle { get; set; }
```

The principle text being streamed to the caller.

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest"></a>

### `PrinciplesRequest`

Requests the baseline Cephalon principles stream.

#### Declaration
```csharp
public sealed class PrinciplesRequest
```

<a id="namespace-cephalon-aspnetcore-grpc-hosting"></a>

## Namespace Cephalon.AspNetCore.Grpc.Hosting

<a id="type-cephalon-aspnetcore-grpc-hosting-grpctransportservicecollectionextensions"></a>

### `GrpcTransportServiceCollectionExtensions`

Registers the gRPC ASP.NET Core transport adapter for Cephalon.

#### Declaration
```csharp
public static class GrpcTransportServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-hosting-grpctransportservicecollectionextensions-addgrpctransport-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddGrpcTransport`

```csharp
IServiceCollection AddGrpcTransport(this IServiceCollection services)
```

Adds the gRPC transport mapper and ASP.NET Core gRPC services to the service collection.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.

<a id="member-m-cephalon-aspnetcore-grpc-hosting-grpctransportservicecollectionextensions-addgrpctransport-microsoft-aspnetcore-builder-webapplicationbuilder"></a>

##### `AddGrpcTransport`

```csharp
WebApplicationBuilder AddGrpcTransport(this WebApplicationBuilder builder)
```

Adds the gRPC transport mapper to a `WebApplicationBuilder`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.

<a id="namespace-cephalon-aspnetcore-grpc-modules"></a>

## Namespace Cephalon.AspNetCore.Grpc.Modules

<a id="type-cephalon-aspnetcore-grpc-modules-igrpcmodule"></a>

### `IGrpcModule`

Defines gRPC endpoint contributions made by a Cephalon module on ASP.NET Core.

#### Declaration
```csharp
public interface IGrpcModule
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-modules-igrpcmodule-mapgrpcendpoints-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapGrpcEndpoints`

```csharp
void MapGrpcEndpoints(IEndpointRouteBuilder endpoints)
```

Maps the module's gRPC endpoints onto the supplied endpoint route builder.

Parameters:
- `endpoints`: The endpoint route builder that receives the module routes.
