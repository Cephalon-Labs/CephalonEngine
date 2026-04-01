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

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase-exchangegreetings-grpc-core-iasyncstreamreader-1-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-iserverstreamwriter-1-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-grpc-core-servercallcontext"></a>

##### `ExchangeGreetings`

```csharp
Task ExchangeGreetings(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase-sayhello-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-servercallcontext"></a>

##### `SayHello`

```csharp
Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryservicebase-streamprinciples-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-grpc-core-iserverstreamwriter-1-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-grpc-core-servercallcontext"></a>

##### `StreamPrinciples`

```csharp
Task StreamPrinciples(PrinciplesRequest request, IServerStreamWriter<PrincipleReply> responseStream, ServerCallContext context)
```

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

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-exchangegreetings-grpc-core-metadata-system-nullable-1-system-datetime-system-threading-cancellationtoken"></a>

##### `ExchangeGreetings`

```csharp
AsyncDuplexStreamingCall<HelloRequest, HelloReply> ExchangeGreetings(Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhello-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-calloptions"></a>

##### `SayHello`

```csharp
HelloReply SayHello(HelloRequest request, CallOptions options)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhello-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-metadata-system-nullable-1-system-datetime-system-threading-cancellationtoken"></a>

##### `SayHello`

```csharp
HelloReply SayHello(HelloRequest request, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhelloasync-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-calloptions"></a>

##### `SayHelloAsync`

```csharp
AsyncUnaryCall<HelloReply> SayHelloAsync(HelloRequest request, CallOptions options)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-sayhelloasync-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-grpc-core-metadata-system-nullable-1-system-datetime-system-threading-cancellationtoken"></a>

##### `SayHelloAsync`

```csharp
AsyncUnaryCall<HelloReply> SayHelloAsync(HelloRequest request, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-streamprinciples-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-grpc-core-calloptions"></a>

##### `StreamPrinciples`

```csharp
AsyncServerStreamingCall<PrincipleReply> StreamPrinciples(PrinciplesRequest request, CallOptions options)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-discoveryservice-discoveryserviceclient-streamprinciples-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-grpc-core-metadata-system-nullable-1-system-datetime-system-threading-cancellationtoken"></a>

##### `StreamPrinciples`

```csharp
AsyncServerStreamingCall<PrincipleReply> StreamPrinciples(PrinciplesRequest request, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
```

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-helloreply"></a>

### `HelloReply`

#### Declaration
```csharp
public sealed class HelloReply
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-ctor"></a>

##### `HelloReply`

```csharp
HelloReply()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-ctor-cephalon-aspnetcore-grpc-contracts-discovery-helloreply"></a>

##### `HelloReply`

