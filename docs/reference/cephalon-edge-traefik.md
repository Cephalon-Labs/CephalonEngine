# Cephalon.Edge.Traefik

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Edge.Traefik)
## Namespaces

- `Cephalon.Edge.Traefik.Configuration`
- `Cephalon.Edge.Traefik.Registration`

<a id="namespace-cephalon-edge-traefik-configuration"></a>

## Namespace Cephalon.Edge.Traefik.Configuration

<a id="type-cephalon-edge-traefik-configuration-traefikingressrouteoptions"></a>

### `TraefikIngressRouteOptions`

Configures one cell traffic automation route that should materialize into Traefik IngressRoute intent.

#### Declaration
```csharp
public sealed class TraefikIngressRouteOptions
```

#### Constructors

<a id="member-m-cephalon-edge-traefik-configuration-traefikingressrouteoptions-ctor"></a>

##### `TraefikIngressRouteOptions`

```csharp
TraefikIngressRouteOptions()
```

Initializes a new instance of the `TraefikIngressRouteOptions` class.

#### Properties

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-backendnamespace"></a>

##### `BackendNamespace`

```csharp
string BackendNamespace { get; set; }
```

Gets or sets the Kubernetes namespace that owns the backend Service. When omitted, the materializer falls back to the effective IngressRoute namespace.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-backendport"></a>

##### `BackendPort`

```csharp
int? BackendPort { get; set; }
```

Gets or sets the backend Service port referenced by the projected route.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-backendscheme"></a>

##### `BackendScheme`

```csharp
string BackendScheme { get; set; }
```

Gets or sets the optional backend scheme such as `http` or `https`.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-backendservicename"></a>

##### `BackendServiceName`

```csharp
string BackendServiceName { get; set; }
```

Gets or sets the backend Kubernetes Service name referenced by the projected route.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-backendweight"></a>

##### `BackendWeight`

```csharp
int? BackendWeight { get; set; }
```

Gets or sets the optional backend weight applied to the projected Service reference.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-entrypoints"></a>

##### `EntryPoints`

```csharp
IList<string> EntryPoints { get; }
```

Gets the Traefik entry points that should accept requests for the projected route. When empty, the materializer falls back to the pack-level `EntryPoints` list.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-ingressroutename"></a>

##### `IngressRouteName`

```csharp
string IngressRouteName { get; set; }
```

Gets or sets the IngressRoute resource name that should represent the route in Traefik. When omitted, the materializer derives a deterministic DNS-safe name from `RouteId`.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-matchrule"></a>

##### `MatchRule`

```csharp
string MatchRule { get; set; }
```

Gets or sets the Traefik rule expression such as `Host(`api.example.com`) && PathPrefix(`/orders`)`.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-middlewares"></a>

##### `Middlewares`

```csharp
IList<TraefikMiddlewareReferenceOptions> Middlewares { get; }
```

Gets the middleware references that should attach to the projected route in declaration order.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-passhostheader"></a>

##### `PassHostHeader`

```csharp
bool? PassHostHeader { get; set; }
```

Gets or sets the optional pass-host-header posture applied to the projected backend Service reference.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-priority"></a>

##### `Priority`

```csharp
int? Priority { get; set; }
```

Gets or sets the optional explicit Traefik route priority.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-routeid"></a>

##### `RouteId`

```csharp
string RouteId { get; set; }
```

Gets or sets the Cephalon cell-route identifier that this provider projection owns.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-routenamespace"></a>

##### `RouteNamespace`

```csharp
string RouteNamespace { get; set; }
```

Gets or sets the Kubernetes namespace that should own the projected IngressRoute resource. When omitted, the materializer falls back to the pack-level `RouteNamespace`.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-tlsoptionsname"></a>

##### `TlsOptionsName`

```csharp
string TlsOptionsName { get; set; }
```

Gets or sets the optional TLSOption resource name associated with the projected route.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-tlsoptionsnamespace"></a>

##### `TlsOptionsNamespace`

```csharp
string TlsOptionsNamespace { get; set; }
```

Gets or sets the optional Kubernetes namespace that owns the referenced TLSOption. When omitted, the materializer falls back to the effective IngressRoute namespace.

<a id="member-p-cephalon-edge-traefik-configuration-traefikingressrouteoptions-tlssecretname"></a>

##### `TlsSecretName`

```csharp
string TlsSecretName { get; set; }
```

Gets or sets the optional TLS Secret name that should terminate the projected IngressRoute.

<a id="type-cephalon-edge-traefik-configuration-traefikmiddlewarereferenceoptions"></a>

