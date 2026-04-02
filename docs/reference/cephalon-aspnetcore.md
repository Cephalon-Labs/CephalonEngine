# Cephalon.AspNetCore

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.AspNetCore)
## Namespaces

- `Cephalon.AspNetCore.Diagnostics`
- `Cephalon.AspNetCore.Documentation`
- `Cephalon.AspNetCore.Hosting`
- `Cephalon.AspNetCore.Modules`
- `Cephalon.AspNetCore.Transformers`
- `Cephalon.AspNetCore.Transports.Rest`
- `Cephalon.AspNetCore.Transports.ServerSentEvents`
- `Cephalon.AspNetCore.Transports.WebSockets`

<a id="namespace-cephalon-aspnetcore-diagnostics"></a>

## Namespace Cephalon.AspNetCore.Diagnostics

<a id="type-cephalon-aspnetcore-diagnostics-diagnosticssurface"></a>

### `DiagnosticsSurface`

Describes the operator-facing diagnostics surface exposed by a Cephalon ASP.NET Core host.

#### Declaration
```csharp
public sealed class DiagnosticsSurface
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-diagnostics-diagnosticssurface-ctor-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-engine-diagnostics-diagnosticsconvention-cephalon-engine-runtime-runtimehealthreport-cephalon-engine-runtime-runtimehealthreport-system-string-system-string-system-string"></a>

##### `DiagnosticsSurface`

```csharp
DiagnosticsSurface(string MeterName, string ActivitySourceName, IReadOnlyList<string> Counters, IReadOnlyList<DiagnosticsConvention> Conventions, RuntimeHealthReport Liveness, RuntimeHealthReport Readiness, string SummaryPath, string LivenessPath, string ReadinessPath)
```

Describes the operator-facing diagnostics surface exposed by a Cephalon ASP.NET Core host.

Parameters:
- `MeterName`: The meter name used for engine metrics.
- `ActivitySourceName`: The activity source name used for engine tracing.
- `Counters`: The built-in counter names exposed by the engine.
- `Conventions`: The published diagnostics conventions and event-id catalogs visible to the current host.
- `Liveness`: The current liveness report.
- `Readiness`: The current readiness report.
- `SummaryPath`: The aggregate health endpoint path.
- `LivenessPath`: The liveness endpoint path.
- `ReadinessPath`: The readiness endpoint path.

#### Properties

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-activitysourcename"></a>

##### `ActivitySourceName`

```csharp
string ActivitySourceName { get; set; }
```

The activity source name used for engine tracing.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-conventions"></a>

##### `Conventions`

```csharp
IReadOnlyList<DiagnosticsConvention> Conventions { get; set; }
```

The published diagnostics conventions and event-id catalogs visible to the current host.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-counters"></a>

##### `Counters`

```csharp
IReadOnlyList<string> Counters { get; set; }
```

The built-in counter names exposed by the engine.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-liveness"></a>

##### `Liveness`

```csharp
RuntimeHealthReport Liveness { get; set; }
```

The current liveness report.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-livenesspath"></a>

##### `LivenessPath`

```csharp
string LivenessPath { get; set; }
```

The liveness endpoint path.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-metername"></a>

##### `MeterName`

```csharp
string MeterName { get; set; }
```

The meter name used for engine metrics.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-readiness"></a>

##### `Readiness`

```csharp
RuntimeHealthReport Readiness { get; set; }
```

The current readiness report.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-readinesspath"></a>

##### `ReadinessPath`

```csharp
string ReadinessPath { get; set; }
```

The readiness endpoint path.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticssurface-summarypath"></a>

##### `SummaryPath`

```csharp
string SummaryPath { get; set; }
```

The aggregate health endpoint path.

<a id="namespace-cephalon-aspnetcore-documentation"></a>

## Namespace Cephalon.AspNetCore.Documentation

<a id="type-cephalon-aspnetcore-documentation-referencedocshostingoptions"></a>

### `ReferenceDocsHostingOptions`

Configures how an ASP.NET Core host serves generated Cephalon reference documentation.

Remarks: These options belong to the host layer rather than the engine core because they describe how already-generated static documentation should be exposed over HTTP.

#### Declaration
```csharp
public sealed class ReferenceDocsHostingOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-documentation-referencedocshostingoptions-ctor"></a>

##### `ReferenceDocsHostingOptions`

```csharp
ReferenceDocsHostingOptions()
```

Creates reference-doc hosting options with the default hosted-doc route settings.

#### Fields

