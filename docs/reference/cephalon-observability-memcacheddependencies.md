# Cephalon.Observability.MemcachedDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.MemcachedDependencies)
## Namespaces

- `Cephalon.Observability.MemcachedDependencies.Configuration`
- `Cephalon.Observability.MemcachedDependencies.Hosting`

<a id="namespace-cephalon-observability-memcacheddependencies-configuration"></a>

## Namespace Cephalon.Observability.MemcachedDependencies.Configuration

<a id="type-cephalon-observability-memcacheddependencies-configuration-memcacheddependencydefinition"></a>

### `MemcachedDependencyDefinition`

Describes one Memcached dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class MemcachedDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-memcacheddependencies-configuration-memcacheddependencydefinition-ctor"></a>

##### `MemcachedDependencyDefinition`

```csharp
MemcachedDependencyDefinition()
```

Initializes a new instance of the `MemcachedDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-memcacheddependencies-configuration-memcacheddependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the Memcached host name or IP address to probe.

<a id="member-p-cephalon-observability-memcacheddependencies-configuration-memcacheddependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the Memcached TCP port.

<a id="type-cephalon-observability-memcacheddependencies-configuration-memcacheddependencyhealthoptions"></a>

### `MemcachedDependencyHealthOptions`

Configures Memcached dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class MemcachedDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-memcacheddependencies-configuration-memcacheddependencyhealthoptions-ctor"></a>

##### `MemcachedDependencyHealthOptions`

```csharp
MemcachedDependencyHealthOptions()
```

Initializes a new instance of the `MemcachedDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-memcacheddependencies-configuration-memcacheddependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MemcachedDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Memcached dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-memcacheddependencies-hosting"></a>

## Namespace Cephalon.Observability.MemcachedDependencies.Hosting

<a id="type-cephalon-observability-memcacheddependencies-hosting-memcacheddependencyhealthservicecollectionextensions"></a>

### `MemcachedDependencyHealthServiceCollectionExtensions`

Adds Memcached dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class MemcachedDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-memcacheddependencies-hosting-memcacheddependencyhealthservicecollectionextensions-addcephalonmemcacheddependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-memcacheddependencies-configuration-memcacheddependencyhealthoptions"></a>

##### `AddCephalonMemcachedDependencyHealth`

```csharp
IServiceCollection AddCephalonMemcachedDependencyHealth(this IServiceCollection services, Action<MemcachedDependencyHealthOptions> configure)
```

Adds Memcached dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-memcacheddependencies-hosting-memcacheddependencyhealthservicecollectionextensions-addcephalonmemcacheddependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-memcacheddependencies-configuration-memcacheddependencyhealthoptions"></a>

##### `AddCephalonMemcachedDependencyHealth`

```csharp
IServiceCollection AddCephalonMemcachedDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<MemcachedDependencyHealthOptions> configure)
```

Adds Memcached dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
