# Cephalon.Edge.KubernetesGateway

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Edge.KubernetesGateway)
## Namespaces

- `Cephalon.Edge.KubernetesGateway.Configuration`
- `Cephalon.Edge.KubernetesGateway.Registration`

<a id="namespace-cephalon-edge-kubernetesgateway-configuration"></a>

## Namespace Cephalon.Edge.KubernetesGateway.Configuration

<a id="type-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions"></a>

### `KubernetesGatewayTrafficMaterializerOptions`

Configures the Kubernetes Gateway API control-plane materializer for provider-managed cell traffic automation.

#### Declaration
```csharp
public sealed class KubernetesGatewayTrafficMaterializerOptions
```

#### Constructors

<a id="member-m-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-ctor"></a>

##### `KubernetesGatewayTrafficMaterializerOptions`

```csharp
KubernetesGatewayTrafficMaterializerOptions()
```

Initializes a new instance of the `KubernetesGatewayTrafficMaterializerOptions` class.

#### Fields

<a id="member-f-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-defaultmaterializerid"></a>

##### `DefaultMaterializerId`

```csharp
const string DefaultMaterializerId
```

Gets the default materializer identifier used by the Kubernetes Gateway traffic materializer.

<a id="member-f-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-defaultproviderid"></a>

##### `DefaultProviderId`

```csharp
const string DefaultProviderId
```

Gets the default provider identifier used by the Kubernetes Gateway traffic materializer.

#### Properties

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-controllername"></a>

##### `ControllerName`

```csharp
string ControllerName { get; set; }
```

Gets or sets the optional Gateway controller name that owns the configured GatewayClass.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-gatewayclassname"></a>

##### `GatewayClassName`

```csharp
string GatewayClassName { get; set; }
```

Gets or sets the optional default GatewayClass name that backs the projected traffic intent.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-gatewayname"></a>

##### `GatewayName`

```csharp
string GatewayName { get; set; }
```

Gets or sets the default Gateway name targeted by projected HTTPRoute parent references.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-gatewaynamespace"></a>

##### `GatewayNamespace`

```csharp
string GatewayNamespace { get; set; }
```

Gets or sets the default Kubernetes namespace that contains the projected Gateway resource.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-listenername"></a>

##### `ListenerName`

```csharp
string ListenerName { get; set; }
```

Gets or sets the optional default Gateway listener or section name used by projected parent references.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-materializerid"></a>

##### `MaterializerId`

```csharp
string MaterializerId { get; set; }
```

Gets or sets the stable materializer identifier that should appear on operator-facing runtime answers.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-observation"></a>

##### `Observation`

```csharp
KubernetesGatewayTrafficObservationOptions Observation { get; }
```

Gets the live-observation options used to overlay Kubernetes Gateway API status back into the shared runtime catalog.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-priority"></a>

##### `Priority`

```csharp
int Priority { get; set; }
```

