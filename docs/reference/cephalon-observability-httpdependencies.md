# Cephalon.Observability.HttpDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.HttpDependencies)
## Namespaces

- `Cephalon.Observability.HttpDependencies.Configuration`
- `Cephalon.Observability.HttpDependencies.Hosting`

<a id="namespace-cephalon-observability-httpdependencies-configuration"></a>

## Namespace Cephalon.Observability.HttpDependencies.Configuration

<a id="type-cephalon-observability-httpdependencies-configuration-httpdependencydefinition"></a>

### `HttpDependencyDefinition`

Describes one external HTTP dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class HttpDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-ctor"></a>

##### `HttpDependencyDefinition`

```csharp
HttpDependencyDefinition()
```

Initializes a new instance of the `HttpDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the absolute endpoint that should be probed for this dependency.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-expectedstatuscodes"></a>

##### `ExpectedStatusCodes`

```csharp
IReadOnlyList<int> ExpectedStatusCodes { get; set; }
```

Gets or sets the explicit HTTP status codes that should be treated as healthy.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-method"></a>

##### `Method`

```csharp
string Method { get; set; }
```

Gets or sets the HTTP method used for the probe request.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-request timeout in seconds.

<a id="type-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions"></a>

### `HttpDependencyHealthOptions`

Configures HTTP and external API dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class HttpDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions-ctor"></a>

##### `HttpDependencyHealthOptions`

```csharp
HttpDependencyHealthOptions()
```

Initializes a new instance of the `HttpDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<HttpDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured HTTP dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
HttpDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds HTTP dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-httpdependencies-hosting"></a>

## Namespace Cephalon.Observability.HttpDependencies.Hosting

<a id="type-cephalon-observability-httpdependencies-hosting-httpdependencyhealthservicecollectionextensions"></a>

### `HttpDependencyHealthServiceCollectionExtensions`

Adds HTTP and external API dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class HttpDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-httpdependencies-hosting-httpdependencyhealthservicecollectionextensions-addcephalonhttpdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions"></a>

##### `AddCephalonHttpDependencyHealth`

```csharp
IServiceCollection AddCephalonHttpDependencyHealth(this IServiceCollection services, Action<HttpDependencyHealthOptions> configure)
```

Adds HTTP dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-httpdependencies-hosting-httpdependencyhealthservicecollectionextensions-addcephalonhttpdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-httpdependencies-configuration-httpdependencyhealthoptions"></a>

##### `AddCephalonHttpDependencyHealth`

```csharp
IServiceCollection AddCephalonHttpDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<HttpDependencyHealthOptions> configure)
```

Adds HTTP dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
