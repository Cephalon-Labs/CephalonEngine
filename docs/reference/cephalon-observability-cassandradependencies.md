# Cephalon.Observability.CassandraDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.CassandraDependencies)
## Namespaces

- `Cephalon.Observability.CassandraDependencies.Configuration`
- `Cephalon.Observability.CassandraDependencies.Hosting`

<a id="namespace-cephalon-observability-cassandradependencies-configuration"></a>

## Namespace Cephalon.Observability.CassandraDependencies.Configuration

<a id="type-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition"></a>

### `CassandraDependencyDefinition`

Describes one Cassandra dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class CassandraDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-ctor"></a>

##### `CassandraDependencyDefinition`

```csharp
CassandraDependencyDefinition()
```

Initializes a new instance of the `CassandraDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-contactpoints"></a>

##### `ContactPoints`

```csharp
IReadOnlyList<string> ContactPoints { get; set; }
```

Gets or sets the Cassandra contact points used to establish the probe session.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the CQL statement executed to verify the dependency.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-keyspace"></a>

##### `Keyspace`

```csharp
string Keyspace { get; set; }
```

Gets or sets the optional Cassandra keyspace used when opening the probe session.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the Cassandra native-protocol TCP port.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication.

<a id="type-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions"></a>

### `CassandraDependencyHealthOptions`

Configures Cassandra dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class CassandraDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions-ctor"></a>

##### `CassandraDependencyHealthOptions`

```csharp
CassandraDependencyHealthOptions()
```

Initializes a new instance of the `CassandraDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<CassandraDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured Cassandra dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
CassandraDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Cassandra dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-cassandradependencies-hosting"></a>

## Namespace Cephalon.Observability.CassandraDependencies.Hosting

<a id="type-cephalon-observability-cassandradependencies-hosting-cassandradependencyhealthservicecollectionextensions"></a>

### `CassandraDependencyHealthServiceCollectionExtensions`

Adds Cassandra dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class CassandraDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-cassandradependencies-hosting-cassandradependencyhealthservicecollectionextensions-addcephaloncassandradependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions"></a>

##### `AddCephalonCassandraDependencyHealth`

```csharp
IServiceCollection AddCephalonCassandraDependencyHealth(this IServiceCollection services, Action<CassandraDependencyHealthOptions> configure)
```

Adds Cassandra dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-cassandradependencies-hosting-cassandradependencyhealthservicecollectionextensions-addcephaloncassandradependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-cassandradependencies-configuration-cassandradependencyhealthoptions"></a>

##### `AddCephalonCassandraDependencyHealth`

```csharp
IServiceCollection AddCephalonCassandraDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<CassandraDependencyHealthOptions> configure)
```

Adds Cassandra dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
