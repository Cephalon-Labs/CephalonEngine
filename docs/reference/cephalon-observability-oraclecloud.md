# Cephalon.Observability.OracleCloud

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.OracleCloud)
## Namespaces

- `Cephalon.Observability.OracleCloud.Configuration`
- `Cephalon.Observability.OracleCloud.Hosting`

<a id="namespace-cephalon-observability-oraclecloud-configuration"></a>

## Namespace Cephalon.Observability.OracleCloud.Configuration

<a id="type-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions"></a>

### `OracleCloudTelemetryExportOptions`

Configures Oracle Cloud observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class OracleCloudTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-ctor"></a>

##### `OracleCloudTelemetryExportOptions`

```csharp
OracleCloudTelemetryExportOptions()
```

Initializes a new instance of the `OracleCloudTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-datauploadendpoint"></a>

##### `DataUploadEndpoint`

```csharp
string DataUploadEndpoint { get; set; }
```

Gets or sets the Oracle Cloud APM data upload endpoint used to build direct managed OTLP/HTTP ingestion URLs.

Remarks: This value should be the Oracle Cloud APM `DataUploadEndpoint` for the target domain. The package appends the documented OpenTelemetry signal paths when direct managed ingestion is enabled.

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the hosted Oracle Cloud platform whose default resource attributes should be applied.

Remarks: Supported values are `compute`, `oke`, and `functions`. The package maps them to the current OpenTelemetry `cloud.platform` attribute values.

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-metricsdatakey"></a>

##### `MetricsDataKey`

```csharp
string MetricsDataKey { get; set; }
```

Gets or sets the Oracle Cloud APM private metrics data key used for direct managed metrics ingestion.

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-region"></a>

##### `Region`

```csharp
string Region { get; set; }
```

Gets or sets the Oracle Cloud region to stamp onto exported resources when one should be explicit.

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-tracedatakey"></a>

##### `TraceDataKey`

```csharp
string TraceDataKey { get; set; }
```

Gets or sets the Oracle Cloud APM trace data key used for direct managed trace ingestion.

Remarks: When `UsePublicTraceDataKey` is `true`, this should be the public trace key. Otherwise it should be the private trace key.

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-usemanagedopentelemetryingestion"></a>

##### `UseManagedOpenTelemetryIngestion`

```csharp
bool UseManagedOpenTelemetryIngestion { get; set; }
```

Gets or sets a value indicating whether the package should use Oracle Cloud APM managed OpenTelemetry ingestion when no shared collector endpoint is configured.

Remarks: This direct managed path is intentionally limited to traces and metrics over OTLP/HTTP. Logs stay on the shared collector path, Oracle Log Analytics, or another runtime-specific route instead of being redirected implicitly.

<a id="member-p-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-usepublictracedatakey"></a>

##### `UsePublicTraceDataKey`

```csharp
bool UsePublicTraceDataKey { get; set; }
```

Gets or sets a value indicating whether trace ingestion should use the public Oracle Cloud APM data key path instead of the private data key path.

Remarks: Metrics always use the private-key path in Oracle Cloud APM.

#### Methods

<a id="member-m-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
OracleCloudTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Oracle Cloud telemetry export options from configuration.

Returns: The bound Oracle Cloud telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-oraclecloud-hosting"></a>

## Namespace Cephalon.Observability.OracleCloud.Hosting

<a id="type-cephalon-observability-oraclecloud-hosting-oraclecloudhostapplicationbuilderextensions"></a>

### `OracleCloudHostApplicationBuilderExtensions`

Adds Oracle Cloud-hosted observability defaults and optional Oracle Cloud APM managed ingestion for Cephalon hosts.

#### Declaration
```csharp
public static class OracleCloudHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-oraclecloud-hosting-oraclecloudhostapplicationbuilderextensions-addcephalonoraclecloud-1-0-system-action-cephalon-observability-oraclecloud-configuration-oraclecloudtelemetryexportoptions"></a>

##### `AddCephalonOracleCloud`

```csharp
TBuilder AddCephalonOracleCloud<TBuilder>(this TBuilder builder, Action<OracleCloudTelemetryExportOptions> configure)
```

Adds Oracle Cloud-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps Oracle Cloud-specific resource defaults and managed APM ingestion concerns outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers Oracle Cloud resource defaults on top. When those shared endpoint settings are absent and `UseManagedOpenTelemetryIngestion` is enabled, the package targets Oracle Cloud APM OTLP/HTTP ingestion for traces and metrics only. Logs stay on the shared collector path, Oracle Log Analytics, or another runtime-specific route instead of being redirected implicitly.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Oracle Cloud telemetry export options.
