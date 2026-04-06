# Cephalon.Observability.MongoDbDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.MongoDbDependencies)
## Namespaces

- `Cephalon.Observability.MongoDbDependencies.Configuration`
- `Cephalon.Observability.MongoDbDependencies.Hosting`

<a id="namespace-cephalon-observability-mongodbdependencies-configuration"></a>

## Namespace Cephalon.Observability.MongoDbDependencies.Configuration

<a id="type-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition"></a>

### `MongoDbDependencyDefinition`

Describes one MongoDB dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class MongoDbDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-ctor"></a>

##### `MongoDbDependencyDefinition`

```csharp
MongoDbDependencyDefinition()
```

Initializes a new instance of the `MongoDbDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-allowinsecuretls"></a>

##### `AllowInsecureTls`

```csharp
bool? AllowInsecureTls { get; set; }
```

Gets or sets the optional value that controls whether TLS certificate validation should be relaxed.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-authsource"></a>

##### `AuthSource`

```csharp
string AuthSource { get; set; }
```

Gets or sets the optional authentication source used when creating credentials from discrete settings.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional full MongoDB connection string used for the probe.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-database"></a>

##### `Database`

```csharp
string Database { get; set; }
```

Gets or sets the database name used for the health command.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-directconnection"></a>

##### `DirectConnection`

```csharp
bool? DirectConnection { get; set; }
```

Gets or sets the optional value that controls whether the client should connect directly to the target server.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-healthcommand"></a>

##### `HealthCommand`

```csharp
string HealthCommand { get; set; }
```

Gets or sets the MongoDB database command executed to verify the dependency.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the MongoDB host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the MongoDB TCP port.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-mongodbdependencies-configuration-mongodbdependencydefinition-usetls"></a>

##### `UseTls`

```csharp
bool? UseTls { get; set; }
```

Gets or sets the optional value that controls whether TLS should be used for the probe.

<a id="type-cephalon-observability-mongodbdependencies-configuration-mongodbdependencyhealthoptions"></a>

### `MongoDbDependencyHealthOptions`

Configures MongoDB dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class MongoDbDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-mongodbdependencies-configuration-mongodbdependencyhealthoptions-ctor"></a>

##### `MongoDbDependencyHealthOptions`

```csharp
MongoDbDependencyHealthOptions()
```

Initializes a new instance of the `MongoDbDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-mongodbdependencies-configuration-mongodbdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MongoDbDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds MongoDB dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-mongodbdependencies-hosting"></a>

## Namespace Cephalon.Observability.MongoDbDependencies.Hosting

<a id="type-cephalon-observability-mongodbdependencies-hosting-mongodbdependencyhealthservicecollectionextensions"></a>

### `MongoDbDependencyHealthServiceCollectionExtensions`

Adds MongoDB dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class MongoDbDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-mongodbdependencies-hosting-mongodbdependencyhealthservicecollectionextensions-addcephalonmongodbdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-mongodbdependencies-configuration-mongodbdependencyhealthoptions"></a>

##### `AddCephalonMongoDbDependencyHealth`

```csharp
IServiceCollection AddCephalonMongoDbDependencyHealth(this IServiceCollection services, Action<MongoDbDependencyHealthOptions> configure)
```

Adds MongoDB dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-mongodbdependencies-hosting-mongodbdependencyhealthservicecollectionextensions-addcephalonmongodbdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-mongodbdependencies-configuration-mongodbdependencyhealthoptions"></a>

##### `AddCephalonMongoDbDependencyHealth`

```csharp
IServiceCollection AddCephalonMongoDbDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<MongoDbDependencyHealthOptions> configure)
```

Adds MongoDB dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