### `TraefikMiddlewareReferenceOptions`

Configures one Traefik middleware reference that should attach to a projected IngressRoute rule.

#### Declaration
```csharp
public sealed class TraefikMiddlewareReferenceOptions
```

#### Constructors

<a id="member-m-cephalon-edge-traefik-configuration-traefikmiddlewarereferenceoptions-ctor"></a>

##### `TraefikMiddlewareReferenceOptions`

```csharp
TraefikMiddlewareReferenceOptions()
```

Initializes a new instance of the `TraefikMiddlewareReferenceOptions` class.

#### Properties

<a id="member-p-cephalon-edge-traefik-configuration-traefikmiddlewarereferenceoptions-name"></a>

##### `Name`

```csharp
string Name { get; set; }
```

Gets or sets the Traefik middleware resource name.

<a id="member-p-cephalon-edge-traefik-configuration-traefikmiddlewarereferenceoptions-namespace"></a>

##### `Namespace`

```csharp
string Namespace { get; set; }
```

Gets or sets the optional Kubernetes namespace that owns the middleware. When omitted, the materializer falls back to the effective IngressRoute namespace.

<a id="type-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions"></a>

### `TraefikTrafficMaterializerOptions`

Configures the Traefik IngressRoute control-plane materializer for provider-managed cell traffic automation.

#### Declaration
```csharp
public sealed class TraefikTrafficMaterializerOptions
```

#### Constructors

<a id="member-m-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-ctor"></a>

##### `TraefikTrafficMaterializerOptions`

```csharp
TraefikTrafficMaterializerOptions()
```

Initializes a new instance of the `TraefikTrafficMaterializerOptions` class.

#### Fields

<a id="member-f-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-defaultmaterializerid"></a>

##### `DefaultMaterializerId`

```csharp
const string DefaultMaterializerId
```

Gets the default materializer identifier used by the Traefik traffic materializer.

<a id="member-f-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-defaultproviderid"></a>

##### `DefaultProviderId`

```csharp
const string DefaultProviderId
```

Gets the default provider identifier used by the Traefik traffic materializer.

#### Properties

<a id="member-p-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-entrypoints"></a>

##### `EntryPoints`

```csharp
IList<string> EntryPoints { get; }
```

Gets the default Traefik entry points applied when a route-level override is absent.

<a id="member-p-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-materializerid"></a>

##### `MaterializerId`

```csharp
string MaterializerId { get; set; }
```

Gets or sets the stable materializer identifier that should appear on operator-facing runtime answers.

<a id="member-p-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-priority"></a>

##### `Priority`

```csharp
int Priority { get; set; }
```

Gets or sets the priority used when multiple provider materializers can reconcile the same automation answer. Higher values win while ties still fail deterministically in the engine.

<a id="member-p-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-providerid"></a>

##### `ProviderId`

```csharp
string ProviderId { get; set; }
```

Gets or sets the provider identifier that the materializer owns.

<a id="member-p-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-routenamespace"></a>

##### `RouteNamespace`

```csharp
string RouteNamespace { get; set; }
```

Gets or sets the default Kubernetes namespace that contains projected IngressRoute resources.

<a id="member-p-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions-routes"></a>

##### `Routes`

```csharp
IList<TraefikIngressRouteOptions> Routes { get; }
```

Gets the route-level Traefik IngressRoute projections owned by this materializer.

<a id="namespace-cephalon-edge-traefik-registration"></a>

## Namespace Cephalon.Edge.Traefik.Registration

<a id="type-cephalon-edge-traefik-registration-traefikenginebuilderextensions"></a>

### `TraefikEngineBuilderExtensions`

Registers the Traefik traffic materializer companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class TraefikEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-edge-traefik-registration-traefikenginebuilderextensions-addtraefiktrafficmaterializer-cephalon-engine-composition-enginebuilder-system-action-cephalon-edge-traefik-configuration-traefiktrafficmaterializeroptions"></a>

##### `AddTraefikTrafficMaterializer`

```csharp
EngineBuilder AddTraefikTrafficMaterializer(this EngineBuilder builder, Action<TraefikTrafficMaterializerOptions> configure)
```

Adds the Traefik IngressRoute traffic materializer companion pack to the engine.

Remarks: This pack keeps Traefik-specific control-plane projection outside `Cephalon.Engine` while still reporting provider materialization truth back through the shared `/engine/cell-traffic-automations*`, `/engine/technology-surfaces`, and `snapshot` surfaces.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures how provider-managed cell traffic automation should project into Traefik IngressRoute intent.