```csharp
HelloReply(HelloReply other)
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

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-descriptor"></a>

##### `Descriptor`

```csharp
MessageDescriptor Descriptor { get; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
string GeneratedAtUtc { get; set; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-parser"></a>

##### `Parser`

```csharp
MessageParser<HelloReply> Parser { get; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-traits"></a>

##### `Traits`

```csharp
RepeatedField<string> Traits { get; }
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-calculatesize"></a>

##### `CalculateSize`

```csharp
int CalculateSize()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-clone"></a>

##### `Clone`

```csharp
HelloReply Clone()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-mergefrom-cephalon-aspnetcore-grpc-contracts-discovery-helloreply"></a>

##### `MergeFrom`

```csharp
void MergeFrom(HelloReply other)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-mergefrom-google-protobuf-codedinputstream"></a>

##### `MergeFrom`

```csharp
void MergeFrom(CodedInputStream input)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-helloreply-writeto-google-protobuf-codedoutputstream"></a>

##### `WriteTo`

```csharp
void WriteTo(CodedOutputStream output)
```

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest"></a>

### `HelloRequest`

#### Declaration
```csharp
public sealed class HelloRequest
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-ctor"></a>

##### `HelloRequest`

```csharp
HelloRequest()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-ctor-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest"></a>

##### `HelloRequest`

```csharp
HelloRequest(HelloRequest other)
```

#### Fields

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-namefieldnumber"></a>

##### `NameFieldNumber`

```csharp
const int NameFieldNumber
```

Field number for the "name" field.

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-descriptor"></a>

##### `Descriptor`

```csharp
MessageDescriptor Descriptor { get; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-name"></a>

##### `Name`

```csharp
string Name { get; set; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-parser"></a>

##### `Parser`

```csharp
MessageParser<HelloRequest> Parser { get; }
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-calculatesize"></a>

##### `CalculateSize`

```csharp
int CalculateSize()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-clone"></a>

##### `Clone`

```csharp
HelloRequest Clone()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-mergefrom-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest"></a>

##### `MergeFrom`

```csharp
void MergeFrom(HelloRequest other)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-mergefrom-google-protobuf-codedinputstream"></a>

##### `MergeFrom`

```csharp
void MergeFrom(CodedInputStream input)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-hellorequest-writeto-google-protobuf-codedoutputstream"></a>

##### `WriteTo`

```csharp
void WriteTo(CodedOutputStream output)
```

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-principlereply"></a>

### `PrincipleReply`

#### Declaration
```csharp
public sealed class PrincipleReply
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-ctor"></a>

##### `PrincipleReply`

```csharp
PrincipleReply()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-ctor-cephalon-aspnetcore-grpc-contracts-discovery-principlereply"></a>

##### `PrincipleReply`

```csharp
PrincipleReply(PrincipleReply other)
```

#### Fields

<a id="member-f-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-principlefieldnumber"></a>

##### `PrincipleFieldNumber`

```csharp
const int PrincipleFieldNumber
```

Field number for the "principle" field.

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-descriptor"></a>

##### `Descriptor`

```csharp
MessageDescriptor Descriptor { get; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-parser"></a>

##### `Parser`

```csharp
MessageParser<PrincipleReply> Parser { get; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-principle"></a>

##### `Principle`

```csharp
string Principle { get; set; }
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-calculatesize"></a>

##### `CalculateSize`

```csharp
int CalculateSize()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-clone"></a>

##### `Clone`

```csharp
PrincipleReply Clone()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-mergefrom-cephalon-aspnetcore-grpc-contracts-discovery-principlereply"></a>

##### `MergeFrom`

```csharp
void MergeFrom(PrincipleReply other)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-mergefrom-google-protobuf-codedinputstream"></a>

##### `MergeFrom`

```csharp
void MergeFrom(CodedInputStream input)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlereply-writeto-google-protobuf-codedoutputstream"></a>

##### `WriteTo`

```csharp
void WriteTo(CodedOutputStream output)
```

<a id="type-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest"></a>

### `PrinciplesRequest`

#### Declaration
```csharp
public sealed class PrinciplesRequest
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-ctor"></a>

##### `PrinciplesRequest`

```csharp
PrinciplesRequest()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-ctor-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest"></a>

##### `PrinciplesRequest`

```csharp
PrinciplesRequest(PrinciplesRequest other)
```

#### Properties

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-descriptor"></a>

##### `Descriptor`

```csharp
MessageDescriptor Descriptor { get; }
```

<a id="member-p-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-parser"></a>

##### `Parser`

```csharp
MessageParser<PrinciplesRequest> Parser { get; }
```

#### Methods

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-calculatesize"></a>

##### `CalculateSize`

```csharp
int CalculateSize()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-clone"></a>

##### `Clone`

```csharp
PrinciplesRequest Clone()
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-mergefrom-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest"></a>

##### `MergeFrom`

```csharp
void MergeFrom(PrinciplesRequest other)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-mergefrom-google-protobuf-codedinputstream"></a>

##### `MergeFrom`

```csharp
void MergeFrom(CodedInputStream input)
```

<a id="member-m-cephalon-aspnetcore-grpc-contracts-discovery-principlesrequest-writeto-google-protobuf-codedoutputstream"></a>

##### `WriteTo`

```csharp
void WriteTo(CodedOutputStream output)
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