<a id="member-f-cephalon-aspnetcore-documentation-referencedocshostingoptions-sectionname"></a>

##### `SectionName`

```csharp
const string SectionName
```

Gets the default configuration section used for reference-doc hosting.

#### Properties

<a id="member-p-cephalon-aspnetcore-documentation-referencedocshostingoptions-defaultdocument"></a>

##### `DefaultDocument`

```csharp
string DefaultDocument { get; set; }
```

Gets or sets the document that should open when a user requests the route prefix itself.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocshostingoptions-directorypath"></a>

##### `DirectoryPath`

```csharp
string DirectoryPath { get; set; }
```

Gets or sets the directory that contains the generated reference-doc output.

Remarks: Relative paths are resolved against the ASP.NET Core content root.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocshostingoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether hosted reference docs should be exposed.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocshostingoptions-routeprefix"></a>

##### `RoutePrefix`

```csharp
string RoutePrefix { get; set; }
```

Gets or sets the route prefix where the documentation should be served.

Remarks: The value may be supplied with or without a leading slash. The host normalizes it into a rooted path such as `/reference`.

#### Methods

<a id="member-m-cephalon-aspnetcore-documentation-referencedocshostingoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string-system-string"></a>

##### `FromConfiguration`

```csharp
ReferenceDocsHostingOptions FromConfiguration(IConfiguration configuration, string sectionPath, string contentRootPath)
```

Binds reference-doc hosting options from configuration.

Returns: The bound and normalized hosting options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The section path that contains the hosting settings.
- `contentRootPath`: The application content root used to normalize relative documentation paths.

<a id="type-cephalon-aspnetcore-documentation-referencedocssurface"></a>

### `ReferenceDocsSurface`

Describes the operator-facing HTTP surface for hosted Cephalon reference documentation.

#### Declaration
```csharp
public sealed class ReferenceDocsSurface
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-documentation-referencedocssurface-ctor-system-boolean-system-boolean-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string"></a>

##### `ReferenceDocsSurface`

```csharp
ReferenceDocsSurface(bool Enabled, bool Available, string RoutePrefix, string DefaultDocument, string DefaultDocumentPath, string ReadmePath, string BrowserPath, string NamespaceIndexPath, string TypeIndexPath, string MemberIndexPath, string ManifestPath)
```

Describes the operator-facing HTTP surface for hosted Cephalon reference documentation.

Parameters:
- `Enabled`: Whether reference-doc hosting is enabled for the current host.
- `Available`: Whether the configured documentation directory and default document were found and mapped.
- `RoutePrefix`: The route prefix where the documentation is served.
- `DefaultDocument`: The document opened when the route prefix is requested.
- `DefaultDocumentPath`: The hosted path to the configured default document.
- `ReadmePath`: The hosted path to the reference-doc landing page.
- `BrowserPath`: The hosted path to the interactive browser UI.
- `NamespaceIndexPath`: The hosted path to the namespace index.
- `TypeIndexPath`: The hosted path to the type index.
- `MemberIndexPath`: The hosted path to the member index.
- `ManifestPath`: The hosted path to the machine-readable manifest.

#### Properties

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-available"></a>

##### `Available`

```csharp
bool Available { get; set; }
```

Whether the configured documentation directory and default document were found and mapped.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-browserpath"></a>

##### `BrowserPath`

```csharp
string BrowserPath { get; set; }
```

The hosted path to the interactive browser UI.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-defaultdocument"></a>

##### `DefaultDocument`

```csharp
string DefaultDocument { get; set; }
```

The document opened when the route prefix is requested.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-defaultdocumentpath"></a>

##### `DefaultDocumentPath`

```csharp
string DefaultDocumentPath { get; set; }
```

The hosted path to the configured default document.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Whether reference-doc hosting is enabled for the current host.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-manifestpath"></a>

##### `ManifestPath`

```csharp
string ManifestPath { get; set; }
```

The hosted path to the machine-readable manifest.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-memberindexpath"></a>

##### `MemberIndexPath`

```csharp
string MemberIndexPath { get; set; }
```

The hosted path to the member index.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-namespaceindexpath"></a>

##### `NamespaceIndexPath`

```csharp
string NamespaceIndexPath { get; set; }
```

The hosted path to the namespace index.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-readmepath"></a>

##### `ReadmePath`

```csharp
string ReadmePath { get; set; }
```

The hosted path to the reference-doc landing page.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-routeprefix"></a>

##### `RoutePrefix`

```csharp
string RoutePrefix { get; set; }
```

The route prefix where the documentation is served.

<a id="member-p-cephalon-aspnetcore-documentation-referencedocssurface-typeindexpath"></a>

##### `TypeIndexPath`

```csharp
string TypeIndexPath { get; set; }
```

The hosted path to the type index.

<a id="namespace-cephalon-aspnetcore-hosting"></a>

## Namespace Cephalon.AspNetCore.Hosting

<a id="type-cephalon-aspnetcore-hosting-enginewebapplicationbuilderextensions"></a>

### `EngineWebApplicationBuilderExtensions`

Registers the Cephalon ASP.NET Core host services on a `WebApplicationBuilder`.

#### Declaration
```csharp
public static class EngineWebApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-enginewebapplicationbuilderextensions-addcephalon-microsoft-aspnetcore-builder-webapplicationbuilder"></a>

