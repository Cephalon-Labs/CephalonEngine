# Cephalon.Observability.ClickHouseDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.ClickHouseDependencies)
## Namespaces

- `Cephalon.Observability.ClickHouseDependencies.Configuration`
- `Cephalon.Observability.ClickHouseDependencies.Hosting`

<a id="namespace-cephalon-observability-clickhousedependencies-configuration"></a>

## Namespace Cephalon.Observability.ClickHouseDependencies.Configuration

<a id="type-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition"></a>

### `ClickHouseDependencyDefinition`

Describes one ClickHouse dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class ClickHouseDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-ctor"></a>

##### `ClickHouseDependencyDefinition`

```csharp
ClickHouseDependencyDefinition()
```

Initializes a new instance of the `ClickHouseDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional full ClickHouse connection string used for the probe.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-database"></a>

##### `Database`

```csharp
string Database { get; set; }
```

Gets or sets the optional ClickHouse database to select for the probe session.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the SQL statement executed to verify the dependency.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the ClickHouse host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the ClickHouse TCP port used by the selected protocol.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-protocol"></a>

##### `Protocol`

```csharp
string Protocol { get; set; }
```

Gets or sets the ClickHouse protocol used for the probe, such as `http` or `https`.

<a id="member-p-cephalon-observability-clickhousedependencies-configuration-clickhousedependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="type-cephalon-observability-clickhousedependencies-configuration-clickhousedependencyhealthoptions"></a>

### `ClickHouseDependencyHealthOptions`

Configures ClickHouse dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class ClickHouseDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-clickhousedependencies-configuration-clickhousedependencyhealthoptions-ctor"></a>

##### `ClickHouseDependencyHealthOptions`

```csharp
ClickHouseDependencyHealthOptions()
```

Initializes a new instance of the `ClickHouseDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-clickhousedependencies-configuration-clickhousedependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
ClickHouseDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds ClickHouse dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-clickhousedependencies-hosting"></a>

## Namespace Cephalon.Observability.ClickHouseDependencies.Hosting

<a id="type-cephalon-observability-clickhousedependencies-hosting-clickhousedependencyhealthservicecollectionextensions"></a>

### `ClickHouseDependencyHealthServiceCollectionExtensions`

Adds ClickHouse dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class ClickHouseDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-clickhousedependencies-hosting-clickhousedependencyhealthservicecollectionextensions-addcephalonclickhousedependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-clickhousedependencies-configuration-clickhousedependencyhealthoptions"></a>

##### `AddCephalonClickHouseDependencyHealth`

```csharp
IServiceCollection AddCephalonClickHouseDependencyHealth(this IServiceCollection services, Action<ClickHouseDependencyHealthOptions> configure)
```

Adds ClickHouse dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-clickhousedependencies-hosting-clickhousedependencyhealthservicecollectionextensions-addcephalonclickhousedependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-clickhousedependencies-configuration-clickhousedependencyhealthoptions"></a>

##### `AddCephalonClickHouseDependencyHealth`

```csharp
IServiceCollection AddCephalonClickHouseDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<ClickHouseDependencyHealthOptions> configure)
```

Adds ClickHouse dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
