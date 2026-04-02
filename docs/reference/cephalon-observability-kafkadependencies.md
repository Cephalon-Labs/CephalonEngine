# Cephalon.Observability.KafkaDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.KafkaDependencies)
## Namespaces

- `Cephalon.Observability.KafkaDependencies.Configuration`
- `Cephalon.Observability.KafkaDependencies.Hosting`

<a id="namespace-cephalon-observability-kafkadependencies-configuration"></a>

## Namespace Cephalon.Observability.KafkaDependencies.Configuration

<a id="type-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition"></a>

### `KafkaDependencyDefinition`

Describes one Kafka dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class KafkaDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-ctor"></a>

##### `KafkaDependencyDefinition`

```csharp
KafkaDependencyDefinition()
```

Initializes a new instance of the `KafkaDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-bootstrapservers"></a>

##### `BootstrapServers`

```csharp
string BootstrapServers { get; set; }
```

Gets or sets the Kafka bootstrap server list, such as `broker-1:9092,broker-2:9092`.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-clientid"></a>

##### `ClientId`

```csharp
string ClientId { get; set; }
```

Gets or sets the optional client identifier sent to the Kafka cluster.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional SASL password used when authenticated broker access is required.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-saslmechanism"></a>

##### `SaslMechanism`

```csharp
string SaslMechanism { get; set; }
```

Gets or sets the optional SASL mechanism, such as `Plain`, `ScramSha256`, or `ScramSha512`.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-securityprotocol"></a>

##### `SecurityProtocol`

```csharp
string SecurityProtocol { get; set; }
```

Gets or sets the optional Kafka security protocol, such as `Plaintext`, `Ssl`, `SaslPlaintext`, or `SaslSsl`.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-topic"></a>

##### `Topic`

```csharp
string Topic { get; set; }
```

Gets or sets the optional topic name that should be present in returned cluster metadata.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional SASL user name used when authenticated broker access is required.

<a id="type-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions"></a>

### `KafkaDependencyHealthOptions`

Configures Kafka dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class KafkaDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions-ctor"></a>

##### `KafkaDependencyHealthOptions`

```csharp
KafkaDependencyHealthOptions()
```

Initializes a new instance of the `KafkaDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<KafkaDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured Kafka dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
KafkaDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Kafka dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-kafkadependencies-hosting"></a>

## Namespace Cephalon.Observability.KafkaDependencies.Hosting

<a id="type-cephalon-observability-kafkadependencies-hosting-kafkadependencyhealthservicecollectionextensions"></a>

### `KafkaDependencyHealthServiceCollectionExtensions`

Adds Kafka dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class KafkaDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-kafkadependencies-hosting-kafkadependencyhealthservicecollectionextensions-addcephalonkafkadependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions"></a>

##### `AddCephalonKafkaDependencyHealth`

```csharp
IServiceCollection AddCephalonKafkaDependencyHealth(this IServiceCollection services, Action<KafkaDependencyHealthOptions> configure)
```

Adds Kafka dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-kafkadependencies-hosting-kafkadependencyhealthservicecollectionextensions-addcephalonkafkadependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-kafkadependencies-configuration-kafkadependencyhealthoptions"></a>

##### `AddCephalonKafkaDependencyHealth`

```csharp
IServiceCollection AddCephalonKafkaDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<KafkaDependencyHealthOptions> configure)
```

Adds Kafka dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
