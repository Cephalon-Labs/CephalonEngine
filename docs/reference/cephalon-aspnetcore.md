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

<a id="type-cephalon-aspnetcore-diagnostics-diagnosticsconventionssurface"></a>

### `DiagnosticsConventionsSurface`

Describes the canonical OpenTelemetry name set the Cephalon engine and its host adapters emit telemetry under, projected so operators and AI tooling can introspect what the engine emits without reading source.

#### Declaration
```csharp
public sealed class DiagnosticsConventionsSurface
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-diagnostics-diagnosticsconventionssurface-ctor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `DiagnosticsConventionsSurface`

```csharp
DiagnosticsConventionsSurface(IReadOnlyList<string> ActivitySources, IReadOnlyList<string> Meters, IReadOnlyList<string> CephalonAttributeKeys)
```

Describes the canonical OpenTelemetry name set the Cephalon engine and its host adapters emit telemetry under, projected so operators and AI tooling can introspect what the engine emits without reading source.

Parameters:
- `ActivitySources`: The stable `ActivitySource` names emitted by the engine and its shipped host adapters. Names come from the `Cephalon.Diagnostics` package's `CephalonActivitySources` static class.
- `Meters`: The stable `Meter` names. Names come from the `Cephalon.Diagnostics` package's `CephalonMeters` static class. These typically match the activity-source names because the engine emits both kinds of instruments under the same logical namespace.
- `CephalonAttributeKeys`: The `cephalon.*` attribute keys that complement OpenTelemetry semantic conventions. Names come from the `Cephalon.Diagnostics` package's `CephalonDiagnosticsAttributeKeys` static class. Engine concepts that have no OpenTelemetry semantic-convention equivalent live here; concepts that have a semconv equivalent are emitted under the OpenTelemetry attribute name directly and are not re-declared in this surface.

#### Properties

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticsconventionssurface-activitysources"></a>

##### `ActivitySources`

```csharp
IReadOnlyList<string> ActivitySources { get; set; }
```

The stable `ActivitySource` names emitted by the engine and its shipped host adapters. Names come from the `Cephalon.Diagnostics` package's `CephalonActivitySources` static class.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticsconventionssurface-cephalonattributekeys"></a>

##### `CephalonAttributeKeys`

```csharp
IReadOnlyList<string> CephalonAttributeKeys { get; set; }
```

The `cephalon.*` attribute keys that complement OpenTelemetry semantic conventions. Names come from the `Cephalon.Diagnostics` package's `CephalonDiagnosticsAttributeKeys` static class. Engine concepts that have no OpenTelemetry semantic-convention equivalent live here; concepts that have a semconv equivalent are emitted under the OpenTelemetry attribute name directly and are not re-declared in this surface.

<a id="member-p-cephalon-aspnetcore-diagnostics-diagnosticsconventionssurface-meters"></a>

##### `Meters`

```csharp
IReadOnlyList<string> Meters { get; set; }
```

The stable `Meter` names. Names come from the `Cephalon.Diagnostics` package's `CephalonMeters` static class. These typically match the activity-source names because the engine emits both kinds of instruments under the same logical namespace.

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

<a id="type-cephalon-aspnetcore-documentation-openapiendpointoptions"></a>

### `OpenApiEndpointOptions`

Configures the host-level OpenAPI JSON and Scalar UI endpoints exposed by Cephalon ASP.NET Core hosts.

Remarks: These options stay in the ASP.NET Core adapter because they describe HTTP route layout for generated documentation assets rather than engine-core behavior.

#### Declaration
```csharp
public sealed class OpenApiEndpointOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-documentation-openapiendpointoptions-ctor"></a>

##### `OpenApiEndpointOptions`

```csharp
OpenApiEndpointOptions()
```

Initializes a new `OpenApiEndpointOptions` with the canonical Cephalon OpenAPI and Scalar routes.

#### Fields

<a id="member-f-cephalon-aspnetcore-documentation-openapiendpointoptions-sectionname"></a>

##### `SectionName`

```csharp
const string SectionName
```

Gets the root configuration section used for OpenAPI endpoint routing.

#### Properties

<a id="member-p-cephalon-aspnetcore-documentation-openapiendpointoptions-behaviorrestdocumentedstatuscodes"></a>

##### `BehaviorRestDocumentedStatusCodes`

```csharp
IReadOnlyList<int> BehaviorRestDocumentedStatusCodes { get; set; }
```

Gets or sets the HTTP status codes that Cephalon's behavior-owned REST helpers publish in OpenAPI documents by default.

Remarks: This list controls documentation metadata only. It does not change the runtime HTTP status codes emitted by ASP.NET Core.

<a id="member-p-cephalon-aspnetcore-documentation-openapiendpointoptions-routepattern"></a>

##### `RoutePattern`

```csharp
string RoutePattern { get; set; }
```

Gets or sets the route pattern used by `MapOpenApi(...)`.

Remarks: The pattern must include the `{documentName}` placeholder so versioned and named documents remain addressable.

<a id="member-p-cephalon-aspnetcore-documentation-openapiendpointoptions-scalarrouteprefix"></a>

##### `ScalarRoutePrefix`

```csharp
string ScalarRoutePrefix { get; set; }
```

Gets or sets the route prefix used by the Scalar UI.

Remarks: The value may be supplied with or without a leading slash. Cephalon normalizes it to a rooted path such as `/scalar`.

#### Methods

<a id="member-m-cephalon-aspnetcore-documentation-openapiendpointoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
OpenApiEndpointOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds and normalizes OpenAPI endpoint options from configuration.

Returns: The normalized OpenAPI endpoint options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path to bind.

<a id="type-cephalon-aspnetcore-documentation-openapitagmetadata"></a>

### `OpenApiTagMetadata`

Describes OpenAPI tag metadata projected from ASP.NET Core route groups or endpoints.

