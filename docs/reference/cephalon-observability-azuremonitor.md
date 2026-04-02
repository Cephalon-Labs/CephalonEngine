# Cephalon.Observability.AzureMonitor

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.AzureMonitor)
## Namespaces

- `Cephalon.Observability.AzureMonitor.Configuration`
- `Cephalon.Observability.AzureMonitor.Hosting`

<a id="namespace-cephalon-observability-azuremonitor-configuration"></a>

## Namespace Cephalon.Observability.AzureMonitor.Configuration

<a id="type-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions"></a>

### `AzureMonitorExportOptions`

Configures Azure Monitor exporter wiring on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class AzureMonitorExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions-ctor"></a>

##### `AzureMonitorExportOptions`

```csharp
AzureMonitorExportOptions()
```

Initializes a new instance of the `AzureMonitorExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the Azure Monitor / Application Insights connection string used by the exporter.

<a id="member-p-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the hosted Azure platform whose default resource attributes should be applied.

Remarks: Supported values are `appservice`, `functions`, `aks`, `containerapps`, and `vm`. The package maps them to the current OpenTelemetry `cloud.platform` attribute values.

<a id="member-p-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions-usedefaultazurecredential"></a>

##### `UseDefaultAzureCredential`

```csharp
bool UseDefaultAzureCredential { get; set; }
```

Gets or sets a value indicating whether the exporter should authenticate with `DefaultAzureCredential`.

Remarks: The connection string still determines the Azure Monitor resource endpoint. This flag only adds Azure Active Directory authentication on top of that endpoint selection.

#### Methods

<a id="member-m-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
AzureMonitorExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Azure Monitor export options from configuration.

Returns: The bound Azure Monitor export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-azuremonitor-hosting"></a>

## Namespace Cephalon.Observability.AzureMonitor.Hosting

<a id="type-cephalon-observability-azuremonitor-hosting-azuremonitorhostapplicationbuilderextensions"></a>

### `AzureMonitorHostApplicationBuilderExtensions`

Adds Azure Monitor exporter wiring for Cephalon hosts.

#### Declaration
```csharp
public static class AzureMonitorHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-azuremonitor-hosting-azuremonitorhostapplicationbuilderextensions-addcephalonazuremonitor-1-0-system-action-cephalon-observability-azuremonitor-configuration-azuremonitorexportoptions"></a>

##### `AddCephalonAzureMonitor`

```csharp
TBuilder AddCephalonAzureMonitor<TBuilder>(this TBuilder builder, Action<AzureMonitorExportOptions> configure)
```

Adds Azure Monitor exporter registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps Azure-specific exporter wiring outside `Cephalon.Engine` and `Cephalon.Observability`. Hosts opt in explicitly when they want Azure Monitor / Application Insights export over the same shared `ILogger` and OpenTelemetry baseline.

Registration is skipped when every signal is disabled or when no Azure Monitor connection string is configured. When `HostedPlatform` is supplied, the package adds Azure-specific `cloud.provider`, `cloud.platform`, and `deployment.environment.name` resource attributes on top of the existing service-name and service-version defaults.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Azure Monitor export options.
