# Cephalon.Observability.OpenSearchDependencies

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.OpenSearchDependencies)
## Namespaces

- `Cephalon.Observability.OpenSearchDependencies.Configuration`
- `Cephalon.Observability.OpenSearchDependencies.Hosting`

<a id="namespace-cephalon-observability-opensearchdependencies-configuration"></a>

## Namespace Cephalon.Observability.OpenSearchDependencies.Configuration

<a id="type-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition"></a>

### `OpenSearchDependencyDefinition`

Describes one OpenSearch dependency that should contribute to runtime health.

#### Declaration
```csharp
public sealed class OpenSearchDependencyDefinition
```

#### Constructors

<a id="member-m-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition-ctor"></a>

##### `OpenSearchDependencyDefinition`

```csharp
OpenSearchDependencyDefinition()
```

Initializes a new instance of the `OpenSearchDependencyDefinition` class.

#### Properties

<a id="member-p-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition-bearertoken"></a>

##### `BearerToken`

```csharp
string BearerToken { get; set; }
```

Gets or sets the optional bearer token used for OpenSearch bearer-token authentication.

<a id="member-p-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the absolute OpenSearch base URL or cluster-health endpoint that should be probed.

<a id="member-p-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition-index"></a>

##### `Index`

```csharp
string Index { get; set; }
```

Gets or sets the optional index name or comma-delimited index list that should be checked through the cluster-health API.

<a id="member-p-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition-password"></a>

##### `Password`

```csharp
string Password { get; set; }
```

Gets or sets the optional password used for OpenSearch basic authentication.

<a id="member-p-cephalon-observability-opensearchdependencies-configuration-opensearchdependencydefinition-username"></a>

##### `Username`

```csharp
string Username { get; set; }
```

Gets or sets the optional user name used for OpenSearch basic authentication.

<a id="type-cephalon-observability-opensearchdependencies-configuration-opensearchdependencyhealthoptions"></a>

### `OpenSearchDependencyHealthOptions`

Configures OpenSearch dependency probes contributed to Cephalon runtime health.

#### Declaration
```csharp
public sealed class OpenSearchDependencyHealthOptions
```

#### Constructors

<a id="member-m-cephalon-observability-opensearchdependencies-configuration-opensearchdependencyhealthoptions-ctor"></a>

##### `OpenSearchDependencyHealthOptions`

```csharp
OpenSearchDependencyHealthOptions()
```

Initializes a new instance of the `OpenSearchDependencyHealthOptions` class.

#### Methods

<a id="member-m-cephalon-observability-opensearchdependencies-configuration-opensearchdependencyhealthoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
OpenSearchDependencyHealthOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds OpenSearch dependency-health options from configuration.

Returns: The bound dependency-health options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-opensearchdependencies-hosting"></a>

## Namespace Cephalon.Observability.OpenSearchDependencies.Hosting

<a id="type-cephalon-observability-opensearchdependencies-hosting-opensearchdependencyhealthservicecollectionextensions"></a>

### `OpenSearchDependencyHealthServiceCollectionExtensions`

Adds OpenSearch dependency-health services to a Cephalon host.

#### Declaration
```csharp
public static class OpenSearchDependencyHealthServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-observability-opensearchdependencies-hosting-opensearchdependencyhealthservicecollectionextensions-addcephalonopensearchdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-observability-opensearchdependencies-configuration-opensearchdependencyhealthoptions"></a>

##### `AddCephalonOpenSearchDependencyHealth`

```csharp
IServiceCollection AddCephalonOpenSearchDependencyHealth(this IServiceCollection services, Action<OpenSearchDependencyHealthOptions> configure)
```

Adds OpenSearch dependency-health services using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: An optional callback that configures dependency-health options.

<a id="member-m-cephalon-observability-opensearchdependencies-hosting-opensearchdependencyhealthservicecollectionextensions-addcephalonopensearchdependencyhealth-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-observability-opensearchdependencies-configuration-opensearchdependencyhealthoptions"></a>

##### `AddCephalonOpenSearchDependencyHealth`

```csharp
IServiceCollection AddCephalonOpenSearchDependencyHealth(this IServiceCollection services, IConfiguration configuration, Action<OpenSearchDependencyHealthOptions> configure)
```

Adds OpenSearch dependency-health services using configuration as the primary source of probe settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven dependency-health setup.
