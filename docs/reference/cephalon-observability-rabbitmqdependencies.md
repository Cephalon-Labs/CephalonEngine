# Cephalon.Observability.RabbitMqDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.RabbitMqDependencies)
## Namespaces

- `Cephalon.Observability.RabbitMqDependencies.Configuration`
- `Cephalon.Observability.RabbitMqDependencies.Hosting`

<a id="namespace-cephalon-observability-rabbitmqdependencies-configuration"></a>

## Namespace Cephalon.Observability.RabbitMqDependencies.Configuration

<a id="type-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition"></a>

### `RabbitMqDependencyDefinition`

Describes one RabbitMQ dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class RabbitMqDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-ctor"></a>

##### `RabbitMqDependencyDefinition`

```csharp
RabbitMqDependencyDefinition()
```

Initializes a new instance of the `RabbitMqDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the optional AMQP connection string used for the probe.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the RabbitMQ host name or IP address to probe when no full connection string is supplied.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the RabbitMQ TCP port.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for authentication when no full connection string is supplied.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-usetls"></a>

##### `UseTls`

```csharp
bool UseTls { get; set; }
```

Gets or sets a value indicating whether TLS should be enabled for the broker probe.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencydefinition-virtualhost"></a>

##### `VirtualHost`

```csharp
string VirtualHost { get; set; }
```

Gets or sets the RabbitMQ virtual host used for the probe connection.

<a id="type-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions"></a>

### `RabbitMqDependencyHealthOptions`

Configures RabbitMQ dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class RabbitMqDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions-ctor"></a>

##### `RabbitMqDependencyHealthOptions`

```csharp
RabbitMqDependencyHealthOptions()
```

Initializes a new instance of the `RabbitMqDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<RabbitMqDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured RabbitMQ dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
RabbitMqDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds RabbitMQ dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-rabbitmqdependencies-hosting"></a>

## Namespace Cephalon.Observability.RabbitMqDependencies.Hosting

<a id="type-cephalon-observability-rabbitmqdependencies-hosting-rabbitmqdependencyhealthservicecollectionextensions"></a>

### `RabbitMqDependencyHealthServiceCollectionExtensions`

Adds RabbitMQ dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class RabbitMqDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-rabbitmqdependencies-hosting-rabbitmqdependencyhealthservicecollectionextensions-addcephalonrabbitmqdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions"></a>

##### `AddCephalonRabbitMqDependencyHealth`

```csharp
IServiceCollection AddCephalonRabbitMqDependencyHealth(this IServiceCollection services, Action<RabbitMqDependencyHealthOptions> configure)
```

Adds RabbitMQ dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-rabbitmqdependencies-hosting-rabbitmqdependencyhealthservicecollectionextensions-addcephalonrabbitmqdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-rabbitmqdependencies-configuration-rabbitmqdependencyhealthoptions"></a>

##### `AddCephalonRabbitMqDependencyHealth`

```csharp
IServiceCollection AddCephalonRabbitMqDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<RabbitMqDependencyHealthOptions> configure)
```

Adds RabbitMQ dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
