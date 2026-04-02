# Cephalon.Observability.AlibabaCloud

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.AlibabaCloud)
## Namespaces

- `Cephalon.Observability.AlibabaCloud.Configuration`
- `Cephalon.Observability.AlibabaCloud.Hosting`

<a id="namespace-cephalon-observability-alibabacloud-configuration"></a>

## Namespace Cephalon.Observability.AlibabaCloud.Configuration

<a id="type-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions"></a>

### `AlibabaCloudTelemetryExportOptions`

Configures Alibaba Cloud observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class AlibabaCloudTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-ctor"></a>

##### `AlibabaCloudTelemetryExportOptions`

```csharp
AlibabaCloudTelemetryExportOptions()
```

Initializes a new instance of the `AlibabaCloudTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-authenticationtoken"></a>

##### `AuthenticationToken`

```csharp
string AuthenticationToken { get; set; }
```

Gets or sets the authentication token written to the Alibaba Cloud `Authentication` header for OTLP/gRPC direct managed ingestion.

Remarks: This value is only used when `UseManagedOpenTelemetryIngestion` is enabled and the protocol stays on `otlp` or `otlp/grpc`. OTLP/HTTP managed ingestion expects the supplied signal-specific endpoints to already follow the provider's documented tokenized URL shape instead.

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the hosted Alibaba Cloud platform whose default resource attributes should be applied.

Remarks: Supported values are `ecs`, `fc` / `functioncompute`, and `openshift`. The package maps them to the current OpenTelemetry `cloud.platform` attribute values.

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-managedgrpcendpoint"></a>

##### `ManagedGrpcEndpoint`

```csharp
string ManagedGrpcEndpoint { get; set; }
```

Gets or sets the Alibaba Cloud Managed Service for OpenTelemetry OTLP/gRPC endpoint used for direct managed traces and metrics ingestion.

Remarks: This endpoint is only used when `UseManagedOpenTelemetryIngestion` is enabled, the shared `Engine:Observability:Telemetry:Endpoint` setting is omitted, and the protocol remains on `otlp` or `otlp/grpc`.

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-managedhttpmetricsendpoint"></a>

##### `ManagedHttpMetricsEndpoint`

```csharp
string ManagedHttpMetricsEndpoint { get; set; }
```

Gets or sets the Alibaba Cloud Managed Service for OpenTelemetry OTLP/HTTP metrics endpoint used for direct managed ingestion.

Remarks: This endpoint is only used when `UseManagedOpenTelemetryIngestion` is enabled, the shared endpoint is omitted, and the protocol stays on `otlp/http`. The value should already be the full Alibaba Cloud-managed metrics URL, including any tokenized path shape required by the provider.

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-managedhttptracesendpoint"></a>

##### `ManagedHttpTracesEndpoint`

```csharp
string ManagedHttpTracesEndpoint { get; set; }
```

Gets or sets the Alibaba Cloud Managed Service for OpenTelemetry OTLP/HTTP traces endpoint used for direct managed ingestion.

Remarks: This endpoint is only used when `UseManagedOpenTelemetryIngestion` is enabled, the shared endpoint is omitted, and the protocol stays on `otlp/http`. The value should already be the full Alibaba Cloud-managed trace URL, including any tokenized path shape required by the provider.

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-region"></a>

##### `Region`

```csharp
string Region { get; set; }
```

Gets or sets the Alibaba Cloud region to stamp onto exported resources when one should be explicit.

<a id="member-p-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-usemanagedopentelemetryingestion"></a>

##### `UseManagedOpenTelemetryIngestion`

```csharp
bool UseManagedOpenTelemetryIngestion { get; set; }
```

Gets or sets a value indicating whether the package should use Alibaba Cloud Managed Service for OpenTelemetry when no shared collector endpoint is configured.

Remarks: This direct managed path is intentionally limited to traces and metrics. Logs stay on the shared collector path, SLS, or another runtime-specific route instead of being redirected implicitly.

#### Methods

<a id="member-m-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
AlibabaCloudTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Alibaba Cloud telemetry export options from configuration.

Returns: The bound Alibaba Cloud telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-alibabacloud-hosting"></a>

## Namespace Cephalon.Observability.AlibabaCloud.Hosting

<a id="type-cephalon-observability-alibabacloud-hosting-alibabacloudhostapplicationbuilderextensions"></a>

### `AlibabaCloudHostApplicationBuilderExtensions`

Adds Alibaba Cloud-hosted observability defaults and optional managed OpenTelemetry ingestion for Cephalon hosts.

#### Declaration
```csharp
public static class AlibabaCloudHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-alibabacloud-hosting-alibabacloudhostapplicationbuilderextensions-addcephalonalibabacloud-1-0-system-action-cephalon-observability-alibabacloud-configuration-alibabacloudtelemetryexportoptions"></a>

##### `AddCephalonAlibabaCloud`

```csharp
TBuilder AddCephalonAlibabaCloud<TBuilder>(this TBuilder builder, Action<AlibabaCloudTelemetryExportOptions> configure)
```

Adds Alibaba Cloud-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps Alibaba Cloud-specific resource defaults and managed OpenTelemetry-ingestion concerns outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers Alibaba Cloud resource defaults on top. When those shared endpoint settings are absent and `UseManagedOpenTelemetryIngestion` is enabled, the package targets the configured Alibaba Cloud Managed Service for OpenTelemetry path for traces and metrics only. Logs stay on the shared collector path, SLS, or another runtime-specific route instead of being redirected implicitly.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Alibaba Cloud telemetry export options.
