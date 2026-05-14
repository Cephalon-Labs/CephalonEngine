# Cephalon.Observability.DependencyHealth.Core

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.DependencyHealth.Core)
## Namespaces

- `Cephalon.Observability.DependencyHealth.Core.Configuration`

<a id="namespace-cephalon-observability-dependencyhealth-core-configuration"></a>

## Namespace Cephalon.Observability.DependencyHealth.Core.Configuration

<a id="type-cephalon-observability-dependencyhealth-core-configuration-dependencydefinitionbase"></a>

### `DependencyDefinitionBase`

Base class for all dependency definitions that contribute to Cephalon runtime health.

#### Declaration
```csharp
public abstract class DependencyDefinitionBase
```

#### Properties

<a id="member-p-cephalon-observability-dependencyhealth-core-configuration-dependencydefinitionbase-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the human-readable dependency name.

<a id="member-p-cephalon-observability-dependencyhealth-core-configuration-dependencydefinitionbase-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable dependency identifier.

<a id="member-p-cephalon-observability-dependencyhealth-core-configuration-dependencydefinitionbase-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

Gets or sets a value indicating whether this dependency is required for readiness.

<a id="member-p-cephalon-observability-dependencyhealth-core-configuration-dependencydefinitionbase-timeoutseconds"></a>

##### `TimeoutSeconds`

```csharp
int TimeoutSeconds { get; set; }
```

Gets or sets the per-probe timeout in seconds.

<a id="type-cephalon-observability-dependencyhealth-core-configuration-dependencyhealthoptionsbase-tdefinition"></a>

### `DependencyHealthOptionsBase<TDefinition>`

Base class for provider-specific dependency-health options.

#### Declaration
```csharp
public abstract class DependencyHealthOptionsBase<TDefinition>
```

#### Properties

<a id="member-p-cephalon-observability-dependencyhealth-core-configuration-dependencyhealthoptionsbase-1-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<TDefinition> Dependencies { get; set; }
```

Gets or sets the configured dependencies.

<a id="member-p-cephalon-observability-dependencyhealth-core-configuration-dependencyhealthoptionsbase-1-refreshintervalseconds"></a>

##### `RefreshIntervalSeconds`

```csharp
int RefreshIntervalSeconds { get; set; }
```

Gets or sets the interval in seconds between background refresh attempts.
