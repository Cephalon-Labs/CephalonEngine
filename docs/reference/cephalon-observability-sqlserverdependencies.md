# Cephalon.Observability.SqlServerDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.SqlServerDependencies)
## Namespaces

- `Cephalon.Observability.SqlServerDependencies.Configuration`
- `Cephalon.Observability.SqlServerDependencies.Hosting`

<a id="namespace-cephalon-observability-sqlserverdependencies-configuration"></a>

## Namespace Cephalon.Observability.SqlServerDependencies.Configuration

<a id="type-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition"></a>

### `SqlServerDependencyDefinition`

Describes one SQL Server dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class SqlServerDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-ctor"></a>

##### `SqlServerDependencyDefinition`

```csharp
SqlServerDependencyDefinition()
```

Initializes a new instance of the `SqlServerDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional full SQL Server connection string used for the probe.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-database"></a>

##### `Database`

```csharp
string Database { get; set; }
```

Gets or sets the database name used for the health query.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-encrypt"></a>

##### `Encrypt`

```csharp
string Encrypt { get; set; }
```

Gets or sets the optional SQL Server encryption mode such as `Optional`, `Mandatory`, or `Strict`.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the SQL statement executed to verify the dependency.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the SQL Server host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the SQL Server TCP port.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-trustservercertificate"></a>

##### `TrustServerCertificate`

```csharp
bool? TrustServerCertificate { get; set; }
```

Gets or sets the optional value that controls whether server certificate validation should be bypassed.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="type-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions"></a>

### `SqlServerDependencyHealthOptions`

Configures SQL Server dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class SqlServerDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions-ctor"></a>

##### `SqlServerDependencyHealthOptions`

```csharp
SqlServerDependencyHealthOptions()
```

Initializes a new instance of the `SqlServerDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<SqlServerDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured SQL Server dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
SqlServerDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds SQL Server dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-sqlserverdependencies-hosting"></a>

## Namespace Cephalon.Observability.SqlServerDependencies.Hosting

<a id="type-cephalon-observability-sqlserverdependencies-hosting-sqlserverdependencyhealthservicecollectionextensions"></a>

### `SqlServerDependencyHealthServiceCollectionExtensions`

Adds SQL Server dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class SqlServerDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-sqlserverdependencies-hosting-sqlserverdependencyhealthservicecollectionextensions-addcephalonsqlserverdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions"></a>

##### `AddCephalonSqlServerDependencyHealth`

```csharp
IServiceCollection AddCephalonSqlServerDependencyHealth(this IServiceCollection services, Action<SqlServerDependencyHealthOptions> configure)
```

Adds SQL Server dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-sqlserverdependencies-hosting-sqlserverdependencyhealthservicecollectionextensions-addcephalonsqlserverdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-sqlserverdependencies-configuration-sqlserverdependencyhealthoptions"></a>

##### `AddCephalonSqlServerDependencyHealth`

```csharp
IServiceCollection AddCephalonSqlServerDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<SqlServerDependencyHealthOptions> configure)
```

Adds SQL Server dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