#### Declaration
```csharp
public sealed class OpenApiTagMetadata
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-documentation-openapitagmetadata-ctor-system-string-system-string"></a>

##### `OpenApiTagMetadata`

```csharp
OpenApiTagMetadata(string Name, string Description)
```

Describes OpenAPI tag metadata projected from ASP.NET Core route groups or endpoints.

Parameters:
- `Name`: The public tag name shown in OpenAPI and Scalar.
- `Description`: The optional tag description shown in OpenAPI and Scalar.

#### Properties

<a id="member-p-cephalon-aspnetcore-documentation-openapitagmetadata-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

The optional tag description shown in OpenAPI and Scalar.

<a id="member-p-cephalon-aspnetcore-documentation-openapitagmetadata-name"></a>

##### `Name`

```csharp
string Name { get; set; }
```

The public tag name shown in OpenAPI and Scalar.

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

<a id="type-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest"></a>

### `AgentToolExecutionHttpRequest`

Represents the operator HTTP request body used to execute an agent tool.

#### Declaration
```csharp
public sealed class AgentToolExecutionHttpRequest
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-ctor"></a>

##### `AgentToolExecutionHttpRequest`

```csharp
AgentToolExecutionHttpRequest()
```

Initializes a new instance of the `AgentToolExecutionHttpRequest` class.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; set; }
```

Gets or initializes the actor identifier responsible for the run.

<a id="member-p-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-arguments"></a>

##### `Arguments`

```csharp
IReadOnlyDictionary<string, string> Arguments { get; set; }
```

Gets or initializes the tool arguments.

<a id="member-p-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-attempt"></a>

##### `Attempt`

```csharp
int? Attempt { get; set; }
```

Gets or initializes the execution attempt number.

<a id="member-p-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or initializes the correlation identifier for the run.

<a id="member-p-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Gets or initializes metadata to attach to the run.

<a id="member-p-cephalon-aspnetcore-hosting-agenttoolexecutionhttprequest-runid"></a>

##### `RunId`

```csharp
string RunId { get; set; }
```

Gets or initializes the caller-supplied run identifier.

<a id="type-cephalon-aspnetcore-hosting-apiroutesoptions"></a>

### `ApiRoutesOptions`

Configures host-level HTTP route prefixes for Cephalon ASP.NET Core transports.

Remarks: These settings describe the public HTTP surface of the ASP.NET Core adapter. They intentionally stay out of the engine core.

#### Declaration
```csharp
public sealed class ApiRoutesOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-apiroutesoptions-ctor"></a>

##### `ApiRoutesOptions`

```csharp
ApiRoutesOptions()
```

Initializes a new `ApiRoutesOptions` with the canonical Cephalon route-prefix defaults.

#### Fields

<a id="member-f-cephalon-aspnetcore-hosting-apiroutesoptions-sectionname"></a>

##### `SectionName`

```csharp
const string SectionName
```

Gets the configuration section used for API route settings.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-defaultbehaviordocumentname"></a>

##### `DefaultBehaviorDocumentName`

```csharp
string DefaultBehaviorDocumentName { get; set; }
```

Gets or sets the default document/version segment projected into generic behavior transport routes.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-graphqlprefix"></a>

##### `GraphQLPrefix`

```csharp
string GraphQLPrefix { get; set; }
```

Gets or sets the root prefix used by the built-in GraphQL transport mapper.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-graphqlsseprefix"></a>

##### `GraphQLSsePrefix`

```csharp
string GraphQLSsePrefix { get; set; }
```

Gets or sets the canonical prefix used by the generic behavior GraphQL-over-SSE binding surface.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-graphqlwsprefix"></a>

##### `GraphQLWsPrefix`

```csharp
string GraphQLWsPrefix { get; set; }
```

Gets or sets the canonical prefix used by the generic behavior GraphQL-over-WebSocket binding surface.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-grpcprefix"></a>

##### `GrpcPrefix`

```csharp
string GrpcPrefix { get; set; }
```

Gets or sets the root prefix used by the built-in gRPC transport mapper.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-jsonrpcprefix"></a>

##### `JsonRpcPrefix`

```csharp
string JsonRpcPrefix { get; set; }
```

Gets or sets the canonical prefix used by the generic behavior JSON-RPC binding surface.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-restprefix"></a>

##### `RestPrefix`

```csharp
string RestPrefix { get; set; }
```

Gets or sets the root prefix used by the built-in REST transport mapper.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-sseprefix"></a>

##### `SsePrefix`

```csharp
string SsePrefix { get; set; }
```

Gets or sets the canonical prefix used by the generic behavior Server-Sent Events binding surface.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-useresultmodelenvelope"></a>

##### `UseResultModelEnvelope`

```csharp
bool UseResultModelEnvelope { get; set; }
```

Gets or sets a value indicating whether behavior-aware REST endpoints should emit the Cephalon result envelope.

<a id="member-p-cephalon-aspnetcore-hosting-apiroutesoptions-wsprefix"></a>

##### `WsPrefix`

```csharp
string WsPrefix { get; set; }
```

Gets or sets the canonical prefix used by the generic behavior WebSocket binding surface.

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-apiroutesoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
ApiRoutesOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds and normalizes API route settings from configuration.

Returns: The normalized route settings.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path to bind.

<a id="type-cephalon-aspnetcore-hosting-audithistoryexporthttpresponseextensions"></a>

### `AuditHistoryExportHttpResponseExtensions`

Writes audit-history export responses for ASP.NET Core hosts.

#### Declaration
```csharp
public static class AuditHistoryExportHttpResponseExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-audithistoryexporthttpresponseextensions-writeaudithistoryndjsonasync-microsoft-aspnetcore-http-httpresponse-cephalon-abstractions-audit-iaudithistoryexporter-cephalon-abstractions-audit-audithistoryexportrequest-system-string-system-threading-cancellationtoken"></a>

##### `WriteAuditHistoryNdjsonAsync`

```csharp
Task WriteAuditHistoryNdjsonAsync(this HttpResponse response, IAuditHistoryExporter exporter, AuditHistoryExportRequest request, string fileName, CancellationToken cancellationToken)
```

Writes the supplied audit-history export as newline-delimited JSON.

Returns: A task that completes when the response has been written.

Parameters:
- `response`: The HTTP response to populate.
- `exporter`: The audit-history exporter that supplies the entries.
- `request`: The export request to execute.
- `fileName`: An optional download file name.
- `cancellationToken`: The token that cancels the response stream.

<a id="type-cephalon-aspnetcore-hosting-cephalonratelimitingendpointconventionbuilderextensions"></a>

### `CephalonRateLimitingEndpointConventionBuilderExtensions`

Applies Cephalon ASP.NET Core rate-limiting conventions to endpoint builders by consulting the host's effective rate-limiting policy catalog.

#### Declaration
```csharp
public static class CephalonRateLimitingEndpointConventionBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-cephalonratelimitingendpointconventionbuilderextensions-applycephalonratelimiting-1-0-system-iserviceprovider-system-string-system-string"></a>

