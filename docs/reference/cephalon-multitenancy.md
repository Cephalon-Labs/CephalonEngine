# Cephalon.MultiTenancy

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.MultiTenancy)
## Namespaces

- `Cephalon.MultiTenancy.Configuration`
- `Cephalon.MultiTenancy.Registration`

<a id="namespace-cephalon-multitenancy-configuration"></a>

## Namespace Cephalon.MultiTenancy.Configuration

<a id="type-cephalon-multitenancy-configuration-multitenancyruntimeoptions"></a>

### `MultiTenancyRuntimeOptions`

Describes host-agnostic runtime options for the Cephalon multi-tenancy companion pack.

#### Declaration
```csharp
public sealed class MultiTenancyRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-multitenancy-configuration-multitenancyruntimeoptions-ctor"></a>

##### `MultiTenancyRuntimeOptions`

```csharp
MultiTenancyRuntimeOptions()
```

Initializes a new instance of the `MultiTenancyRuntimeOptions` class.

#### Properties

<a id="member-p-cephalon-multitenancy-configuration-multitenancyruntimeoptions-defaulttenantid"></a>

##### `DefaultTenantId`

```csharp
string DefaultTenantId { get; set; }
```

Gets or sets the default tenant identifier used when no explicit request hint resolves a tenant.

<a id="member-p-cephalon-multitenancy-configuration-multitenancyruntimeoptions-enabledefaultresolver"></a>

##### `EnableDefaultResolver`

```csharp
bool EnableDefaultResolver { get; set; }
```

Gets or sets a value indicating whether the built-in configuration-driven tenant resolver is active.

<a id="member-p-cephalon-multitenancy-configuration-multitenancyruntimeoptions-tenants"></a>

##### `Tenants`

```csharp
List<TenantContext> Tenants { get; }
```

Gets the configured tenants that the built-in resolver can match by id, key, or domain.

#### Methods

<a id="member-m-cephalon-multitenancy-configuration-multitenancyruntimeoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
MultiTenancyRuntimeOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads multi-tenancy runtime options from configuration.

Returns: The parsed multi-tenancy runtime options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="namespace-cephalon-multitenancy-registration"></a>

## Namespace Cephalon.MultiTenancy.Registration

<a id="type-cephalon-multitenancy-registration-multitenancyenginebuilderextensions"></a>

### `MultiTenancyEngineBuilderExtensions`

Registers the host-agnostic Cephalon multi-tenancy companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class MultiTenancyEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-multitenancy-registration-multitenancyenginebuilderextensions-addmultitenancy-cephalon-engine-composition-enginebuilder-system-action-cephalon-multitenancy-configuration-multitenancyruntimeoptions"></a>

##### `AddMultiTenancy`

```csharp
EngineBuilder AddMultiTenancy(this EngineBuilder builder, Action<MultiTenancyRuntimeOptions> configure)
```

Adds the Cephalon multi-tenancy companion pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures host-owned multi-tenancy runtime options.
