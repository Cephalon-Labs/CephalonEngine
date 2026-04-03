# Cephalon.Observability

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability)
## Namespaces

- `Cephalon.Observability.Configuration`
- `Cephalon.Observability.Hosting`

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

Remarks: These settings let Cephalon packages, hosts, and downstream companion packages agree on provider, protocol, endpoint, and enabled signals without forcing exporter dependencies into the engine core. Companion packages such as `Cephalon.Observability.OpenTelemetry`, `Cephalon.Observability.AlibabaCloud`, `Cephalon.Observability.Aws`, `Cephalon.Observability.Gcp`, `Cephalon.Observability.DigitalOcean`, `Cephalon.Observability.GrafanaCloud`, `Cephalon.Observability.HuaweiCloud`, `Cephalon.Observability.OracleCloud`, `Cephalon.Observability.OpenShift`, `Cephalon.Observability.Tanzu`, or `Cephalon.Observability.AzureMonitor` can interpret the same contract when a host wants a supported export path, including the explicit self-hosted collector defaults that remain outside `Cephalon.Engine`. The same contract is also intended for developer-authored provider packages that need to layer Cloudflare, internal gateway, or other deployment-targeted auth, resource attributes, and hosted defaults on top of the existing `ILogger` pipeline.

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