Gets or sets the priority used when multiple provider materializers can reconcile the same automation answer. Higher values win while ties still fail deterministically in the engine.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; set; }
```

Gets or sets the provider identifier that the materializer owns.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-routenamespace"></a>

##### `RouteNamespace`

```csharp
string RouteNamespace { get; set; }
```

Gets or sets the default namespace used for projected HTTPRoute resources when a route-level override is absent.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions-routes"></a>

##### `Routes`

```csharp
IList<KubernetesGatewayTrafficRouteOptions> Routes { get; }
```

Gets the route-level Kubernetes Gateway projections owned by this materializer.

<a id="type-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationmodes"></a>

### `KubernetesGatewayTrafficObservationModes`

Defines the stable control-plane modes supported by the Kubernetes Gateway traffic materializer.

#### Declaration
```csharp
public static class KubernetesGatewayTrafficObservationModes
```

#### Fields

<a id="member-f-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationmodes-applyandreconcile"></a>

##### `ApplyAndReconcile`

```csharp
const string ApplyAndReconcile
```

Applies owned HTTPRoute resources and then reconciles the observed Gateway API status back into the shared runtime catalog.

<a id="member-f-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationmodes-configuredintent"></a>

##### `ConfiguredIntent`

```csharp
const string ConfiguredIntent
```

Publishes configured Kubernetes Gateway API intent without reading or writing live control-plane resources.

<a id="member-f-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationmodes-observeonly"></a>

##### `ObserveOnly`

```csharp
const string ObserveOnly
```

Reads live Kubernetes Gateway API status and projects the observed posture back into the shared runtime catalog.

<a id="type-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions"></a>

### `KubernetesGatewayTrafficObservationOptions`

Configures how the Kubernetes Gateway traffic materializer interacts with the Kubernetes Gateway API control plane.

#### Declaration
```csharp
public sealed class KubernetesGatewayTrafficObservationOptions
```

#### Constructors

<a id="member-m-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-ctor"></a>

##### `KubernetesGatewayTrafficObservationOptions`

```csharp
KubernetesGatewayTrafficObservationOptions()
```

Initializes a new instance of the `KubernetesGatewayTrafficObservationOptions` class.

#### Properties

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-enablecleanupsweep"></a>

##### `EnableCleanupSweep`

```csharp
bool EnableCleanupSweep { get; set; }
```

Gets or sets a value indicating whether apply-and-reconcile mode should also sweep previously owned stale HTTPRoute resources.

Remarks: This cleanup sweep is disabled by default so existing apply-and-reconcile behavior stays additive. When enabled, the materializer will delete transferred resources and prune orphaned resources that still carry Cephalon ownership labels in the configured route namespaces.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-kubeconfigpath"></a>

##### `KubeConfigPath`

```csharp
string KubeConfigPath { get; set; }
```

Gets or sets the explicit kubeconfig path used when the pack creates its own client outside the cluster.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-kubecontext"></a>

##### `KubeContext`

```csharp
string KubeContext { get; set; }
```

Gets or sets the optional kubeconfig context override used when the pack creates its own client.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-masterurl"></a>

##### `MasterUrl`

```csharp
string MasterUrl { get; set; }
```

Gets or sets the optional API-server override used when the pack creates its own client from kubeconfig.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-mode"></a>

##### `Mode`

```csharp
string Mode { get; set; }
```

Gets or sets the control-plane mode used by the Kubernetes Gateway materializer.

Remarks: The default value keeps the pack in configured-intent mode so existing projected-intent behavior remains additive without claiming a live apply. Set this to `observe-only` when the pack should read live Gateway API resources without writing them, or `apply-and-reconcile` when the pack should manage owned HTTPRoute resources and continuously reconcile observed status back into the shared runtime surfaces.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-pollingintervalseconds"></a>

##### `PollingIntervalSeconds`

```csharp
int PollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, used for recurring live observation or reconciliation after startup materialization.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-staleafterseconds"></a>

##### `StaleAfterSeconds`

```csharp
int StaleAfterSeconds { get; set; }
```

Gets or sets the freshness window, in seconds, that observed status should advertise to operators.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficobservationoptions-useinclusterconfiguration"></a>

##### `UseInClusterConfiguration`

```csharp
bool UseInClusterConfiguration { get; set; }
```

Gets or sets a value indicating whether in-cluster Kubernetes configuration should be used when the pack creates its own client.

<a id="type-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions"></a>

### `KubernetesGatewayTrafficRouteOptions`

Configures one cell traffic automation route that should materialize into Kubernetes Gateway API intent.

#### Declaration
```csharp
public sealed class KubernetesGatewayTrafficRouteOptions
```

#### Constructors

<a id="member-m-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-ctor"></a>

##### `KubernetesGatewayTrafficRouteOptions`

```csharp
KubernetesGatewayTrafficRouteOptions()
```

Initializes a new instance of the `KubernetesGatewayTrafficRouteOptions` class.

#### Properties

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-backendnamespace"></a>

##### `BackendNamespace`

```csharp
string BackendNamespace { get; set; }
```

Gets or sets the Kubernetes namespace that contains the projected backend Service. When omitted, the materializer falls back to the effective target route namespace.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-backendport"></a>

##### `BackendPort`

```csharp
int? BackendPort { get; set; }
```