##### `ApplyCephalonRateLimiting`

```csharp
TBuilder ApplyCephalonRateLimiting<TBuilder>(this TBuilder builder, IServiceProvider services, string transportId, string behaviorId)
```

Applies the effective Cephalon rate-limiting policy for the supplied transport and optional behavior identifier onto the endpoint builder.

Returns: The same builder instance for fluent composition.

Type parameters:
- `TBuilder`: The endpoint convention builder type.

Parameters:
- `builder`: The endpoint builder to configure.
- `services`: The application service provider.
- `transportId`: The transport identifier used by the endpoint.
- `behaviorId`: The optional behavior identifier when the endpoint maps a single behavior.

<a id="member-m-cephalon-aspnetcore-hosting-cephalonratelimitingendpointconventionbuilderextensions-hascephalonratelimiting-system-iserviceprovider-system-string-system-string"></a>

##### `HasCephalonRateLimiting`

```csharp
bool HasCephalonRateLimiting(this IServiceProvider services, string transportId, string behaviorId)
```

Determines whether the effective Cephalon rate-limiting policy for the supplied transport and optional behavior identifier actively enforces a limiter.

Returns: `true` when the endpoint will require a limiter; otherwise `false`.

Parameters:
- `services`: The application service provider.
- `transportId`: The transport identifier used by the endpoint.
- `behaviorId`: The optional behavior identifier when the endpoint maps a single behavior.

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

Remarks: This loads split configuration files from the project's `Configurations` folder so settings such as engine, OpenAPI, CORS, or hosted-doc options can be grouped by concern without taking away the standard `appsettings.json` and `appsettings.{Environment}.json` override flow.

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

The current full operator route surface uses ASP.NET Core Minimal API delegate binding, which is not a trim or Native AOT support claim for this package. The annotation is intentional so package-local analyzer builds report the boundary where consumers would otherwise receive framework warnings.

Returns: The same application instance for fluent host composition.

Parameters:
- `app`: The ASP.NET Core application to extend.

<a id="type-cephalon-aspnetcore-hosting-eventpublicationhttprequest"></a>

### `EventPublicationHttpRequest`

Represents the operator HTTP request body used to publish an event through the active eventing runtime.

#### Declaration
```csharp
public sealed class EventPublicationHttpRequest
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-eventpublicationhttprequest-ctor"></a>

##### `EventPublicationHttpRequest`

```csharp
EventPublicationHttpRequest()
```

Initializes a new instance of the `EventPublicationHttpRequest` class.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; set; }
```

Gets or initializes the actor identifier responsible for the publication.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or initializes the target event channel identifier.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; set; }
```

Gets or initializes the payload content type.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or initializes the correlation identifier for the publication.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; set; }
```

Gets or initializes the logical event type.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-headers"></a>

##### `Headers`

```csharp
IReadOnlyDictionary<string, string> Headers { get; set; }
```

