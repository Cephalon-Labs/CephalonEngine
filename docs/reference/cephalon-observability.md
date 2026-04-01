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

Remarks: These settings are intentionally guidance-oriented. They let Cephalon packages and hosts agree on provider, protocol, endpoint, and enabled signals without forcing a specific exporter implementation.

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

#### Properties

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the target export endpoint, if one is configured.

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

Gets or sets the telemetry transport protocol, such as `otlp`.

<a id="member-p-cephalon-observability-configuration-telemetryexportoptions-provider"></a>

##### `Provider`

```csharp
string Provider { get; set; }
```

Gets or sets the telemetry provider name, such as `OpenTelemetry`.

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

<a id="member-m-cephalon-observability-hosting-observabilityservicecollectionextensions-addcephalonobservability-microsoft-extensions-dependencyinjection-iservicecollection-system-action-1-cephalon-observability-configuration-observabilityoptions"></a>

##### `AddCephalonObservability`

```csharp
IServiceCollection AddCephalonObservability(this IServiceCollection services, Action<ObservabilityOptions> configure)
```

<a id="member-m-cephalon-observability-hosting-observabilityservicecollectionextensions-addcephalonobservability-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-1-cephalon-observability-configuration-observabilityoptions"></a>

##### `AddCephalonObservability`

```csharp
IServiceCollection AddCephalonObservability(this IServiceCollection services, IConfiguration configuration, Action<ObservabilityOptions> configure)
```
