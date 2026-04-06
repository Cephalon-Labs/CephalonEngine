# Cephalon.Observability.OracleDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.OracleDependencies)
## Namespaces

- `Cephalon.Observability.OracleDependencies.Configuration`
- `Cephalon.Observability.OracleDependencies.Hosting`

<a id="namespace-cephalon-observability-oracledependencies-configuration"></a>

## Namespace Cephalon.Observability.OracleDependencies.Configuration

<a id="type-cephalon-observability-oracledependencies-configuration-oracledependencydefinition"></a>

### `OracleDependencyDefinition`

Describes one Oracle dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class OracleDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-ctor"></a>

##### `OracleDependencyDefinition`

```csharp
OracleDependencyDefinition()
```

Initializes a new instance of the `OracleDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional full Oracle connection string used for the probe.

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the SQL statement executed to verify the dependency.

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the Oracle host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the Oracle TCP port.

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-servicename"></a>

##### `ServiceName`

```csharp
string ServiceName { get; set; }
```

Gets or sets the Oracle service name used in the Easy Connect data source when no full connection string is supplied.

<a id="member-p-cephalon-observability-oracledependencies-configuration-oracledependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="type-cephalon-observability-oracledependencies-configuration-oracledependencyhealthoptions"></a>

### `OracleDependencyHealthOptions`

Configures Oracle dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class OracleDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-oracledependencies-configuration-oracledependencyhealthoptions-ctor"></a>

##### `OracleDependencyHealthOptions`

```csharp
OracleDependencyHealthOptions()
```

Initializes a new instance of the `OracleDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-oracledependencies-configuration-oracledependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
OracleDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Oracle dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-oracledependencies-hosting"></a>

## Namespace Cephalon.Observability.OracleDependencies.Hosting

<a id="type-cephalon-observability-oracledependencies-hosting-oracledependencyhealthservicecollectionextensions"></a>

### `OracleDependencyHealthServiceCollectionExtensions`

Adds Oracle dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class OracleDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-oracledependencies-hosting-oracledependencyhealthservicecollectionextensions-addcephalonoracledependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-oracledependencies-configuration-oracledependencyhealthoptions"></a>

##### `AddCephalonOracleDependencyHealth`

```csharp
IServiceCollection AddCephalonOracleDependencyHealth(this IServiceCollection services, Action<OracleDependencyHealthOptions> configure)
```

Adds Oracle dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-oracledependencies-hosting-oracledependencyhealthservicecollectionextensions-addcephalonoracledependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-oracledependencies-configuration-oracledependencyhealthoptions"></a>

##### `AddCephalonOracleDependencyHealth`

```csharp
IServiceCollection AddCephalonOracleDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<OracleDependencyHealthOptions> configure)
```

Adds Oracle dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
