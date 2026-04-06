# Cephalon.Observability.NatsDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.NatsDependencies)
## Namespaces

- `Cephalon.Observability.NatsDependencies.Configuration`
- `Cephalon.Observability.NatsDependencies.Hosting`

<a id="namespace-cephalon-observability-natsdependencies-configuration"></a>

## Namespace Cephalon.Observability.NatsDependencies.Configuration

<a id="type-cephalon-observability-natsdependencies-configuration-natsdependencydefinition"></a>

### `NatsDependencyDefinition`

Describes one NATS dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class NatsDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-ctor"></a>

##### `NatsDependencyDefinition`

```csharp
NatsDependencyDefinition()
```

Initializes a new instance of the `NatsDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-clientname"></a>

##### `ClientName`

```csharp
string ClientName { get; set; }
```

Gets or sets the optional client name sent in the NATS `CONNECT` payload.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-host"></a>

##### `Host`

```csharp
string Host { get; set; }
```

Gets or sets the NATS host name or IP address to probe.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for NATS user/password authentication.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-port"></a>

##### `Port`

```csharp
int Port { get; set; }
```

Gets or sets the NATS client port.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-tlsservername"></a>

##### `TlsServerName`

```csharp
string TlsServerName { get; set; }
```

Gets or sets the TLS server name used for certificate validation when `UseTls` is enabled.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-token"></a>

##### `Token`

```csharp
string Token { get; set; }
```

Gets or sets the optional auth token used for token-based NATS authentication.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for NATS user/password authentication.

<a id="member-p-cephalon-observability-natsdependencies-configuration-natsdependencydefinition-usetls"></a>

##### `UseTls`

```csharp
bool UseTls { get; set; }
```

Gets or sets a value indicating whether the probe should upgrade the connection to TLS after reading the initial server info line.

<a id="type-cephalon-observability-natsdependencies-configuration-natsdependencyhealthoptions"></a>

### `NatsDependencyHealthOptions`

Configures NATS dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class NatsDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-natsdependencies-configuration-natsdependencyhealthoptions-ctor"></a>

##### `NatsDependencyHealthOptions`

```csharp
NatsDependencyHealthOptions()
```

Initializes a new instance of the `NatsDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-natsdependencies-configuration-natsdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
NatsDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds NATS dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-natsdependencies-hosting"></a>

## Namespace Cephalon.Observability.NatsDependencies.Hosting

<a id="type-cephalon-observability-natsdependencies-hosting-natsdependencyhealthservicecollectionextensions"></a>

### `NatsDependencyHealthServiceCollectionExtensions`

Adds NATS dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class NatsDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-natsdependencies-hosting-natsdependencyhealthservicecollectionextensions-addcephalonnatsdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-natsdependencies-configuration-natsdependencyhealthoptions"></a>

##### `AddCephalonNatsDependencyHealth`

```csharp
IServiceCollection AddCephalonNatsDependencyHealth(this IServiceCollection services, Action<NatsDependencyHealthOptions> configure)
```

Adds NATS dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-natsdependencies-hosting-natsdependencyhealthservicecollectionextensions-addcephalonnatsdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-natsdependencies-configuration-natsdependencyhealthoptions"></a>

##### `AddCephalonNatsDependencyHealth`

```csharp
IServiceCollection AddCephalonNatsDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<NatsDependencyHealthOptions> configure)
```

Adds NATS dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