Gets or initializes provider-specific event headers.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or initializes the caller-supplied publication identifier.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Gets or initializes metadata to attach to the publication.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset? OccurredAtUtc { get; set; }
```

Gets or initializes the event occurrence timestamp.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-payload"></a>

##### `Payload`

```csharp
JsonElement? Payload { get; set; }
```

Gets or initializes the event payload as JSON.

<a id="member-p-cephalon-aspnetcore-hosting-eventpublicationhttprequest-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or initializes the tenant identifier associated with the event.

<a id="type-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions"></a>

### `HttpRequestResponseLoggingOptions`

Configures opt-in HTTP request and response logging for Cephalon ASP.NET Core hosts.

Remarks: These settings are read from `Engine:Observability:HttpLogging` by default. Request and response bodies are captured only for textual content types such as JSON, XML, GraphQL, form payloads, and `text/*` responses, and body capture is truncated to the configured limits. Sensitive query-string and payload fields can also be redacted before the log event is written, including JSON, form, and header-style plain-text key/value content.

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

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-redactedfieldnames"></a>

##### `RedactedFieldNames`

```csharp
IReadOnlyList<string> RedactedFieldNames { get; set; }
```

Gets or sets the field names that should be treated as sensitive when request and response content is logged.

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-redactionvalue"></a>

##### `RedactionValue`

```csharp
string RedactionValue { get; set; }
```

Gets or sets the placeholder written to logs when a sensitive value is redacted.

<a id="member-p-cephalon-aspnetcore-hosting-httprequestresponseloggingoptions-redactsensitivevalues"></a>

##### `RedactSensitiveValues`

```csharp
bool RedactSensitiveValues { get; set; }
```

Gets or sets a value indicating whether known-sensitive query-string and payload fields should be redacted before logging.

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

<a id="type-cephalon-aspnetcore-hosting-knowledgequeryhttprequest"></a>

### `KnowledgeQueryHttpRequest`

Represents the operator HTTP request body used to query a knowledge collection.

#### Declaration
```csharp
public sealed class KnowledgeQueryHttpRequest
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-knowledgequeryhttprequest-ctor"></a>

##### `KnowledgeQueryHttpRequest`

```csharp
KnowledgeQueryHttpRequest()
```

Initializes a new instance of the `KnowledgeQueryHttpRequest` class.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-knowledgequeryhttprequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; set; }
```

Gets or initializes the actor identifier responsible for the query.

<a id="member-p-cephalon-aspnetcore-hosting-knowledgequeryhttprequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or initializes the correlation identifier for the query.

<a id="member-p-cephalon-aspnetcore-hosting-knowledgequeryhttprequest-maxresults"></a>

##### `MaxResults`

```csharp
int? MaxResults { get; set; }
```

Gets or initializes the maximum number of results to return.

<a id="member-p-cephalon-aspnetcore-hosting-knowledgequeryhttprequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Gets or initializes metadata to attach to the query.

<a id="member-p-cephalon-aspnetcore-hosting-knowledgequeryhttprequest-querytext"></a>

##### `QueryText`

```csharp
string QueryText { get; set; }
```

Gets or initializes the query text.

<a id="type-cephalon-aspnetcore-hosting-restapigovernanceoptions"></a>

### `RestApiGovernanceOptions`

Configures host-level governance for public REST endpoint publication in Cephalon ASP.NET Core hosts.

Remarks: These settings describe host-level publication governance for module-owned REST shorthand paths. They intentionally stay out of the engine core because they govern the ASP.NET Core public REST surface.

#### Declaration
```csharp
public sealed class RestApiGovernanceOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-restapigovernanceoptions-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointpublicationgroupauthoringpolicydescriptor-system-collections-generic-ireadonlylist-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-system-collections-generic-ireadonlylist-cephalon-aspnetcore-hosting-restendpointoverrideoptions"></a>

##### `RestApiGovernanceOptions`

```csharp
RestApiGovernanceOptions(IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicyDescriptor> authoringPolicies, IReadOnlyList<RestEndpointSuppressionOptions> suppressions, IReadOnlyList<RestEndpointOverrideOptions> overrides)
```

Initializes a new instance of the `RestApiGovernanceOptions` class.

Parameters:
- `authoringPolicies`: The configured behavior-level authoring policies for REST publication groups.
- `suppressions`: The configured suppression rules for shorthand REST candidates.
- `overrides`: The configured override rules for shorthand REST candidates.

#### Fields

<a id="member-f-cephalon-aspnetcore-hosting-restapigovernanceoptions-sectionname"></a>

##### `SectionName`

```csharp
const string SectionName
```

Gets the root configuration section used for REST API governance settings.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-restapigovernanceoptions-authoringpolicies"></a>

##### `AuthoringPolicies`

```csharp
IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicyDescriptor> AuthoringPolicies { get; }
```

Gets the configured behavior-level authoring policies for REST publication groups.

<a id="member-p-cephalon-aspnetcore-hosting-restapigovernanceoptions-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any REST governance values were explicitly supplied.

<a id="member-p-cephalon-aspnetcore-hosting-restapigovernanceoptions-overrides"></a>

##### `Overrides`

```csharp
IReadOnlyList<RestEndpointOverrideOptions> Overrides { get; }
```

Gets the configured override rules for descriptor-backed REST shorthand candidates.

<a id="member-p-cephalon-aspnetcore-hosting-restapigovernanceoptions-suppressions"></a>

##### `Suppressions`

```csharp
IReadOnlyList<RestEndpointSuppressionOptions> Suppressions { get; }
```

Gets the configured suppression rules for descriptor-backed REST shorthand candidates.

#### Methods

<a id="member-m-cephalon-aspnetcore-hosting-restapigovernanceoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
RestApiGovernanceOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds and normalizes REST governance settings from configuration.

Returns: The normalized REST governance settings.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path to bind.

<a id="type-cephalon-aspnetcore-hosting-restendpointoverrideoptions"></a>

### `RestEndpointOverrideOptions`

Describes one host-level override rule for descriptor-backed REST shorthand candidates.

#### Declaration
```csharp
public sealed class RestEndpointOverrideOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-restendpointoverrideoptions-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-nullable-system-int32-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-collections-generic-ireadonlylist-system-string-system-boolean-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-collections-generic-ireadonlylist-system-string-system-boolean-cephalon-abstractions-transports-restendpointoverridebindingmode-system-boolean-system-boolean-system-boolean-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingfallbackmode-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-boolean"></a>

##### `RestEndpointOverrideOptions`

```csharp
RestEndpointOverrideOptions(string id, IReadOnlyList<string> candidateIds, IReadOnlyList<string> behaviorIds, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<string> authoringStyles, IReadOnlyList<int> apiVersionMajors, IReadOnlyList<string> methods, IReadOnlyList<string> relativePatterns, IReadOnlyList<string> routeGroupPrefixes, int? apiVersionMajor, string method, string pattern, string routeGroupPrefix, string openApiDocumentName, string tagName, string endpointName, string summary, string description, string requiredCapabilityKey, bool clearRequiredCapability, IReadOnlyList<string> requiredFeatureFlagIds, bool clearRequiredFeatureFlags, IReadOnlyList<RestEndpointBindingDescriptor> bindings, IReadOnlyList<string> removedBindingProperties, bool clearBindings, RestEndpointOverrideBindingMode bindingMode, bool clearEndpointName, bool clearSummary, bool clearDescription, IReadOnlyList<string> openApiDocumentNames, IReadOnlyList<string> tagNames, IReadOnlyList<string> endpointNames, IReadOnlyList<RestEndpointBindingFallbackMode> bindingFallbackModes, IReadOnlyList<RestEndpointBindingDescriptor> targetBindings, IReadOnlyList<string> hostGovernanceScopes, IReadOnlyList<string> behaviorIdPrefixes, bool preserveImplicitQueryFallback)
```

Initializes a new instance of the `RestEndpointOverrideOptions` class.

Parameters:
- `id`: The stable override identifier.
- `candidateIds`: The original shorthand candidate identifiers targeted by the override rule.
- `behaviorIds`: The behavior identifiers targeted by the override rule.
- `sourceModuleIds`: The source-module identifiers targeted by the override rule.
- `authoringStyles`: The module-owned REST authoring styles targeted by the override rule. When omitted, the rule targets only shorthand styles `behavior-module-profile` and `behavior-module-generated`. Explicit `behavior-module-dsl` routes participate only when the owning route group opted into host governance.
- `apiVersionMajors`: The effective API major versions targeted by the override rule before any override actions are applied.
- `methods`: The effective HTTP methods targeted by the override rule before any override actions are applied.
- `relativePatterns`: The shorthand relative route patterns targeted by the override rule before any override actions are applied.
- `routeGroupPrefixes`: The published route-group prefixes targeted by the override rule before any override actions are applied.
- `apiVersionMajor`: The effective API major version applied when the rule matches a shorthand candidate.
- `method`: The effective HTTP method applied when the rule matches a shorthand candidate.
- `pattern`: The effective relative route pattern applied when the rule matches a shorthand candidate.
- `routeGroupPrefix`: The effective published route-group prefix applied when the rule matches a shorthand candidate.
- `openApiDocumentName`: The effective OpenAPI document name applied when the rule matches a shorthand candidate.
- `tagName`: The effective primary OpenAPI tag name applied when the rule matches a shorthand candidate.
- `endpointName`: The effective endpoint name applied when the rule matches a shorthand candidate.
- `summary`: The effective OpenAPI summary applied when the rule matches a shorthand candidate.
- `description`: The effective OpenAPI description applied when the rule matches a shorthand candidate.
- `requiredCapabilityKey`: The required Cephalon capability key enforced at the REST boundary when the rule matches a shorthand candidate.
- `clearRequiredCapability`: `true` when the rule removes any previously declared Cephalon capability boundary from the matched shorthand candidate.
- `requiredFeatureFlagIds`: The required Cephalon feature-flag identifiers enforced at the REST boundary when the rule matches a shorthand candidate.
- `clearRequiredFeatureFlags`: `true` when the rule removes any previously declared Cephalon feature-flag requirements from the matched shorthand candidate.
- `bindings`: The effective explicit request-binding plan applied when the rule matches a shorthand candidate.
- `removedBindingProperties`: The explicit shorthand binding properties removed from the source binding plan when the rule matches.
- `clearBindings`: `true` when the rule removes the matched shorthand candidate's entire explicit binding plan and returns publication to the implicit request-binding baseline.
- `bindingMode`: The mode used to apply `bindings` and `removedBindingProperties` to the shorthand candidate's explicit binding plan.
- `clearEndpointName`: `true` when the rule removes any previously declared shorthand endpoint name from the matched candidate.
- `clearSummary`: `true` when the rule removes any previously declared shorthand endpoint summary from the matched candidate.
- `clearDescription`: `true` when the rule removes any previously declared shorthand endpoint description from the matched candidate.
- `openApiDocumentNames`: The original shorthand OpenAPI document names targeted by the override rule before any override actions are applied.
- `tagNames`: The original shorthand primary OpenAPI tag names targeted by the override rule before any override actions are applied.
- `endpointNames`: The original shorthand endpoint names targeted by the override rule before any override actions are applied.
- `hostGovernanceScopes`: The original shorthand host-governance scopes targeted by the override rule before any override actions are applied. This selector can also serve as the rule's primary target when candidate, behavior, and source-module identifiers are intentionally omitted.
- `bindingFallbackModes`: The original shorthand request-binding fallback modes targeted by the override rule before any override actions are applied.
- `targetBindings`: The original shorthand explicit binding descriptors targeted by the override rule before any override actions are applied.
- `behaviorIdPrefixes`: The behavior-id prefixes targeted by the override rule. Prefix matches use the stable dot-separated behavior-id hierarchy, so a prefix targets the exact behavior id and any descendant behavior ids beneath that prefix.
- `preserveImplicitQueryFallback`: `true` when the rule opts the matched explicit-binding shorthand candidate into preserved implicit-query fallback for any remaining unbound query properties.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-actionkinds"></a>

##### `ActionKinds`

```csharp
IReadOnlyList<RestEndpointOverrideActionKind> ActionKinds { get; }
```

Gets the normalized action dimensions declared by this override rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-apiversionmajor"></a>

##### `ApiVersionMajor`

```csharp
int? ApiVersionMajor { get; }
```

Gets the effective API major version applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-apiversionmajors"></a>

##### `ApiVersionMajors`

```csharp
IReadOnlyList<int> ApiVersionMajors { get; }
```

Gets the effective API major versions targeted by this override rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-authoringstyles"></a>

##### `AuthoringStyles`

```csharp
IReadOnlyList<string> AuthoringStyles { get; }
```

Gets the normalized shorthand authoring styles targeted by this override rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-behavioridprefixes"></a>

##### `BehaviorIdPrefixes`

```csharp
IReadOnlyList<string> BehaviorIdPrefixes { get; }
```

Gets the behavior-id prefixes targeted by this override rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; }
```

Gets the behavior identifiers targeted by this override rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-bindingfallbackmodes"></a>

##### `BindingFallbackModes`

```csharp
IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }
```

Gets the original shorthand request-binding fallback modes targeted by this override rule before any override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-bindingmode"></a>

##### `BindingMode`

```csharp
RestEndpointOverrideBindingMode BindingMode { get; }
```

Gets how `Bindings` and `RemovedBindingProperties` apply to the shorthand candidate's explicit binding plan.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-bindings"></a>

##### `Bindings`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> Bindings { get; }
```

Gets the effective explicit request-binding plan applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the original shorthand candidate identifiers targeted by this override rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-clearbindings"></a>

##### `ClearBindings`

```csharp
bool ClearBindings { get; }
```

Gets a value indicating whether this override rule clears the matched shorthand candidate's entire explicit binding plan.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-cleardescription"></a>

##### `ClearDescription`

```csharp
bool ClearDescription { get; }
```

Gets a value indicating whether this override rule clears any previously declared endpoint description from the matched shorthand candidate.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-clearendpointname"></a>

##### `ClearEndpointName`

```csharp
bool ClearEndpointName { get; }
```

Gets a value indicating whether this override rule clears any previously declared endpoint name from the matched shorthand candidate.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-clearrequiredcapability"></a>

##### `ClearRequiredCapability`

```csharp
bool ClearRequiredCapability { get; }
```

Gets a value indicating whether this override rule clears any previously declared Cephalon capability boundary from the matched shorthand candidate.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-clearrequiredfeatureflags"></a>

##### `ClearRequiredFeatureFlags`

```csharp
bool ClearRequiredFeatureFlags { get; }
```

Gets a value indicating whether this override rule clears any previously declared Cephalon feature-flag requirements from the matched shorthand candidate.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-clearsummary"></a>

##### `ClearSummary`

```csharp
bool ClearSummary { get; }
```

Gets a value indicating whether this override rule clears any previously declared endpoint summary from the matched shorthand candidate.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the effective OpenAPI description applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-endpointname"></a>

##### `EndpointName`

```csharp
string EndpointName { get; }
```

Gets the effective endpoint name applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-endpointnames"></a>

##### `EndpointNames`

```csharp
IReadOnlyList<string> EndpointNames { get; }
```

Gets the original shorthand endpoint names targeted by this override rule before any override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any targeting values or override actions were explicitly supplied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-hostgovernancescopes"></a>

##### `HostGovernanceScopes`

```csharp
IReadOnlyList<string> HostGovernanceScopes { get; }
```

Gets the original shorthand host-governance scopes targeted by this override rule before any override actions are applied. These scopes can also act as the rule's primary target.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable override identifier.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-method"></a>

##### `Method`

```csharp
string Method { get; }
```

Gets the effective HTTP method applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the effective HTTP methods targeted by this override rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-openapidocumentname"></a>

##### `OpenApiDocumentName`

```csharp
string OpenApiDocumentName { get; }
```

Gets the effective OpenAPI document name applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-openapidocumentnames"></a>

##### `OpenApiDocumentNames`

```csharp
IReadOnlyList<string> OpenApiDocumentNames { get; }
```

Gets the original shorthand OpenAPI document names targeted by this override rule before any override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-pattern"></a>

##### `Pattern`

```csharp
string Pattern { get; }
```

Gets the effective relative route pattern applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-preserveimplicitqueryfallback"></a>

##### `PreserveImplicitQueryFallback`

```csharp
bool PreserveImplicitQueryFallback { get; }
```

Gets a value indicating whether this override rule opts the matched explicit-binding shorthand candidate into preserved implicit-query fallback for remaining unbound query properties.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-relativepatterns"></a>

##### `RelativePatterns`

```csharp
IReadOnlyList<string> RelativePatterns { get; }
```

Gets the shorthand relative route patterns targeted by this override rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-removedbindingproperties"></a>

##### `RemovedBindingProperties`

```csharp
IReadOnlyList<string> RemovedBindingProperties { get; }
```

Gets the explicit shorthand binding properties removed from the source binding plan when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-requiredcapabilitykey"></a>

##### `RequiredCapabilityKey`

```csharp
string RequiredCapabilityKey { get; }
```

Gets the required Cephalon capability key enforced at the REST boundary when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-requiredfeatureflagids"></a>

##### `RequiredFeatureFlagIds`

```csharp
IReadOnlyList<string> RequiredFeatureFlagIds { get; }
```

Gets the required Cephalon feature-flag identifiers enforced at the REST boundary when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-routegroupprefix"></a>

##### `RouteGroupPrefix`

```csharp
string RouteGroupPrefix { get; }
```

Gets the effective published route-group prefix applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-routegroupprefixes"></a>

##### `RouteGroupPrefixes`

```csharp
IReadOnlyList<string> RouteGroupPrefixes { get; }
```

Gets the published route-group prefixes targeted by this override rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the source-module identifiers targeted by this override rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-summary"></a>

##### `Summary`

```csharp
string Summary { get; }
```

Gets the effective OpenAPI summary applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-tagname"></a>

##### `TagName`

```csharp
string TagName { get; }
```

Gets the effective primary OpenAPI tag name applied when this override rule matches.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-tagnames"></a>

##### `TagNames`

```csharp
IReadOnlyList<string> TagNames { get; }
```

Gets the original shorthand primary OpenAPI tag names targeted by this override rule before any override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointoverrideoptions-targetbindings"></a>

##### `TargetBindings`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }
```

Gets the original shorthand explicit binding descriptors targeted by this override rule before any override actions are applied.

<a id="type-cephalon-aspnetcore-hosting-restendpointsuppressionoptions"></a>

### `RestEndpointSuppressionOptions`

Describes one host-level suppression rule for descriptor-backed REST shorthand candidates.

#### Declaration
```csharp
public sealed class RestEndpointSuppressionOptions
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-int32-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingfallbackmode-system-collections-generic-ireadonlylist-cephalon-abstractions-transports-restendpointbindingdescriptor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `RestEndpointSuppressionOptions`

```csharp
RestEndpointSuppressionOptions(string id, IReadOnlyList<string> candidateIds, IReadOnlyList<string> behaviorIds, IReadOnlyList<string> sourceModuleIds, IReadOnlyList<string> authoringStyles, IReadOnlyList<int> apiVersionMajors, IReadOnlyList<string> methods, IReadOnlyList<string> relativePatterns, IReadOnlyList<string> routeGroupPrefixes, IReadOnlyList<string> openApiDocumentNames, IReadOnlyList<string> tagNames, IReadOnlyList<string> endpointNames, IReadOnlyList<RestEndpointBindingFallbackMode> bindingFallbackModes, IReadOnlyList<RestEndpointBindingDescriptor> targetBindings, IReadOnlyList<string> hostGovernanceScopes, IReadOnlyList<string> behaviorIdPrefixes)
```

Initializes a new instance of the `RestEndpointSuppressionOptions` class.

Parameters:
- `id`: The stable suppression identifier.
- `candidateIds`: The original shorthand candidate identifiers targeted by the suppression rule.
- `behaviorIds`: The behavior identifiers targeted by the suppression rule.
- `sourceModuleIds`: The source-module identifiers targeted by the suppression rule.
- `authoringStyles`: The module-owned REST authoring styles targeted by the suppression rule. When omitted, the rule targets only shorthand styles `behavior-module-profile` and `behavior-module-generated`. Explicit `behavior-module-dsl` routes participate only when the owning route group opted into host governance.
- `apiVersionMajors`: The effective API major versions targeted by the suppression rule before any override actions are applied.
- `methods`: The effective HTTP methods targeted by the suppression rule before any override actions are applied.
- `relativePatterns`: The shorthand relative route patterns targeted by the suppression rule before any override actions are applied.
- `routeGroupPrefixes`: The published route-group prefixes targeted by the suppression rule before any override actions are applied.
- `openApiDocumentNames`: The original shorthand OpenAPI document names targeted by the suppression rule before any override actions are applied.
- `tagNames`: The original shorthand primary OpenAPI tag names targeted by the suppression rule before any override actions are applied.
- `endpointNames`: The original shorthand endpoint names targeted by the suppression rule before any override actions are applied.
- `hostGovernanceScopes`: The original shorthand host-governance scopes targeted by the suppression rule before any override actions are applied. This selector can also serve as the rule's primary target when candidate, behavior, and source-module identifiers are intentionally omitted.
- `bindingFallbackModes`: The original shorthand request-binding fallback modes targeted by the suppression rule before any override actions are applied.
- `targetBindings`: The original shorthand explicit binding descriptors targeted by the suppression rule before any override actions are applied.
- `behaviorIdPrefixes`: The behavior-id prefixes targeted by the suppression rule. Prefix matches use the stable dot-separated behavior-id hierarchy, so a prefix targets the exact behavior id and any descendant behavior ids beneath that prefix.

#### Properties

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-apiversionmajors"></a>

##### `ApiVersionMajors`

```csharp
IReadOnlyList<int> ApiVersionMajors { get; }
```

Gets the effective API major versions targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-authoringstyles"></a>

##### `AuthoringStyles`

```csharp
IReadOnlyList<string> AuthoringStyles { get; }
```

Gets the normalized shorthand authoring styles targeted by this suppression rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-behavioridprefixes"></a>

##### `BehaviorIdPrefixes`

```csharp
IReadOnlyList<string> BehaviorIdPrefixes { get; }
```

Gets the behavior-id prefixes targeted by this suppression rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-behaviorids"></a>

##### `BehaviorIds`

```csharp
IReadOnlyList<string> BehaviorIds { get; }
```

Gets the behavior identifiers targeted by this suppression rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-bindingfallbackmodes"></a>

##### `BindingFallbackModes`

```csharp
IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }
```

Gets the original shorthand request-binding fallback modes targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-candidateids"></a>

##### `CandidateIds`

```csharp
IReadOnlyList<string> CandidateIds { get; }
```

Gets the original shorthand candidate identifiers targeted by this suppression rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-endpointnames"></a>

##### `EndpointNames`

```csharp
IReadOnlyList<string> EndpointNames { get; }
```

Gets the original shorthand endpoint names targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any targeting values were explicitly supplied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-hostgovernancescopes"></a>

##### `HostGovernanceScopes`

```csharp
IReadOnlyList<string> HostGovernanceScopes { get; }
```

Gets the original shorthand host-governance scopes targeted by this suppression rule before override actions are applied. These scopes can also act as the rule's primary target.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable suppression identifier.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-methods"></a>

##### `Methods`

```csharp
IReadOnlyList<string> Methods { get; }
```

Gets the effective HTTP methods targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-openapidocumentnames"></a>

##### `OpenApiDocumentNames`

```csharp
IReadOnlyList<string> OpenApiDocumentNames { get; }
```

Gets the original shorthand OpenAPI document names targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-relativepatterns"></a>

##### `RelativePatterns`

```csharp
IReadOnlyList<string> RelativePatterns { get; }
```

Gets the shorthand relative route patterns targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-routegroupprefixes"></a>

##### `RouteGroupPrefixes`

```csharp
IReadOnlyList<string> RouteGroupPrefixes { get; }
```

Gets the published route-group prefixes targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-sourcemoduleids"></a>

##### `SourceModuleIds`

```csharp
IReadOnlyList<string> SourceModuleIds { get; }
```

Gets the source-module identifiers targeted by this suppression rule.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-tagnames"></a>

##### `TagNames`

```csharp
IReadOnlyList<string> TagNames { get; }
```

Gets the original shorthand primary OpenAPI tag names targeted by this suppression rule before override actions are applied.

<a id="member-p-cephalon-aspnetcore-hosting-restendpointsuppressionoptions-targetbindings"></a>

##### `TargetBindings`

```csharp
IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }
```

Gets the original shorthand explicit binding descriptors targeted by this suppression rule before override actions are applied.

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

<a id="member-m-cephalon-aspnetcore-transports-rest-restendpointconventionbuilderextensions-clearrequiredcapability-microsoft-aspnetcore-builder-routehandlerbuilder"></a>

##### `ClearRequiredCapability`

```csharp
RouteHandlerBuilder ClearRequiredCapability(this RouteHandlerBuilder builder)
```

Clears any previously declared Cephalon capability decision from a REST endpoint.

Remarks: This uses the same last-declaration-wins model as `RequireCapability`. A later clear declaration suppresses earlier capability requirements for the same route.

Returns: The same route handler builder for further convention chaining.

Parameters:
- `builder`: The route handler builder to update.

<a id="member-m-cephalon-aspnetcore-transports-rest-restendpointconventionbuilderextensions-clearrequiredfeatureflags-microsoft-aspnetcore-builder-routehandlerbuilder"></a>

##### `ClearRequiredFeatureFlags`

```csharp
RouteHandlerBuilder ClearRequiredFeatureFlags(this RouteHandlerBuilder builder)
```

Clears any previously declared Cephalon feature-flag requirements from a REST endpoint.

Remarks: This uses the same last-declaration-wins model as `RequireFeatureFlags`. A later clear declaration suppresses earlier feature requirements for the same route.

Returns: The same route handler builder for further convention chaining.

Parameters:
- `builder`: The route handler builder to update.

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

<a id="member-m-cephalon-aspnetcore-transports-rest-restendpointconventionbuilderextensions-requirefeatureflag-microsoft-aspnetcore-builder-routehandlerbuilder-system-string"></a>

##### `RequireFeatureFlag`

```csharp
RouteHandlerBuilder RequireFeatureFlag(this RouteHandlerBuilder builder, string featureFlagId)
```

Requires one Cephalon feature flag to be enabled before a REST endpoint can execute.

Returns: The same route handler builder for further convention chaining.

Parameters:
- `builder`: The route handler builder to protect.
- `featureFlagId`: The feature-flag identifier that must resolve to enabled.

<a id="member-m-cephalon-aspnetcore-transports-rest-restendpointconventionbuilderextensions-requirefeatureflags-microsoft-aspnetcore-builder-routehandlerbuilder-system-string"></a>

##### `RequireFeatureFlags`

```csharp
RouteHandlerBuilder RequireFeatureFlags(this RouteHandlerBuilder builder, string[] featureFlagIds)
```

Requires all requested Cephalon feature flags to be enabled before a REST endpoint can execute.

Remarks: This guard keeps the endpoint published and introspectable while shifting rollout decisions to runtime evaluation at the HTTP boundary. If any required feature flag is unavailable for the request context, the endpoint returns a `404 Not Found` problem response.

Returns: The same route handler builder for further convention chaining.

Parameters:
- `builder`: The route handler builder to protect.
- `featureFlagIds`: The feature-flag identifiers that must resolve to enabled.

<a id="type-cephalon-aspnetcore-transports-rest-resultmodelenveloperesponsemetadata"></a>

### `ResultModelEnvelopeResponseMetadata`

Describes a response whose OpenAPI schema should be published through the Cephalon result envelope.

Remarks: Runtime adapters can attach this metadata when the wire response uses `ResultModel<T>` but endpoint metadata should avoid constructing closed generic result-envelope types at runtime.

#### Declaration
```csharp
public sealed class ResultModelEnvelopeResponseMetadata
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-transports-rest-resultmodelenveloperesponsemetadata-ctor-system-int32-system-type-system-boolean"></a>

##### `ResultModelEnvelopeResponseMetadata`

```csharp
ResultModelEnvelopeResponseMetadata(int statusCode, Type payloadType, bool isError)
```

Initializes a new instance of the `ResultModelEnvelopeResponseMetadata` class.

Parameters:
- `statusCode`: The HTTP status code described by this response metadata.
- `payloadType`: The payload type carried in the envelope `data` property.
- `isError`: Whether the response represents an error envelope.

#### Properties

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelenveloperesponsemetadata-iserror"></a>

##### `IsError`

```csharp
bool IsError { get; }
```

Gets a value indicating whether the response represents an error envelope.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelenveloperesponsemetadata-payloadtype"></a>

##### `PayloadType`

```csharp
Type PayloadType { get; }
```

Gets the payload type carried in the envelope `data` property.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelenveloperesponsemetadata-statuscode"></a>

##### `StatusCode`

```csharp
int StatusCode { get; }
```

Gets the HTTP status code described by this response metadata.

<a id="type-cephalon-aspnetcore-transports-rest-resultmodelerror"></a>

### `ResultModelError`

Represents the optional Cephalon REST error envelope projected by the ASP.NET Core adapter.

#### Declaration
```csharp
public sealed class ResultModelError
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-transports-rest-resultmodelerror-ctor"></a>

##### `ResultModelError`

```csharp
ResultModelError()
```

Initializes a new instance of the `ResultModelError` class.

<a id="type-cephalon-aspnetcore-transports-rest-resultmodelerrordetail"></a>

### `ResultModelErrorDetail`

Represents structured error details inside a `ResultModel<T>`.

#### Declaration
```csharp
public sealed class ResultModelErrorDetail
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-transports-rest-resultmodelerrordetail-ctor"></a>

##### `ResultModelErrorDetail`

```csharp
ResultModelErrorDetail()
```

Initializes a new instance of the `ResultModelErrorDetail` class.

#### Properties

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelerrordetail-details"></a>

##### `Details`

```csharp
string Details { get; set; }
```

Gets or sets additional error details when one was supplied.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelerrordetail-key"></a>

##### `Key`

```csharp
string Key { get; set; }
```

Gets or sets the stable error key.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelerrordetail-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

Gets or sets the human-readable error message.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodelerrordetail-severity"></a>

##### `Severity`

```csharp
BehaviorFaultSeverity Severity { get; set; }
```

Gets or sets the error severity.

<a id="type-cephalon-aspnetcore-transports-rest-resultmodel-tmodel"></a>

### `ResultModel<TModel>`

Represents the optional Cephalon REST success envelope projected by the ASP.NET Core adapter.

#### Declaration
```csharp
public class ResultModel<TModel>
```

#### Constructors

<a id="member-m-cephalon-aspnetcore-transports-rest-resultmodel-1-ctor"></a>

##### `ResultModel<TModel>`

```csharp
ResultModel<TModel>()
```

Initializes a new instance of the `ResultModel<T>` class.

#### Properties

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-data"></a>

##### `Data`

```csharp
TModel Data { get; set; }
```

Gets or sets the payload returned by the endpoint.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-errors"></a>

##### `Errors`

```csharp
List<ResultModelErrorDetail> Errors { get; set; }
```

Gets or sets the structured error details when the response is not successful.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

Gets or sets the human-readable response message.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-statuscode"></a>

##### `StatusCode`

```csharp
int StatusCode { get; set; }
```

Gets or sets the effective HTTP status code associated with the response.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-success"></a>

##### `Success`

```csharp
bool Success { get; set; }
```

Gets or sets a value indicating whether the response is successful.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-title"></a>

##### `Title`

```csharp
string Title { get; set; }
```

Gets or sets the short response title.

<a id="member-p-cephalon-aspnetcore-transports-rest-resultmodel-1-type"></a>

##### `Type`

```csharp
string Type { get; set; }
```

Gets or sets the optional problem type URI associated with the response.

Remarks: Success envelopes omit this value by default. Error envelopes derive the RFC problem type from `StatusCode` unless a host or mapper supplies a more specific URI.

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
