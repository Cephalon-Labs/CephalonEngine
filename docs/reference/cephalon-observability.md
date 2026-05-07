# Cephalon.Observability

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability)
## Namespaces

- `Cephalon.Observability.Configuration`
- `Cephalon.Observability.Hosting`
- `Cephalon.Observability.Runtime`

<a id="namespace-cephalon-observability-configuration"></a>

## Namespace Cephalon.Observability.Configuration

<a id="type-cephalon-observability-configuration-observabilityoptions"></a>

### `ObservabilityOptions`

Configures the built-in Cephalon observability package.

#### Declaration
```csharp
public sealed class ObservabilityOptions
```

#### Constructors

<a id="member-m-cephalon-observability-configuration-observabilityoptions-ctor"></a>

##### `ObservabilityOptions`

```csharp
ObservabilityOptions()
```

Creates observability options with the default startup diagnostics behavior.

#### Properties

<a id="member-p-cephalon-observability-configuration-observabilityoptions-logcapabilitysummary"></a>

##### `LogCapabilitySummary`

```csharp
bool LogCapabilitySummary { get; set; }
```

Gets or sets a value indicating whether a capability summary should be written at host startup.

<a id="member-p-cephalon-observability-configuration-observabilityoptions-logmanifestsummary"></a>

##### `LogManifestSummary`

```csharp
bool LogManifestSummary { get; set; }
```

Gets or sets a value indicating whether a manifest summary should be written at host startup.

<a id="member-p-cephalon-observability-configuration-observabilityoptions-logmodulesummary"></a>

##### `LogModuleSummary`

```csharp
bool LogModuleSummary { get; set; }
```

Gets or sets a value indicating whether a module summary should be written at host startup.

<a id="member-p-cephalon-observability-configuration-observabilityoptions-telemetry"></a>

##### `Telemetry`

```csharp
TelemetryExportOptions Telemetry { get; set; }
```

Gets or sets the telemetry export guidance associated with the host.

#### Methods

<a id="member-m-cephalon-observability-configuration-observabilityoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
ObservabilityOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds observability options from configuration.

Returns: The bound observability options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="type-cephalon-observability-configuration-telemetryexportoptions"></a>

### `TelemetryExportOptions`

Describes how operators intend host telemetry to be exported.

Remarks: These settings let Cephalon packages, hosts, and downstream companion packages agree on provider, protocol, endpoint, and enabled signals without forcing exporter dependencies into the engine core. Companion packages such as `Cephalon.Observability.OpenTelemetry`, `Cephalon.Observability.AlibabaCloud`, `Cephalon.Observability.Aws`, `Cephalon.Observability.Gcp`, `Cephalon.Observability.DigitalOcean`, `Cephalon.Observability.GrafanaCloud`, `Cephalon.Observability.HuaweiCloud`, `Cephalon.Observability.NewRelic`, `Cephalon.Observability.OracleCloud`, `Cephalon.Observability.OpenShift`, `Cephalon.Observability.Tanzu`, or `Cephalon.Observability.AzureMonitor` can interpret the same contract when a host wants a supported export path, including the explicit self-hosted collector defaults that remain outside `Cephalon.Engine`. The same contract is also intended for developer-authored provider packages that need to layer Cloudflare, internal gateway, or other deployment-targeted auth, resource attributes, and hosted defaults on top of the existing `ILogger` pipeline.

#### Declaration
```csharp
public sealed class TelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-configuration-telemetryexportoptions-ctor"></a>

##### `TelemetryExportOptions`

```csharp
TelemetryExportOptions()
```

Creates telemetry export options with the default guidance values.

#### Properties

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the target export endpoint, if one is configured. Companion packages interpret this as the base collector endpoint for the selected export protocol.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-exportlogs"></a>

##### `ExportLogs`

```csharp
bool ExportLogs { get; set; }
```

Gets or sets a value indicating whether logs should be exported.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-exportmetrics"></a>

##### `ExportMetrics`

```csharp
bool ExportMetrics { get; set; }
```

Gets or sets a value indicating whether metrics should be exported.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-exporttraces"></a>

##### `ExportTraces`

```csharp
bool ExportTraces { get; set; }
```

Gets or sets a value indicating whether traces should be exported.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-protocol"></a>

##### `Protocol`

```csharp
string Protocol { get; set; }
```

Gets or sets the telemetry transport protocol, such as `otlp`, `otlp/grpc`, or `otlp/http`.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-provider"></a>

##### `Provider`

```csharp
string Provider { get; set; }
```

Gets or sets the telemetry provider name, such as `OpenTelemetry`.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-useselfhosteddefaults"></a>

##### `UseSelfHostedDefaults`

```csharp
bool UseSelfHostedDefaults { get; set; }
```

Gets or sets a value indicating whether companion packages should apply the supported self-hosted collector and runtime defaults when the export endpoint is omitted.

Remarks: The shipped OpenTelemetry companion interprets this flag as an explicit self-hosted path on top of the shared OTLP baseline, using the standard local collector ports and host-managed runtime resource defaults instead of vendor-specific wiring.

<a id="namespace-cephalon-observability-hosting"></a>

## Namespace Cephalon.Observability.Hosting

<a id="type-cephalon-observability-hosting-observabilityservicecollectionextensions"></a>

### `ObservabilityServiceCollectionExtensions`

Adds the Cephalon observability package to an `IServiceCollection`.

#### Declaration
```csharp
public static class ObservabilityServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-hosting-observabilityservicecollectionextensions-addcephalonobservability-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-configuration-observabilityoptions"></a>

