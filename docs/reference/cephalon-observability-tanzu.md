# Cephalon.Observability.Tanzu

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.Tanzu)
## Namespaces

- `Cephalon.Observability.Tanzu.Configuration`
- `Cephalon.Observability.Tanzu.Hosting`

<a id="namespace-cephalon-observability-tanzu-configuration"></a>

## Namespace Cephalon.Observability.Tanzu.Configuration

<a id="type-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions"></a>

### `TanzuTelemetryExportOptions`

Configures VMware Tanzu observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class TanzuTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-ctor"></a>

##### `TanzuTelemetryExportOptions`

```csharp
TanzuTelemetryExportOptions()
```

Initializes a new instance of the `TanzuTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-clustername"></a>

##### `ClusterName`

```csharp
string ClusterName { get; set; }
```

Gets or sets the Kubernetes cluster name to stamp onto exported resources.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-headers"></a>

##### `Headers`

```csharp
string Headers { get; set; }
```

Gets or sets the raw OTLP headers string added to Tanzu proxy or shared collector requests.

Remarks: Use the standard OTLP `key=value` comma-separated format when a proxy, route, or gateway expects explicit headers.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the VMware Tanzu deployment target whose hosted defaults should be applied.

Remarks: Supported values are `tkg`, `tkgi`, and `tap`.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-namespace"></a>

##### `Namespace`

```csharp
string Namespace { get; set; }
```

Gets or sets the workload namespace to stamp onto exported resources.

Remarks: When omitted, the package falls back to the current pod namespace through the `POD_NAMESPACE` environment variable when it is available.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-proxynamespace"></a>

##### `ProxyNamespace`

```csharp
string ProxyNamespace { get; set; }
```

Gets or sets the proxy namespace used to build the in-cluster Tanzu proxy endpoint.

Remarks: When omitted, the package falls back to `Namespace` and then to `POD_NAMESPACE` when the host runs inside a pod.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-proxypath"></a>

##### `ProxyPath`

```csharp
string ProxyPath { get; set; }
```

Gets or sets the optional base path used for the in-cluster Tanzu proxy endpoint.

Remarks: When `otlp/http` is selected, the package still appends the standard OTLP signal path beneath this base path unless the full signal path was already provided.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-proxyport"></a>

##### `ProxyPort`

```csharp
int? ProxyPort { get; set; }
```

Gets or sets the proxy port used for the in-cluster Tanzu proxy endpoint.

Remarks: This value stays required when `UseInClusterProxyService` is enabled so the package does not pretend the current Tanzu proxy path has one generic vendor-wide OTLP port for every deployment.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-proxyscheme"></a>

##### `ProxyScheme`

```csharp
string ProxyScheme { get; set; }
```

Gets or sets the URI scheme used for the in-cluster Tanzu proxy endpoint.

Remarks: Supported values are `http` and `https`. The default is `http`.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-proxyservicename"></a>

##### `ProxyServiceName`

```csharp
string ProxyServiceName { get; set; }
```

Gets or sets the proxy service name used to build the in-cluster Tanzu proxy endpoint.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-trustedcacertificatepath"></a>

##### `TrustedCaCertificatePath`

```csharp
string TrustedCaCertificatePath { get; set; }
```

Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics against shared collectors or Tanzu proxy endpoints.

Remarks: Because the current OTLP logging exporter does not support custom `HttpClient` wiring for HTTP, the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA wiring is also left explicit and unsupported for now.

<a id="member-p-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-useinclusterproxyservice"></a>

##### `UseInClusterProxyService`

```csharp
bool UseInClusterProxyService { get; set; }
```

Gets or sets a value indicating whether the package should target an in-cluster Tanzu proxy service for trace handoff when no shared endpoint is configured.

Remarks: This trace-handoff path intentionally stays explicit because the current Tanzu and Wavefront documentation centers proxy-mediated OpenTelemetry traces, not one generic managed OTLP backend for every signal.

#### Methods

<a id="member-m-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
TanzuTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Tanzu telemetry export options from configuration.

Returns: The bound Tanzu telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-tanzu-hosting"></a>

## Namespace Cephalon.Observability.Tanzu.Hosting

<a id="type-cephalon-observability-tanzu-hosting-tanzuhostapplicationbuilderextensions"></a>

### `TanzuHostApplicationBuilderExtensions`

Adds VMware Tanzu-hosted observability defaults and proxy handoff wiring for Cephalon hosts.

#### Declaration
```csharp
public static class TanzuHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-tanzu-hosting-tanzuhostapplicationbuilderextensions-addcephalontanzu-1-0-system-action-cephalon-observability-tanzu-configuration-tanzutelemetryexportoptions"></a>

##### `AddCephalonTanzu`

```csharp
TBuilder AddCephalonTanzu<TBuilder>(this TBuilder builder, Action<TanzuTelemetryExportOptions> configure)
```

Adds Tanzu-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps Tanzu-specific proxy inputs and hosted resource defaults outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers Tanzu resource defaults on top. When those shared endpoint settings are absent and `UseInClusterProxyService` is enabled, the package enables an explicit trace-focused Tanzu proxy handoff path instead of pretending the current Tanzu documentation describes one generic vendor-direct OTLP backend for every signal.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Tanzu telemetry export options.
