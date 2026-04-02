# Cephalon.Observability.MqttDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.MqttDependencies)
## Namespaces

- `Cephalon.Observability.MqttDependencies.Configuration`
- `Cephalon.Observability.MqttDependencies.Hosting`

<a id="namespace-cephalon-observability-mqttdependencies-configuration"></a>

## Namespace Cephalon.Observability.MqttDependencies.Configuration

<a id="type-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition"></a>

### `MqttDependencyDefinition`

Describes one MQTT dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class MqttDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-ctor"></a>

##### `MqttDependencyDefinition`

```csharp
MqttDependencyDefinition()
```

Initializes a new instance of the `MqttDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-clientid"></a>

##### `ClientId`

```csharp
string ClientId { get; set; }
```

Gets or sets the MQTT client identifier sent in the `CONNECT` packet.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the MQTT host name or IP address to probe.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-keepaliveseconds"></a>

##### `KeepAliveSeconds`

```csharp
int KeepAliveSeconds { get; set; }
```

Gets or sets the MQTT keep-alive interval, in seconds, advertised through the `CONNECT` packet.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for MQTT username/password authentication.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the MQTT broker port.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-tlsservername"></a>

##### `TlsServerName`

```csharp
string TlsServerName { get; set; }
```

Gets or sets the TLS server name used for certificate validation when `UseTls` is enabled.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for MQTT username/password authentication.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencydefinition-usetls"></a>

##### `UseTls`

```csharp
bool UseTls { get; set; }
```

Gets or sets a value indicating whether the probe should use TLS immediately after opening the TCP connection.

<a id="type-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions"></a>

### `MqttDependencyHealthOptions`

Configures MQTT dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class MqttDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions-ctor"></a>

##### `MqttDependencyHealthOptions`

```csharp
MqttDependencyHealthOptions()
```

Initializes a new instance of the `MqttDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<MqttDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured MQTT dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MqttDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds MQTT dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-mqttdependencies-hosting"></a>

## Namespace Cephalon.Observability.MqttDependencies.Hosting

<a id="type-cephalon-observability-mqttdependencies-hosting-mqttdependencyhealthservicecollectionextensions"></a>

### `MqttDependencyHealthServiceCollectionExtensions`

Adds MQTT dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class MqttDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-mqttdependencies-hosting-mqttdependencyhealthservicecollectionextensions-addcephalonmqttdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions"></a>

##### `AddCephalonMqttDependencyHealth`

```csharp
IServiceCollection AddCephalonMqttDependencyHealth(this IServiceCollection services, Action<MqttDependencyHealthOptions> configure)
```

Adds MQTT dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-mqttdependencies-hosting-mqttdependencyhealthservicecollectionextensions-addcephalonmqttdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-mqttdependencies-configuration-mqttdependencyhealthoptions"></a>

##### `AddCephalonMqttDependencyHealth`

```csharp
IServiceCollection AddCephalonMqttDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<MqttDependencyHealthOptions> configure)
```

Adds MQTT dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
