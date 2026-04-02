# Cephalon.Observability.ElasticsearchDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.ElasticsearchDependencies)
## Namespaces

- `Cephalon.Observability.ElasticsearchDependencies.Configuration`
- `Cephalon.Observability.ElasticsearchDependencies.Hosting`

<a id="namespace-cephalon-observability-elasticsearchdependencies-configuration"></a>

## Namespace Cephalon.Observability.ElasticsearchDependencies.Configuration

<a id="type-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition"></a>

### `ElasticsearchDependencyDefinition`

Describes one Elasticsearch dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class ElasticsearchDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-ctor"></a>

##### `ElasticsearchDependencyDefinition`

```csharp
ElasticsearchDependencyDefinition()
```

Initializes a new instance of the `ElasticsearchDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-apikey"></a>

##### `ApiKey`

```csharp
string ApiKey { get; set; }
```

Gets or sets the optional API key used for Elasticsearch API-key authentication.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-bearertoken"></a>

##### `BearerToken`

```csharp
string BearerToken { get; set; }
```

Gets or sets the optional bearer token used for Elasticsearch bearer-token authentication.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name shown to operators.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the absolute Elasticsearch base URL or cluster-health endpoint that should be probed.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier surfaced through runtime health endpoints.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for Elasticsearch basic authentication.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-request timeout in seconds.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for Elasticsearch basic authentication.

<a id="type-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions"></a>

### `ElasticsearchDependencyHealthOptions`

Configures Elasticsearch dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class ElasticsearchDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions-ctor"></a>

##### `ElasticsearchDependencyHealthOptions`

```csharp
ElasticsearchDependencyHealthOptions()
```

Initializes a new instance of the `ElasticsearchDependencyHealthOptions` class.

#### Properties

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<ElasticsearchDependencyDefinition> Dependencies { get; set; }
```

Gets or sets the configured Elasticsearch dependencies that should contribute to runtime health.

<a id="member-p-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background refresh attempts.

#### Methods

<a id="member-m-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
ElasticsearchDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds Elasticsearch dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-elasticsearchdependencies-hosting"></a>

## Namespace Cephalon.Observability.ElasticsearchDependencies.Hosting

<a id="type-cephalon-observability-elasticsearchdependencies-hosting-elasticsearchdependencyhealthservicecollectionextensions"></a>

### `ElasticsearchDependencyHealthServiceCollectionExtensions`

Adds Elasticsearch dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class ElasticsearchDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-elasticsearchdependencies-hosting-elasticsearchdependencyhealthservicecollectionextensions-addcephalonelasticsearchdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions"></a>

##### `AddCephalonElasticsearchDependencyHealth`

```csharp
IServiceCollection AddCephalonElasticsearchDependencyHealth(this IServiceCollection services, Action<ElasticsearchDependencyHealthOptions> configure)
```

Adds Elasticsearch dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-elasticsearchdependencies-hosting-elasticsearchdependencyhealthservicecollectionextensions-addcephalonelasticsearchdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-elasticsearchdependencies-configuration-elasticsearchdependencyhealthoptions"></a>

##### `AddCephalonElasticsearchDependencyHealth`

```csharp
IServiceCollection AddCephalonElasticsearchDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<ElasticsearchDependencyHealthOptions> configure)
```

Adds Elasticsearch dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
