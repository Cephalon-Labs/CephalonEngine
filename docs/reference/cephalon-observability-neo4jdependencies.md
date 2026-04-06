# Cephalon.Observability.Neo4jDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.Neo4jDependencies)
## Namespaces

- `Cephalon.Observability.Neo4jDependencies.Configuration`
- `Cephalon.Observability.Neo4jDependencies.Hosting`

<a id="namespace-cephalon-observability-neo4jdependencies-configuration"></a>

## Namespace Cephalon.Observability.Neo4jDependencies.Configuration

<a id="type-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition"></a>

### `Neo4jDependencyDefinition`

Describes one Neo4j dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class Neo4jDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-ctor"></a>

##### `Neo4jDependencyDefinition`

```csharp
Neo4jDependencyDefinition()
```

Initializes a new instance of the `Neo4jDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-database"></a>

##### `Database`

```csharp
string Database { get; set; }
```

Gets or sets the optional Neo4j database name used when opening the probe session.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-healthquery"></a>

##### `HealthQuery`

```csharp
string HealthQuery { get; set; }
```

Gets or sets the Cypher statement executed to verify the dependency.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the Neo4j host name or IP address to probe when no full URI is supplied.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the Neo4j Bolt port used when no full URI is supplied.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-scheme"></a>

##### `Scheme`

```csharp
string Scheme { get; set; }
```

Gets or sets the URI scheme used when building a discrete endpoint, such as `neo4j`, `neo4j+s`, `bolt`, or `bolt+s`.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-uri"></a>

##### `Uri`

```csharp
string Uri { get; set; }
```

Gets or sets the optional full Neo4j endpoint URI such as `neo4j://graph.internal.example:7687` or `neo4j+s://graph.internal.example:7687`.

<a id="member-p-cephalon-observability-neo4jdependencies-configuration-neo4jdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication.

<a id="type-cephalon-observability-neo4jdependencies-configuration-neo4jdependencyhealthoptions"></a>

### `Neo4jDependencyHealthOptions`

Configures Neo4j dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class Neo4jDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-neo4jdependencies-configuration-neo4jdependencyhealthoptions-ctor"></a>

##### `Neo4jDependencyHealthOptions`

```csharp
Neo4jDependencyHealthOptions()
```

Initializes a new instance of the `Neo4jDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-neo4jdependencies-configuration-neo4jdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
Neo4jDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Neo4j dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-neo4jdependencies-hosting"></a>

## Namespace Cephalon.Observability.Neo4jDependencies.Hosting

<a id="type-cephalon-observability-neo4jdependencies-hosting-neo4jdependencyhealthservicecollectionextensions"></a>

### `Neo4jDependencyHealthServiceCollectionExtensions`

Adds Neo4j dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class Neo4jDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-neo4jdependencies-hosting-neo4jdependencyhealthservicecollectionextensions-addcephalonneo4jdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-neo4jdependencies-configuration-neo4jdependencyhealthoptions"></a>

##### `AddCephalonNeo4jDependencyHealth`

```csharp
IServiceCollection AddCephalonNeo4jDependencyHealth(this IServiceCollection services, Action<Neo4jDependencyHealthOptions> configure)
```

Adds Neo4j dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-neo4jdependencies-hosting-neo4jdependencyhealthservicecollectionextensions-addcephalonneo4jdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-neo4jdependencies-configuration-neo4jdependencyhealthoptions"></a>

##### `AddCephalonNeo4jDependencyHealth`

```csharp
IServiceCollection AddCephalonNeo4jDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<Neo4jDependencyHealthOptions> configure)
```

Adds Neo4j dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