Gets or sets the backend Service port referenced by the projected HTTPRoute.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-backendservicename"></a>

##### `BackendServiceName`

```csharp
string BackendServiceName { get; set; }
```

Gets or sets the backend Kubernetes Service name referenced by the projected HTTPRoute.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-backendweight"></a>

##### `BackendWeight`

```csharp
int? BackendWeight { get; set; }
```

Gets or sets the optional backend weight applied to the projected Service reference.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-controllername"></a>

##### `ControllerName`

```csharp
string ControllerName { get; set; }
```

Gets or sets the optional controller name associated with the parent GatewayClass. When omitted, the materializer falls back to the pack-level `ControllerName`.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-gatewayclassname"></a>

##### `GatewayClassName`

```csharp
string GatewayClassName { get; set; }
```

Gets or sets the optional GatewayClass name associated with the parent Gateway. When omitted, the materializer falls back to the pack-level `GatewayClassName`.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-gatewayname"></a>

##### `GatewayName`

```csharp
string GatewayName { get; set; }
```

Gets or sets the parent Gateway name targeted by the projected HTTPRoute. When omitted, the materializer falls back to the pack-level `GatewayName`.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-gatewaynamespace"></a>

##### `GatewayNamespace`

```csharp
string GatewayNamespace { get; set; }
```

Gets or sets the Kubernetes namespace that contains the parent Gateway. When omitted, the materializer falls back to the pack-level `GatewayNamespace`.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-hostnames"></a>

##### `Hostnames`

```csharp
IList<string> Hostnames { get; }
```

Gets the optional hostnames published by the projected HTTPRoute.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-httproutename"></a>

##### `HttpRouteName`

```csharp
string HttpRouteName { get; set; }
```

Gets or sets the HTTPRoute resource name that should represent the route in Kubernetes Gateway API. When omitted, the materializer derives a deterministic DNS-safe name from `RouteId`.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-listenername"></a>

##### `ListenerName`

```csharp
string ListenerName { get; set; }
```

Gets or sets the optional Gateway listener or section name used by the projected parent reference. When omitted, the materializer falls back to the pack-level `ListenerName`.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-routeid"></a>

##### `RouteId`

```csharp
string RouteId { get; set; }
```

Gets or sets the Cephalon cell-route identifier that this provider projection owns.

<a id="member-p-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficrouteoptions-routenamespace"></a>

##### `RouteNamespace`

```csharp
string RouteNamespace { get; set; }
```

Gets or sets the Kubernetes namespace that should own the projected HTTPRoute resource. When omitted, the materializer falls back to the pack-level `RouteNamespace` and then the effective Gateway namespace.

<a id="namespace-cephalon-edge-kubernetesgateway-registration"></a>

## Namespace Cephalon.Edge.KubernetesGateway.Registration

<a id="type-cephalon-edge-kubernetesgateway-registration-kubernetesgatewayenginebuilderextensions"></a>

### `KubernetesGatewayEngineBuilderExtensions`

Registers the Kubernetes Gateway API traffic materializer companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class KubernetesGatewayEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-edge-kubernetesgateway-registration-kubernetesgatewayenginebuilderextensions-addkubernetesgatewaytrafficmaterializer-cephalon-engine-composition-enginebuilder-system-action-cephalon-edge-kubernetesgateway-configuration-kubernetesgatewaytrafficmaterializeroptions"></a>

##### `AddKubernetesGatewayTrafficMaterializer`

```csharp
EngineBuilder AddKubernetesGatewayTrafficMaterializer(this EngineBuilder builder, Action<KubernetesGatewayTrafficMaterializerOptions> configure)
```

Adds the Kubernetes Gateway API traffic materializer companion pack to the engine.

Remarks: This pack keeps Kubernetes Gateway API control-plane projection outside `Cephalon.Engine` while still reporting provider materialization truth back through the shared `/engine/cell-traffic-automations*`, `/engine/technology-surfaces`, and `snapshot` surfaces.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures how provider-managed cell traffic automation should project into Kubernetes Gateway API intent and, when enabled, reconcile owned HTTPRoute resources.
