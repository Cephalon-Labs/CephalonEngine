# Cephalon.Observability.OpenShift

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.OpenShift)
## Namespaces

- `Cephalon.Observability.OpenShift.Configuration`
- `Cephalon.Observability.OpenShift.Hosting`

<a id="namespace-cephalon-observability-openshift-configuration"></a>

## Namespace Cephalon.Observability.OpenShift.Configuration

<a id="type-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions"></a>

### `OpenShiftTelemetryExportOptions`

Configures Red Hat OpenShift observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class OpenShiftTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-ctor"></a>

##### `OpenShiftTelemetryExportOptions`

```csharp
OpenShiftTelemetryExportOptions()
```

Initializes a new instance of the `OpenShiftTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-clustername"></a>

##### `ClusterName`

```csharp
string ClusterName { get; set; }
```

Gets or sets the Kubernetes cluster name to stamp onto exported resources.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-collectornamespace"></a>

##### `CollectorNamespace`

```csharp
string CollectorNamespace { get; set; }
```

Gets or sets the collector namespace used to build the in-cluster OpenShift collector endpoint.

Remarks: When omitted, the package falls back to `Namespace` and then to `POD_NAMESPACE` when the host runs inside a pod.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-collectorport"></a>

##### `CollectorPort`

```csharp
int? CollectorPort { get; set; }
```

Gets or sets the collector port used for the in-cluster OpenShift collector endpoint.

Remarks: When omitted, the package falls back to `4317` for `otlp` / `otlp/grpc` and `4318` for `otlp/http`.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-collectorscheme"></a>

##### `CollectorScheme`

```csharp
string CollectorScheme { get; set; }
```

Gets or sets the URI scheme used for the in-cluster OpenShift collector endpoint.

Remarks: Supported values are `http` and `https`. The default is `http`.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-collectorservicename"></a>

##### `CollectorServiceName`

```csharp
string CollectorServiceName { get; set; }
```

Gets or sets the collector service name used to build the in-cluster OpenShift collector endpoint.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-headers"></a>

##### `Headers`

```csharp
string Headers { get; set; }
```

Gets or sets the raw OTLP headers string added to OpenShift collector requests.

Remarks: Use the standard OTLP `key=value` comma-separated format when a collector, gateway, or route expects explicit headers.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the OpenShift deployment target whose hosted defaults should be applied.

Remarks: Supported values are `openshift`, `aro`, and `rosa`.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-namespace"></a>

##### `Namespace`

```csharp
string Namespace { get; set; }
```

Gets or sets the workload namespace to stamp onto exported resources.

Remarks: When omitted, the package falls back to the current pod namespace through the `POD_NAMESPACE` environment variable when it is available.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-trustedcacertificatepath"></a>

##### `TrustedCaCertificatePath`

```csharp
string TrustedCaCertificatePath { get; set; }
```

Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics against in-cluster or shared OpenShift collectors.

Remarks: This setting is intended for OpenShift service CAs or other cluster-local trust roots. Because the current OTLP logging exporter does not support custom `HttpClient` wiring for HTTP, the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA wiring is also left explicit and unsupported for now.

<a id="member-p-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-useinclustercollectorservice"></a>

##### `UseInClusterCollectorService`

```csharp
bool UseInClusterCollectorService { get; set; }
```

Gets or sets a value indicating whether the package should target an in-cluster OpenShift collector service when no shared endpoint is configured.

#### Methods

<a id="member-m-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
OpenShiftTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds OpenShift telemetry export options from configuration.

Returns: The bound OpenShift telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-openshift-hosting"></a>

## Namespace Cephalon.Observability.OpenShift.Hosting

<a id="type-cephalon-observability-openshift-hosting-openshifthostapplicationbuilderextensions"></a>

### `OpenShiftHostApplicationBuilderExtensions`

Adds Red Hat OpenShift-hosted observability defaults and in-cluster collector wiring for Cephalon hosts.

#### Declaration
```csharp
public static class OpenShiftHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-openshift-hosting-openshifthostapplicationbuilderextensions-addcephalonopenshift-1-0-system-action-cephalon-observability-openshift-configuration-openshifttelemetryexportoptions"></a>

##### `AddCephalonOpenShift`

```csharp
TBuilder AddCephalonOpenShift<TBuilder>(this TBuilder builder, Action<OpenShiftTelemetryExportOptions> configure)
```

Adds OpenShift-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps OpenShift-specific collector selection, trust material, and hosted resource defaults outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers OpenShift resource defaults on top. When those shared endpoint settings are absent and `UseInClusterCollectorService` is enabled, the package resolves an explicit in-cluster service endpoint so OpenShift operator-managed collectors stay discoverable without moving provider logic back into the engine core.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven OpenShift telemetry export options.
