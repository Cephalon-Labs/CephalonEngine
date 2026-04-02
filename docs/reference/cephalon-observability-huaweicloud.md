# Cephalon.Observability.HuaweiCloud

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.HuaweiCloud)
## Namespaces

- `Cephalon.Observability.HuaweiCloud.Configuration`
- `Cephalon.Observability.HuaweiCloud.Hosting`

<a id="namespace-cephalon-observability-huaweicloud-configuration"></a>

## Namespace Cephalon.Observability.HuaweiCloud.Configuration

<a id="type-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions"></a>

### `HuaweiCloudTelemetryExportOptions`

Configures Huawei Cloud observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class HuaweiCloudTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-ctor"></a>

##### `HuaweiCloudTelemetryExportOptions`

```csharp
HuaweiCloudTelemetryExportOptions()
```

Initializes a new instance of the `HuaweiCloudTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-apmendpoint"></a>

##### `ApmEndpoint`

```csharp
string ApmEndpoint { get; set; }
```

Gets or sets the Huawei Cloud APM OTLP endpoint used for direct managed trace ingestion.

Remarks: This endpoint is only used when `UseApmManagedTraceIngestion` is enabled and the shared `Engine:Observability:Telemetry:Endpoint` setting is omitted.

<a id="member-p-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-authenticationtoken"></a>

##### `AuthenticationToken`

```csharp
string AuthenticationToken { get; set; }
```

Gets or sets the authentication token written to the Huawei Cloud `Authentication` header for direct managed trace ingestion.

<a id="member-p-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the hosted Huawei Cloud platform whose default resource attributes should be applied.

Remarks: Supported values are `ecs`, `cce`, and `functiongraph`.

<a id="member-p-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-region"></a>

##### `Region`

```csharp
string Region { get; set; }
```

Gets or sets the Huawei Cloud region to stamp onto exported resources when one should be explicit.

<a id="member-p-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-useapmmanagedtraceingestion"></a>

##### `UseApmManagedTraceIngestion`

```csharp
bool UseApmManagedTraceIngestion { get; set; }
```

Gets or sets a value indicating whether the package should send traces directly to Huawei Cloud APM when no shared OTLP endpoint is configured.

Remarks: This direct path is intentionally trace-only. Logs and metrics stay on the shared collector path or on another runtime-specific route instead of being redirected implicitly.

#### Methods

<a id="member-m-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
HuaweiCloudTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Huawei Cloud telemetry export options from configuration.

Returns: The bound Huawei Cloud telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-huaweicloud-hosting"></a>

## Namespace Cephalon.Observability.HuaweiCloud.Hosting

<a id="type-cephalon-observability-huaweicloud-hosting-huaweicloudhostapplicationbuilderextensions"></a>

### `HuaweiCloudHostApplicationBuilderExtensions`

Adds Huawei Cloud-hosted observability defaults and optional managed APM trace ingestion for Cephalon hosts.

#### Declaration
```csharp
public static class HuaweiCloudHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-huaweicloud-hosting-huaweicloudhostapplicationbuilderextensions-addcephalonhuaweicloud-1-0-system-action-cephalon-observability-huaweicloud-configuration-huaweicloudtelemetryexportoptions"></a>

##### `AddCephalonHuaweiCloud`

```csharp
TBuilder AddCephalonHuaweiCloud<TBuilder>(this TBuilder builder, Action<HuaweiCloudTelemetryExportOptions> configure)
```

Adds Huawei Cloud-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps Huawei Cloud-specific resource defaults and managed APM trace-ingestion concerns outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers Huawei Cloud resource defaults on top. When those shared endpoint settings are absent and `UseApmManagedTraceIngestion` is enabled, the package targets the configured Huawei Cloud APM OTLP/gRPC endpoint for traces only by sending the configured `Authentication` header. Logs and metrics stay on the shared collector path or another runtime-specific path instead of being redirected implicitly.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Huawei Cloud telemetry export options.
