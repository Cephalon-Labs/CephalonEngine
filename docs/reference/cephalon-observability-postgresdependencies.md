# Cephalon.Observability.PostgresDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.PostgresDependencies)
## Namespaces

- `Cephalon.Observability.PostgresDependencies.Configuration`
- `Cephalon.Observability.PostgresDependencies.Hosting`

<a id="namespace-cephalon-observability-postgresdependencies-configuration"></a>

## Namespace Cephalon.Observability.PostgresDependencies.Configuration

<a id="type-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition"></a>

### `PostgresDependencyDefinition`

Describes one Postgres dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class PostgresDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-ctor"></a>

##### `PostgresDependencyDefinition`

```csharp
PostgresDependencyDefinition()
```

Initializes a new instance of the `PostgresDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional full Postgres connection string used for the probe.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-database"></a>

##### `Database`

```csharp
string Database { get; set; }
```

Gets or sets the database name used for the health query.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the SQL statement executed to verify the dependency.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the Postgres host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the Postgres TCP port.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-sslmode"></a>

##### `SslMode`

```csharp
string SslMode { get; set; }
```

Gets or sets the optional Postgres SSL mode used when building the probe connection string.

<a id="member-p-cephalon-observability-postgresdependencies-configuration-postgresdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="type-cephalon-observability-postgresdependencies-configuration-postgresdependencyhealthoptions"></a>

### `PostgresDependencyHealthOptions`

Configures Postgres dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class PostgresDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-postgresdependencies-configuration-postgresdependencyhealthoptions-ctor"></a>

##### `PostgresDependencyHealthOptions`

```csharp
PostgresDependencyHealthOptions()
```

Initializes a new instance of the `PostgresDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-postgresdependencies-configuration-postgresdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
PostgresDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Postgres dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-postgresdependencies-hosting"></a>

## Namespace Cephalon.Observability.PostgresDependencies.Hosting

<a id="type-cephalon-observability-postgresdependencies-hosting-postgresdependencyhealthservicecollectionextensions"></a>

### `PostgresDependencyHealthServiceCollectionExtensions`

Adds Postgres dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class PostgresDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-postgresdependencies-hosting-postgresdependencyhealthservicecollectionextensions-addcephalonpostgresdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-postgresdependencies-configuration-postgresdependencyhealthoptions"></a>

##### `AddCephalonPostgresDependencyHealth`

```csharp
IServiceCollection AddCephalonPostgresDependencyHealth(this IServiceCollection services, Action<PostgresDependencyHealthOptions> configure)
```

Adds Postgres dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-postgresdependencies-hosting-postgresdependencyhealthservicecollectionextensions-addcephalonpostgresdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-postgresdependencies-configuration-postgresdependencyhealthoptions"></a>

##### `AddCephalonPostgresDependencyHealth`

```csharp
IServiceCollection AddCephalonPostgresDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<PostgresDependencyHealthOptions> configure)
```

Adds Postgres dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