##### `AddCephalon`

```csharp
WebApplicationBuilder AddCephalon(this WebApplicationBuilder builder)
```

Adds Cephalon to the builder using configuration-only engine setup.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.

<a id="member-m-cephalon-aspnetcore-hosting-enginewebapplicationbuilderextensions-addcephalon-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-cephalon-engine-composition-enginebuilder"></a>

##### `AddCephalon`

```csharp
WebApplicationBuilder AddCephalon(this WebApplicationBuilder builder, Action<EngineBuilder> configure)
```

Adds Cephalon to the builder and allows additional code-based engine configuration.

Remarks: This method wires OpenAPI, Scalar-ready document transformers, health checks, hosted runtime startup, and the built-in ASP.NET Core transport mappers before registering the engine itself.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: The callback that configures the underlying engine builder.

<a id="member-m-cephalon-aspnetcore-hosting-enginewebapplicationbuilderextensions-addcephalonhttplogging-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions"></a>

##### `AddCephalonHttpLogging`

```csharp
WebApplicationBuilder AddCephalonHttpLogging(this WebApplicationBuilder builder, Action<HttpRequestResponseLoggingOptions> configure)
```

Adds Cephalon's HTTP request and response logging options to the ASP.NET Core host.

Remarks: The logging contract is read from `Engine:Observability:HttpLogging` so teams can opt into request/response summaries and bounded body capture without introducing a separate host-specific section.

Returns: The same builder instance for fluent host composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: An optional callback that can extend or override the configuration-driven request-logging setup.

<a id="member-m-cephalon-aspnetcore-hosting-enginewebapplicationbuilderextensions-addcephalonprojectconfigurations-microsoft-aspnetcore-builder-webapplicationbuilder"></a>

##### `AddCephalonProjectConfigurations`

```csharp
WebApplicationBuilder AddCephalonProjectConfigurations(this WebApplicationBuilder builder)
```

Adds Cephalon's project-configuration conventions to the ASP.NET Core builder.

Remarks: This loads split configuration files from the project's `Configurations` folder so settings such as engine, OpenAPI, CORS, or hosted-doc options do not need to live in one large `appsettings.json` file.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.

<a id="member-m-cephalon-aspnetcore-hosting-enginewebapplicationbuilderextensions-addreferencedocshosting-microsoft-aspnetcore-builder-webapplicationbuilder-system-action-cephalon-aspnetcore-documentation-referencedocshostingoptions"></a>

##### `AddReferenceDocsHosting`

```csharp
WebApplicationBuilder AddReferenceDocsHosting(this WebApplicationBuilder builder, Action<ReferenceDocsHostingOptions> configure)
```

Adds hosted reference-doc configuration to the ASP.NET Core host.

Remarks: Reference-doc hosting stays in the host layer because it serves already-generated static artifacts such as `browse.html`, `members.md`, and `reference-manifest.json`.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The ASP.NET Core application builder to extend.
- `configure`: An optional callback that can extend or override the configuration-driven hosting setup.

<a id="type-cephalon-aspnetcore-hosting-enginewebapplicationextensions"></a>

### `EngineWebApplicationExtensions`

Maps the operator-facing HTTP surface exposed by a Cephalon ASP.NET Core host.

