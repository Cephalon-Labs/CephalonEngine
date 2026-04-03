# Cephalon.Observability.DigitalOcean

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.DigitalOcean)
## Namespaces

- `Cephalon.Observability.DigitalOcean.Configuration`
- `Cephalon.Observability.DigitalOcean.Hosting`

<a id="namespace-cephalon-observability-digitalocean-configuration"></a>

## Namespace Cephalon.Observability.DigitalOcean.Configuration

<a id="type-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions"></a>

### `DigitalOceanTelemetryExportOptions`

Configures DigitalOcean observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class DigitalOceanTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-ctor"></a>

##### `DigitalOceanTelemetryExportOptions`

```csharp
DigitalOceanTelemetryExportOptions()
```

Initializes a new instance of the `DigitalOceanTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-appid"></a>

##### `AppId`

```csharp
string AppId { get; set; }
```

Gets or sets the App Platform application identifier to stamp onto exported resources.

Remarks: When omitted, the package falls back to the `APP_ID` or `DIGITALOCEAN_APP_ID` environment variable when it is available.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-appurl"></a>

##### `AppUrl`

```csharp
string AppUrl { get; set; }
```

Gets or sets the App Platform public URL to stamp onto exported resources.

Remarks: When omitted, the package falls back to the `APP_URL` or `DIGITALOCEAN_APP_URL` environment variable when it is available.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-clustername"></a>

##### `ClusterName`

```csharp
string ClusterName { get; set; }
```

Gets or sets the Kubernetes cluster name to stamp onto exported resources for DOKS deployments.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-collectornamespace"></a>

##### `CollectorNamespace`

```csharp
string CollectorNamespace { get; set; }
```

Gets or sets the collector namespace used to build the in-cluster DOKS collector endpoint.

Remarks: When omitted, the package falls back to `Namespace` and then to `POD_NAMESPACE` when the host runs inside a pod.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-collectorport"></a>

##### `CollectorPort`

```csharp
int? CollectorPort { get; set; }
```

Gets or sets the collector port used for the in-cluster DOKS collector endpoint.

Remarks: When omitted, the package falls back to `4317` for `otlp` / `otlp/grpc` and `4318` for `otlp/http`.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-collectorscheme"></a>

##### `CollectorScheme`

```csharp
string CollectorScheme { get; set; }
```

Gets or sets the URI scheme used for the in-cluster DOKS collector endpoint.

Remarks: Supported values are `http` and `https`. The default is `http`.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-collectorservicename"></a>

##### `CollectorServiceName`

```csharp
string CollectorServiceName { get; set; }
```

Gets or sets the collector service name used to build the in-cluster DOKS collector endpoint.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-dropletid"></a>

##### `DropletId`

```csharp
string DropletId { get; set; }
```

Gets or sets the Droplet identifier to stamp onto exported resources.

Remarks: When omitted and `UseDropletMetadataDefaults` is enabled, the package attempts to read the identifier from the Droplet metadata service.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-dropletmetadataendpoint"></a>

##### `DropletMetadataEndpoint`

```csharp
string DropletMetadataEndpoint { get; set; }
```

Gets or sets the base URI of the Droplet metadata-service endpoint.

Remarks: When omitted, the package falls back to the standard DigitalOcean metadata-service base URI. This override exists mainly for proxies, alternative routing, or automated testing.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-headers"></a>

##### `Headers`

```csharp
string Headers { get; set; }
```

Gets or sets the raw OTLP headers string added to collector requests.

Remarks: Use the standard OTLP `key=value` comma-separated format when a collector, gateway, or route expects explicit headers.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the DigitalOcean deployment target whose hosted defaults should be applied.

Remarks: Supported values are `droplet`, `doks`, and `app-platform`. The package maps them to package-specific `cloud.platform` values so the collector-first DigitalOcean slice stays explicit without pretending those values are part of the engine core.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-metadatatimeoutmilliseconds"></a>

##### `MetadataTimeoutMilliseconds`

```csharp
int? MetadataTimeoutMilliseconds { get; set; }
```

Gets or sets the timeout, in milliseconds, used for Droplet metadata-service lookups.

Remarks: When omitted, the package falls back to a short one-second timeout so non-Droplet hosts do not stall during best-effort metadata detection.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-namespace"></a>

##### `Namespace`

```csharp
string Namespace { get; set; }
```

Gets or sets the workload namespace to stamp onto exported resources for DOKS deployments.

Remarks: When omitted, the package falls back to the current pod namespace through the `POD_NAMESPACE` environment variable when it is available.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-region"></a>

##### `Region`

```csharp
string Region { get; set; }
```

Gets or sets the DigitalOcean region slug to stamp onto exported resources.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-trustedcacertificatepath"></a>

##### `TrustedCaCertificatePath`

```csharp
string TrustedCaCertificatePath { get; set; }
```

Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics against shared or in-cluster DigitalOcean collectors.

Remarks: Because the current OTLP logging exporter does not support custom `HttpClient` wiring for HTTP, the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA wiring is also left explicit and unsupported for now.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-usedropletmetadatadefaults"></a>

##### `UseDropletMetadataDefaults`

```csharp
bool UseDropletMetadataDefaults { get; set; }
```

Gets or sets a value indicating whether the package should query the Droplet metadata service to fill in best-effort `host.id`, `host.name`, and `cloud.region` values when they are missing.

<a id="member-p-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-useinclustercollectorservice"></a>

##### `UseInClusterCollectorService`

```csharp
bool UseInClusterCollectorService { get; set; }
```

Gets or sets a value indicating whether the package should target an in-cluster collector service for DOKS deployments when no shared endpoint is configured.

#### Methods

<a id="member-m-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
DigitalOceanTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds DigitalOcean telemetry export options from configuration.

Returns: The bound DigitalOcean telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-digitalocean-hosting"></a>

## Namespace Cephalon.Observability.DigitalOcean.Hosting

<a id="type-cephalon-observability-digitalocean-hosting-digitaloceanhostapplicationbuilderextensions"></a>

### `DigitalOceanHostApplicationBuilderExtensions`

Adds DigitalOcean-hosted observability defaults and collector wiring for Cephalon hosts.

#### Declaration
```csharp
public static class DigitalOceanHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-digitalocean-hosting-digitaloceanhostapplicationbuilderextensions-addcephalondigitalocean-1-0-system-action-cephalon-observability-digitalocean-configuration-digitaloceantelemetryexportoptions"></a>

##### `AddCephalonDigitalOcean`

```csharp
TBuilder AddCephalonDigitalOcean<TBuilder>(this TBuilder builder, Action<DigitalOceanTelemetryExportOptions> configure)
```

Adds DigitalOcean-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps DigitalOcean-specific collector selection, best-effort Droplet metadata defaults, and hosted resource defaults outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers DigitalOcean resource defaults on top. When those shared endpoint settings are absent and `UseInClusterCollectorService` is enabled, the package resolves an explicit in-cluster service endpoint for DOKS deployments so collector-first DigitalOcean setups stay explicit without over-claiming a managed DigitalOcean OTLP exporter path.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven DigitalOcean telemetry export options.
