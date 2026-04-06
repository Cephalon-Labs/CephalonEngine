# Cephalon.Observability.RedisDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.RedisDependencies)
## Namespaces

- `Cephalon.Observability.RedisDependencies.Configuration`
- `Cephalon.Observability.RedisDependencies.Hosting`

<a id="namespace-cephalon-observability-redisdependencies-configuration"></a>

## Namespace Cephalon.Observability.RedisDependencies.Configuration

<a id="type-cephalon-observability-redisdependencies-configuration-redisdependencydefinition"></a>

### `RedisDependencyDefinition`

Describes one Redis dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class RedisDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-redisdependencies-configuration-redisdependencydefinition-ctor"></a>

##### `RedisDependencyDefinition`

```csharp
RedisDependencyDefinition()
```

Initializes a new instance of the `RedisDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-redisdependencies-configuration-redisdependencydefinition-database"></a>

##### `Database`

```csharp
int? Database { get; set; }
```

Gets or sets the optional Redis logical database index to select before pinging.

<a id="member-p-cephalon-observability-redisdependencies-configuration-redisdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the Redis host name or IP address to probe.

<a id="member-p-cephalon-observability-redisdependencies-configuration-redisdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional Redis password used for authentication.

<a id="member-p-cephalon-observability-redisdependencies-configuration-redisdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the Redis TCP port.

<a id="member-p-cephalon-observability-redisdependencies-configuration-redisdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional Redis ACL user name used for authentication.

<a id="type-cephalon-observability-redisdependencies-configuration-redisdependencyhealthoptions"></a>

### `RedisDependencyHealthOptions`

Configures Redis dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class RedisDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-redisdependencies-configuration-redisdependencyhealthoptions-ctor"></a>

##### `RedisDependencyHealthOptions`

```csharp
RedisDependencyHealthOptions()
```

Initializes a new instance of the `RedisDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-redisdependencies-configuration-redisdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
RedisDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Redis dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-redisdependencies-hosting"></a>

## Namespace Cephalon.Observability.RedisDependencies.Hosting

<a id="type-cephalon-observability-redisdependencies-hosting-redisdependencyhealthservicecollectionextensions"></a>

### `RedisDependencyHealthServiceCollectionExtensions`

Adds Redis dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class RedisDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-redisdependencies-hosting-redisdependencyhealthservicecollectionextensions-addcephalonredisdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-redisdependencies-configuration-redisdependencyhealthoptions"></a>

##### `AddCephalonRedisDependencyHealth`

```csharp
IServiceCollection AddCephalonRedisDependencyHealth(this IServiceCollection services, Action<RedisDependencyHealthOptions> configure)
```

Adds Redis dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-redisdependencies-hosting-redisdependencyhealthservicecollectionextensions-addcephalonredisdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-redisdependencies-configuration-redisdependencyhealthoptions"></a>

##### `AddCephalonRedisDependencyHealth`

```csharp
IServiceCollection AddCephalonRedisDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<RedisDependencyHealthOptions> configure)
```

Adds Redis dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
