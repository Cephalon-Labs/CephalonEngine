# Cephalon.Observability.ConsulDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.ConsulDependencies)
## Namespaces

- `Cephalon.Observability.ConsulDependencies.Configuration`
- `Cephalon.Observability.ConsulDependencies.Hosting`

<a id="namespace-cephalon-observability-consuldependencies-configuration"></a>

## Namespace Cephalon.Observability.ConsulDependencies.Configuration

<a id="type-cephalon-observability-consuldependencies-configuration-consuldependencydefinition"></a>

### `ConsulDependencyDefinition`

Describes one Consul dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class ConsulDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-ctor"></a>

##### `ConsulDependencyDefinition`

```csharp
ConsulDependencyDefinition()
```

Initializes a new instance of the `ConsulDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-acltoken"></a>

##### `AclToken`

```csharp
string AclToken { get; set; }
```

Gets or sets the optional Consul ACL token sent as the `X-Consul-Token` header.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-datacenter"></a>

##### `Datacenter`

```csharp
string Datacenter { get; set; }
```

Gets or sets the optional Consul datacenter name added as the `dc` query parameter.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the absolute Consul base URL or status endpoint that should be probed.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-request timeout in seconds.

<a id="type-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions"></a>

### `ConsulDependencyHealthOptions`

Configures Consul dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class ConsulDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions-ctor"></a>

##### `ConsulDependencyHealthOptions`

```csharp
ConsulDependencyHealthOptions()
```

Initializes a new instance of the `ConsulDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<ConsulDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured Consul dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
ConsulDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Consul dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-consuldependencies-hosting"></a>

## Namespace Cephalon.Observability.ConsulDependencies.Hosting

<a id="type-cephalon-observability-consuldependencies-hosting-consuldependencyhealthservicecollectionextensions"></a>

### `ConsulDependencyHealthServiceCollectionExtensions`

Adds Consul dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class ConsulDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-consuldependencies-hosting-consuldependencyhealthservicecollectionextensions-addcephalonconsuldependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions"></a>

##### `AddCephalonConsulDependencyHealth`

```csharp
IServiceCollection AddCephalonConsulDependencyHealth(this IServiceCollection services, Action<ConsulDependencyHealthOptions> configure)
```

Adds Consul dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-consuldependencies-hosting-consuldependencyhealthservicecollectionextensions-addcephalonconsuldependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-consuldependencies-configuration-consuldependencyhealthoptions"></a>

##### `AddCephalonConsulDependencyHealth`

```csharp
IServiceCollection AddCephalonConsulDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<ConsulDependencyHealthOptions> configure)
```

Adds Consul dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
