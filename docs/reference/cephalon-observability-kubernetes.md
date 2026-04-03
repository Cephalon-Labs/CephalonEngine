# Cephalon.Observability.Kubernetes

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.Kubernetes)
## Namespaces

- `Cephalon.Observability.Kubernetes.Configuration`
- `Cephalon.Observability.Kubernetes.Hosting`

<a id="namespace-cephalon-observability-kubernetes-configuration"></a>

## Namespace Cephalon.Observability.Kubernetes.Configuration

<a id="type-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions"></a>

### `KubernetesTelemetryExportOptions`

Configures Kubernetes observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class KubernetesTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-ctor"></a>

##### `KubernetesTelemetryExportOptions`

```csharp
KubernetesTelemetryExportOptions()
```

Initializes a new instance of the `KubernetesTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-clustername"></a>

##### `ClusterName`

```csharp
string ClusterName { get; set; }
```

Gets or sets the Kubernetes cluster name to stamp onto exported resources.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-collectornamespace"></a>

##### `CollectorNamespace`

```csharp
string CollectorNamespace { get; set; }
```

Gets or sets the collector namespace used to build the in-cluster Kubernetes collector endpoint.

Remarks: When omitted, the package falls back to `Namespace` and then to `POD_NAMESPACE` when the host runs inside a pod.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-collectorport"></a>

##### `CollectorPort`

```csharp
int? CollectorPort { get; set; }
```

Gets or sets the collector port used for the in-cluster Kubernetes collector endpoint.

Remarks: When omitted, the package falls back to `4317` for `otlp` / `otlp/grpc` and `4318` for `otlp/http`.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-collectorscheme"></a>

##### `CollectorScheme`

```csharp
string CollectorScheme { get; set; }
```

Gets or sets the URI scheme used for the in-cluster Kubernetes collector endpoint.

Remarks: Supported values are `http` and `https`. The default is `http`.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-collectorservicename"></a>

##### `CollectorServiceName`

```csharp
string CollectorServiceName { get; set; }
```

Gets or sets the collector service name used to build the in-cluster Kubernetes collector endpoint.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-containername"></a>

##### `ContainerName`

```csharp
string ContainerName { get; set; }
```

Gets or sets the container name to stamp onto exported resources.

Remarks: When omitted, the package falls back to the `CONTAINER_NAME` environment variable when it is available.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-headers"></a>

##### `Headers`

```csharp
string Headers { get; set; }
```

Gets or sets the raw OTLP headers string added to Kubernetes collector requests.

Remarks: Use the standard OTLP `key=value` comma-separated format when a collector, gateway, or route expects explicit headers.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-namespace"></a>

##### `Namespace`

```csharp
string Namespace { get; set; }
```

Gets or sets the workload namespace to stamp onto exported resources.

Remarks: When omitted, the package falls back to the current pod namespace through the `POD_NAMESPACE` environment variable when it is available.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-nodename"></a>

##### `NodeName`

```csharp
string NodeName { get; set; }
```

Gets or sets the Kubernetes node name to stamp onto exported resources.

Remarks: When omitted, the package falls back to the `NODE_NAME` environment variable when it is available.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-podname"></a>

##### `PodName`

```csharp
string PodName { get; set; }
```

Gets or sets the pod name to stamp onto exported resources.

Remarks: When omitted, the package falls back to `POD_NAME` and then `HOSTNAME` when those environment variables are available.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-poduid"></a>

##### `PodUid`

```csharp
string PodUid { get; set; }
```

Gets or sets the pod UID to stamp onto exported resources.

Remarks: When omitted, the package falls back to the `POD_UID` environment variable when it is available.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-servicednssuffix"></a>

##### `ServiceDnsSuffix`

```csharp
string ServiceDnsSuffix { get; set; }
```

Gets or sets the service DNS suffix used to build the in-cluster Kubernetes collector endpoint.

Remarks: The default is `svc.cluster.local`. Set this when a cluster uses a non-default service DNS suffix.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-trustedcacertificatepath"></a>

##### `TrustedCaCertificatePath`

```csharp
string TrustedCaCertificatePath { get; set; }
```

Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics against in-cluster or shared Kubernetes collectors.

Remarks: Because the current OTLP logging exporter does not support custom `HttpClient` wiring for HTTP, the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA wiring is also left explicit and unsupported for now.

<a id="member-p-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-useinclustercollectorservice"></a>

##### `UseInClusterCollectorService`

```csharp
bool UseInClusterCollectorService { get; set; }
```

Gets or sets a value indicating whether the package should target an in-cluster collector service when no shared endpoint is configured.

#### Methods

<a id="member-m-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
KubernetesTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Kubernetes telemetry export options from configuration.

Returns: The bound Kubernetes telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-kubernetes-hosting"></a>

## Namespace Cephalon.Observability.Kubernetes.Hosting

<a id="type-cephalon-observability-kubernetes-hosting-kuberneteshostapplicationbuilderextensions"></a>

### `KubernetesHostApplicationBuilderExtensions`

Adds Kubernetes-hosted observability defaults and in-cluster collector wiring for Cephalon hosts.

#### Declaration
```csharp
public static class KubernetesHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-kubernetes-hosting-kuberneteshostapplicationbuilderextensions-addcephalonkubernetes-1-0-system-action-cephalon-observability-kubernetes-configuration-kubernetestelemetryexportoptions"></a>

##### `AddCephalonKubernetes`

```csharp
TBuilder AddCephalonKubernetes<TBuilder>(this TBuilder builder, Action<KubernetesTelemetryExportOptions> configure)
```

Adds Kubernetes-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps Kubernetes-specific collector selection, cluster-local trust material, and resource defaults outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers Kubernetes resource defaults on top. When those shared endpoint settings are absent and `UseInClusterCollectorService` is enabled, the package resolves an explicit in-cluster service endpoint so generic Kubernetes deployments can stay collector-first without forcing teams onto a vendor-specific companion package.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Kubernetes telemetry export options.