#### Declaration
```csharp
public static class EngineWebApplicationExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-enginewebapplicationextensions-mapcephalon-microsoft-aspnetcore-builder-webapplication"></a>

##### `MapCephalon`

```csharp
WebApplication MapCephalon(this WebApplication app)
```

Maps Cephalon runtime, diagnostics, transport, and documentation endpoints onto the application.

Remarks: This method maps the engine introspection surface under `/engine`, health and diagnostics endpoints, and the routes contributed by the transports selected in the runtime manifest.

When the REST transport is active, it also enables OpenAPI and Scalar documentation while keeping non-REST protocol routes out of the generated API description.

Returns: The same application instance for fluent host composition.

Parameters:
- `app`: The ASP.NET Core application to extend.

<a id="type-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions"></a>

### `HttpRequestResponseLoggingOptions`

Configures opt-in HTTP request and response logging for Cephalon ASP.NET Core hosts.

Remarks: These settings are read from `Engine:Observability:HttpLogging` by default. Request and response bodies are captured only for textual content types such as JSON, XML, GraphQL, form payloads, and `text/*` responses, and body capture is truncated to the configured limits.

#### Declaration
```csharp
public sealed class HttpRequestResponseLoggingOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-ctor"></a>

##### `HttpRequestResponseLoggingOptions`

```csharp
HttpRequestResponseLoggingOptions()
```

Creates request and response logging options with body capture disabled by default.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-enabled"></a>

##### `Enabled`

```csharp
bool Enabled { get; set; }
```

Gets or sets a value indicating whether the ASP.NET Core host should log request and response summaries.

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-logrequestbody"></a>

##### `LogRequestBody`

```csharp
bool LogRequestBody { get; set; }
```

Gets or sets a value indicating whether textual request bodies should be logged.

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-logresponsebody"></a>

##### `LogResponseBody`

```csharp
bool LogResponseBody { get; set; }
```

Gets or sets a value indicating whether textual response bodies should be logged.

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-requestbodylimit"></a>

##### `RequestBodyLimit`

```csharp
int RequestBodyLimit { get; set; }
```

Gets or sets the maximum number of request-body characters to log before the payload is truncated.

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-responsebodylimit"></a>

##### `ResponseBodyLimit`

```csharp
int ResponseBodyLimit { get; set; }
```

Gets or sets the maximum number of response-body characters to log before the payload is truncated.

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
HttpRequestResponseLoggingOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds request and response logging options from configuration.

Returns: The bound request and response logging options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="type-cephalon-aspnetcore-hosting-itransportroutemapper"></a>

### `ITransportRouteMapper`

Maps the routes associated with one selected transport onto an ASP.NET Core host.

#### Declaration
```csharp
public interface ITransportRouteMapper
```

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-itransportroutemapper-transportid"></a>

##### `TransportId`

```csharp
string TransportId { get; }
```

Gets the transport identifier that this mapper handles.

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-itransportroutemapper-maproutes-microsoft-aspnetcore-builder-webapplication-cephalon-engine-runtime-iruntime"></a>

##### `MapRoutes`

```csharp
void MapRoutes(WebApplication app, IRuntime runtime)
```

Maps the transport's routes onto the supplied application.

Parameters:
- `app`: The ASP.NET Core application to extend.
- `runtime`: The runtime whose manifest and services back the mapped routes.

<a id="namespace-cephalon-aspnetcore-modules"></a>

## Namespace Cephalon.AspNetCore.Modules

<a id="type-cephalon-aspnetcore-modules-iendpointmodule"></a>

### `IEndpointModule`

Legacy REST module contract kept for compatibility with earlier Cephalon hosts.

Remarks: New module code should generally implement `IRestModule` directly.

#### Declaration
```csharp
public interface IEndpointModule
```

#### Methods

<a id="member-m-cephalon-aspnetcore-modules-iendpointmodule-mapendpoints-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapEndpoints`

```csharp
void MapEndpoints(IEndpointRouteBuilder endpoints)
```

Maps the module's REST endpoints onto the supplied endpoint route builder.

Parameters:
- `endpoints`: The endpoint route builder that receives the module routes.

<a id="namespace-cephalon-aspnetcore-transformers"></a>

## Namespace Cephalon.AspNetCore.Transformers

<a id="type-cephalon-aspnetcore-transformers-xmlcommentsdocumenttransformer"></a>

### `XmlCommentsDocumentTransformer`

Enriches generated OpenAPI schemas with XML documentation comments discovered from loaded assemblies.

Remarks: This transformer keeps OpenAPI descriptions aligned with the XML comments written on public contracts, including summaries, remarks, and examples when available.

#### Declaration
```csharp
public sealed class XmlCommentsDocumentTransformer
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-transformers-xmlcommentsdocumenttransformer-ctor-system-string"></a>

##### `XmlCommentsDocumentTransformer`

```csharp
XmlCommentsDocumentTransformer(string[] xmlFiles)
```

Enriches generated OpenAPI schemas with XML documentation comments discovered from loaded assemblies.

Remarks: This transformer keeps OpenAPI descriptions aligned with the XML comments written on public contracts, including summaries, remarks, and examples when available.

Parameters:
- `xmlFiles`: Optional explicit XML documentation files to scan. When omitted, the transformer searches the current application assemblies and base directory for generated XML documentation files.

#### Methods

<a id="member-m-cephalon-aspnetcore-transformers-xmlcommentsdocumenttransformer-transformasync-microsoft-openapi-openapidocument-microsoft-aspnetcore-openapi-openapidocumenttransformercontext-system-threading-cancellationtoken"></a>

##### `TransformAsync`

```csharp
Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
```

Applies XML comment data to the OpenAPI document schemas produced for the current request.

Returns: A task that completes when the transformation has finished.

Parameters:
- `document`: The OpenAPI document being transformed.
- `context`: The transformation context for the current document generation.
- `cancellationToken`: A token that can cancel document transformation.

<a id="namespace-cephalon-aspnetcore-transports-rest"></a>

## Namespace Cephalon.AspNetCore.Transports.Rest

<a id="type-cephalon-aspnetcore-transports-rest-irestmodule"></a>

### `IRestModule`

Defines REST endpoint contributions made by a Cephalon module on ASP.NET Core.

#### Declaration
```csharp
public interface IRestModule
```

#### Methods

<a id="member-m-cephalon-aspnetcore-transports-rest-irestmodule-maprestendpoints-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapRestEndpoints`

```csharp
void MapRestEndpoints(IEndpointRouteBuilder endpoints)
```

Maps the module's REST endpoints onto the supplied endpoint route builder.

Parameters:
- `endpoints`: The endpoint route builder that receives the module routes.

<a id="type-cephalon-aspnetcore-transports-rest-restendpointconventionbuilderextensions"></a>

### `RestEndpointConventionBuilderExtensions`

Adds Cephalon-specific conventions to REST route handlers.

#### Declaration
```csharp
public static class RestEndpointConventionBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-transports-rest-restendpointconventionbuilderextensions-requirecapability-microsoft-aspnetcore-builder-routehandlerbuilder-system-string"></a>

##### `RequireCapability`

```csharp
RouteHandlerBuilder RequireCapability(this RouteHandlerBuilder builder, string capabilityKey)
```

Requires a Cephalon capability decision before a REST endpoint can execute.

Remarks: This guard enforces the current trust and capability policy at the HTTP boundary. If the capability is denied, the endpoint returns a `403 Forbidden` problem response.

Returns: The same route handler builder for further convention chaining.

Parameters:
- `builder`: The route handler builder to protect.
- `capabilityKey`: The capability key that must be allowed for the request.

<a id="namespace-cephalon-aspnetcore-transports-serversentevents"></a>

## Namespace Cephalon.AspNetCore.Transports.ServerSentEvents

<a id="type-cephalon-aspnetcore-transports-serversentevents-iserversenteventsmodule"></a>

### `IServerSentEventsModule`

Defines Server-Sent Events endpoint contributions made by a Cephalon module on ASP.NET Core.

#### Declaration
```csharp
public interface IServerSentEventsModule
```

#### Methods

<a id="member-m-cephalon-aspnetcore-transports-serversentevents-iserversenteventsmodule-mapserversentevents-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapServerSentEvents`

```csharp
void MapServerSentEvents(IEndpointRouteBuilder endpoints)
```

Maps the module's Server-Sent Events endpoints onto the supplied endpoint route builder.

Parameters:
- `endpoints`: The endpoint route builder that receives the module routes.

<a id="namespace-cephalon-aspnetcore-transports-websockets"></a>

## Namespace Cephalon.AspNetCore.Transports.WebSockets

<a id="type-cephalon-aspnetcore-transports-websockets-iwebsocketmodule"></a>

### `IWebSocketModule`

Defines WebSocket endpoint contributions made by a Cephalon module on ASP.NET Core.

#### Declaration
```csharp
public interface IWebSocketModule
```

#### Methods

<a id="member-m-cephalon-aspnetcore-transports-websockets-iwebsocketmodule-mapwebsocketendpoints-microsoft-aspnetcore-routing-iendpointroutebuilder"></a>

##### `MapWebSocketEndpoints`

```csharp
void MapWebSocketEndpoints(IEndpointRouteBuilder endpoints)
```

Maps the module's WebSocket endpoints onto the supplied endpoint route builder.

Parameters:
- `endpoints`: The endpoint route builder that receives the module routes.
