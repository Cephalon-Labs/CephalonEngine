# Cephalon.Identity

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Identity)
## Namespaces

- `Cephalon.Identity.Configuration`
- `Cephalon.Identity.Policies`
- `Cephalon.Identity.Registration`

<a id="namespace-cephalon-identity-configuration"></a>

## Namespace Cephalon.Identity.Configuration

<a id="type-cephalon-identity-configuration-identityruntimeoptions"></a>

### `IdentityRuntimeOptions`

Describes host-agnostic runtime options for the Cephalon identity companion pack.

#### Declaration
```csharp
public sealed class IdentityRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-identity-configuration-identityruntimeoptions-ctor-system-boolean-system-boolean-system-boolean"></a>

##### `IdentityRuntimeOptions`

```csharp
IdentityRuntimeOptions(bool enableDefaultEvaluator, bool enableRuntimeSurface, bool requireExplicitPolicy)
```

Initializes a new instance of the `IdentityRuntimeOptions` class.

Parameters:
- `enableDefaultEvaluator`: Whether the built-in metadata-driven `IAuthorizationEvaluator` should stay active.
- `enableRuntimeSurface`: Whether the companion pack should project identity and authorization runtime metadata through the shared technology surface set.
- `requireExplicitPolicy`: Whether callers must provide an explicit `AuthorizationContext.PolicyId` when using the built-in metadata-driven evaluator.

#### Properties

<a id="member-p-cephalon-identity-configuration-identityruntimeoptions-enabledefaultevaluator"></a>

##### `EnableDefaultEvaluator`

```csharp
bool EnableDefaultEvaluator { get; set; }
```

Gets or sets a value indicating whether the built-in metadata-driven authorization evaluator is active.

<a id="member-p-cephalon-identity-configuration-identityruntimeoptions-enableruntimesurface"></a>

##### `EnableRuntimeSurface`

```csharp
bool EnableRuntimeSurface { get; set; }
```

Gets or sets a value indicating whether the pack should publish a runtime surface under `identity-access`.

<a id="member-p-cephalon-identity-configuration-identityruntimeoptions-requireexplicitpolicy"></a>

##### `RequireExplicitPolicy`

```csharp
bool RequireExplicitPolicy { get; set; }
```

Gets or sets a value indicating whether authorization calls must provide an explicit policy identifier.

#### Methods

<a id="member-m-cephalon-identity-configuration-identityruntimeoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
IdentityRuntimeOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads identity runtime options from configuration.

Returns: The parsed identity runtime options.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="namespace-cephalon-identity-policies"></a>

## Namespace Cephalon.Identity.Policies

<a id="type-cephalon-identity-policies-identitypolicymetadatakeys"></a>

### `IdentityPolicyMetadataKeys`

Defines the metadata keys understood by the built-in Cephalon identity evaluator.

#### Declaration
```csharp
public static class IdentityPolicyMetadataKeys
```

#### Fields

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-contextattributeprefix"></a>

##### `ContextAttributePrefix`

```csharp
const string ContextAttributePrefix
```

The metadata key prefix for required evaluation-context attributes.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-requiredrolematch"></a>

##### `RequiredRoleMatch`

```csharp
const string RequiredRoleMatch
```

The metadata key that controls how `RequiredRoles` should be matched.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-requiredrolematchall"></a>

##### `RequiredRoleMatchAll`

```csharp
const string RequiredRoleMatchAll
```

The value used by `RequiredRoleMatch` when all listed roles must be present.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-requiredrolematchany"></a>

##### `RequiredRoleMatchAny`

```csharp
const string RequiredRoleMatchAny
```

The value used by `RequiredRoleMatch` when any listed role may satisfy the policy.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-requiredroles"></a>

##### `RequiredRoles`

```csharp
const string RequiredRoles
```

The metadata key that lists required subject roles as a comma-separated value.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-requireowner"></a>

##### `RequireOwner`

```csharp
const string RequireOwner
```

The metadata key that requires the current subject to match the resource owner when set to `true`.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-requiretenantmatch"></a>

##### `RequireTenantMatch`

```csharp
const string RequireTenantMatch
```

The metadata key that requires the subject, resource, and context to stay within the same tenant boundary when set to `true`.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-resourceattributeprefix"></a>

##### `ResourceAttributePrefix`

```csharp
const string ResourceAttributePrefix
```

The metadata key prefix for required resource attributes.

<a id="member-f-cephalon-identity-policies-identitypolicymetadatakeys-subjectattributeprefix"></a>

##### `SubjectAttributePrefix`

```csharp
const string SubjectAttributePrefix
```

The metadata key prefix for required subject attributes.

<a id="namespace-cephalon-identity-registration"></a>

## Namespace Cephalon.Identity.Registration

<a id="type-cephalon-identity-registration-identityenginebuilderextensions"></a>

### `IdentityEngineBuilderExtensions`

Registers the host-agnostic Cephalon identity companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class IdentityEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-identity-registration-identityenginebuilderextensions-addidentityaccess-cephalon-engine-composition-enginebuilder-system-action-cephalon-identity-configuration-identityruntimeoptions"></a>

##### `AddIdentityAccess`

```csharp
EngineBuilder AddIdentityAccess(this EngineBuilder builder, Action<IdentityRuntimeOptions> configure)
```

Adds the Cephalon identity companion pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures host-owned identity runtime options.
