# Cephalon.Observability.MySqlDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.MySqlDependencies)
## Namespaces

- `Cephalon.Observability.MySqlDependencies.Configuration`
- `Cephalon.Observability.MySqlDependencies.Hosting`

<a id="namespace-cephalon-observability-mysqldependencies-configuration"></a>

## Namespace Cephalon.Observability.MySqlDependencies.Configuration

<a id="type-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition"></a>

### `MySqlDependencyDefinition`

Describes one MySQL dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class MySqlDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-ctor"></a>

##### `MySqlDependencyDefinition`

```csharp
MySqlDependencyDefinition()
```

Initializes a new instance of the `MySqlDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-allowpublickeyretrieval"></a>

##### `AllowPublicKeyRetrieval`

```csharp
bool? AllowPublicKeyRetrieval { get; set; }
```

Gets or sets the optional value that controls whether the server RSA public key may be requested automatically.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional full MySQL connection string used for the probe.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-database"></a>

##### `Database`

```csharp
string Database { get; set; }
```

Gets or sets the database name used for the health query.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the SQL statement executed to verify the dependency.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the MySQL host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the MySQL TCP port.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-sslmode"></a>

##### `SslMode`

```csharp
string SslMode { get; set; }
```

Gets or sets the optional MySQL SSL mode such as `Preferred`, `Required`, `VerifyCA`, or `VerifyFull`.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="type-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions"></a>

### `MySqlDependencyHealthOptions`

Configures MySQL dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class MySqlDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions-ctor"></a>

##### `MySqlDependencyHealthOptions`

```csharp
MySqlDependencyHealthOptions()
```

Initializes a new instance of the `MySqlDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<MySqlDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured MySQL dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MySqlDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds MySQL dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-mysqldependencies-hosting"></a>

## Namespace Cephalon.Observability.MySqlDependencies.Hosting

<a id="type-cephalon-observability-mysqldependencies-hosting-mysqldependencyhealthservicecollectionextensions"></a>

### `MySqlDependencyHealthServiceCollectionExtensions`

Adds MySQL dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class MySqlDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-mysqldependencies-hosting-mysqldependencyhealthservicecollectionextensions-addcephalonmysqldependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions"></a>

##### `AddCephalonMySqlDependencyHealth`

```csharp
IServiceCollection AddCephalonMySqlDependencyHealth(this IServiceCollection services, Action<MySqlDependencyHealthOptions> configure)
```

Adds MySQL dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-mysqldependencies-hosting-mysqldependencyhealthservicecollectionextensions-addcephalonmysqldependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-mysqldependencies-configuration-mysqldependencyhealthoptions"></a>

##### `AddCephalonMySqlDependencyHealth`

```csharp
IServiceCollection AddCephalonMySqlDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<MySqlDependencyHealthOptions> configure)
```

Adds MySQL dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