##### `AddCephalonObservability`

```csharp
IServiceCollection AddCephalonObservability(this IServiceCollection services, Action<ObservabilityOptions> configure)
```

Adds observability services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures observability options.

<a id="member-m-cephalon-observability-hosting-observabilityservicecollectionextensions-addcephalonobservability-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-configuration-observabilityoptions"></a>

##### `AddCephalonObservability`

```csharp
IServiceCollection AddCephalonObservability(this IServiceCollection services, IConfiguration configuration, Action<ObservabilityOptions> configure)
```

Adds observability services using configuration as the primary source of observability options.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven observability setup.

<a id="namespace-cephalon-observability-runtime"></a>

## Namespace Cephalon.Observability.Runtime

<a id="type-cephalon-observability-runtime-telemetryexportruntimesurfacefactory"></a>

### `TelemetryExportRuntimeSurfaceFactory`

Creates sanitized runtime-surface projections for Cephalon observability exporter companion packages.

Remarks: The generated metadata intentionally describes configured telemetry intent without exposing raw endpoints, headers, tokens, connection strings, or other deployment secrets.

#### Declaration
```csharp
public static class TelemetryExportRuntimeSurfaceFactory
```

#### Fields

<a id="member-f-cephalon-observability-runtime-telemetryexportruntimesurfacefactory-technologyid"></a>

##### `TechnologyId`

```csharp
const string TechnologyId
```

Gets the technology identifier used by Cephalon observability runtime surfaces.

#### Methods

<a id="member-m-cephalon-observability-runtime-telemetryexportruntimesurfacefactory-createbasemetadata-cephalon-observability-configuration-telemetryexportoptions"></a>

##### `CreateBaseMetadata`

```csharp
Dictionary<string, string> CreateBaseMetadata(TelemetryExportOptions telemetry)
```

Creates sanitized metadata from shared telemetry export options.

Returns: A mutable metadata dictionary with stable, non-secret telemetry intent values.

Parameters:
- `telemetry`: The shared telemetry export options to project.

<a id="member-m-cephalon-observability-runtime-telemetryexportruntimesurfacefactory-createsurface-system-string-system-string-system-string-system-string-system-string-system-string-cephalon-observability-configuration-telemetryexportoptions-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CreateSurface`

```csharp
TechnologyRuntimeSurface CreateSurface(string surfaceId, string displayName, string description, string entryId, string entryDisplayName, string entryDescription, TelemetryExportOptions telemetry, IReadOnlyDictionary<string, string> metadata)
```

Creates a telemetry-export runtime surface from shared telemetry export options.

Returns: A runtime surface that can be exposed through the technology runtime catalog.

Parameters:
- `surfaceId`: The stable surface identifier within the observability technology profile.
- `displayName`: The operator-facing display name for the surface.
- `description`: A human-readable description of the surface.
- `entryId`: The stable entry identifier for the active exporter or provider.
- `entryDisplayName`: The operator-facing display name for the entry.
- `entryDescription`: A human-readable description of the entry.
- `telemetry`: The shared telemetry export options to project.
- `metadata`: Additional sanitized metadata to merge into the entry.

<a id="member-m-cephalon-observability-runtime-telemetryexportruntimesurfacefactory-resolvesharedendpointmode-cephalon-observability-configuration-telemetryexportoptions"></a>

##### `ResolveSharedEndpointMode`

```csharp
string ResolveSharedEndpointMode(TelemetryExportOptions telemetry)
```

Resolves the shared telemetry endpoint mode without returning the configured endpoint value.

Returns: A stable mode string describing whether a shared endpoint or self-hosted defaults are active.

Parameters:
- `telemetry`: The shared telemetry export options to inspect.
