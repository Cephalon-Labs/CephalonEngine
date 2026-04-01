# Cephalon.Abstractions

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Abstractions)
## Namespaces

- `Cephalon.Abstractions.AppModel`
- `Cephalon.Abstractions.AppModel.Scaffolding`
- `Cephalon.Abstractions.Capabilities`
- `Cephalon.Abstractions.Health`
- `Cephalon.Abstractions.Localization`
- `Cephalon.Abstractions.Modules`
- `Cephalon.Abstractions.Patterns`
- `Cephalon.Abstractions.Technologies`
- `Cephalon.Abstractions.Transports`

<a id="namespace-cephalon-abstractions-appmodel"></a>

## Namespace Cephalon.Abstractions.AppModel

<a id="type-cephalon-abstractions-appmodel-appblueprint"></a>

### `AppBlueprint`

#### Declaration
```csharp
public sealed class AppBlueprint
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-appblueprint-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-cephalon-abstractions-patterns-patterndescriptor-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `AppBlueprint`

```csharp
AppBlueprint(string id, string displayName, string description, IReadOnlyList<PatternDescriptor> patterns, IReadOnlyDictionary<string, string> metadata)
```

<a id="member-m-cephalon-abstractions-appmodel-appblueprint-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-cephalon-abstractions-patterns-patterndescriptor-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `AppBlueprint`

```csharp
AppBlueprint(string id, string displayName, string description, IReadOnlyList<PatternDescriptor> patterns, ScaffoldPlan scaffold, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-patterns"></a>

##### `Patterns`

```csharp
IReadOnlyList<PatternDescriptor> Patterns { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appblueprint-scaffold"></a>

##### `Scaffold`

```csharp
ScaffoldPlan Scaffold { get; }
```

<a id="type-cephalon-abstractions-appmodel-appprofile"></a>

### `AppProfile`

#### Declaration
```csharp
public sealed class AppProfile
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-cephalon-abstractions-patterns-patterndescriptor-system-collections-generic-ireadonlylist-1-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-1-cephalon-abstractions-transports-transportdescriptor"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports)
```

<a id="member-m-cephalon-abstractions-appmodel-appprofile-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-cephalon-abstractions-patterns-patterndescriptor-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-system-collections-generic-ireadonlylist-1-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-1-cephalon-abstractions-transports-transportdescriptor"></a>

##### `AppProfile`

```csharp
AppProfile(string blueprintId, string blueprintDisplayName, string blueprintDescription, IReadOnlyList<PatternDescriptor> patterns, ScaffoldPlan scaffold, IReadOnlyList<TechnologyDescriptor> technologies, IReadOnlyList<TransportDescriptor> transports)
```

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-appprofile-blueprintdescription"></a>

##### `BlueprintDescription`

```csharp
string BlueprintDescription { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appprofile-blueprintdisplayname"></a>

##### `BlueprintDisplayName`

```csharp
string BlueprintDisplayName { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appprofile-blueprintid"></a>

##### `BlueprintId`

```csharp
string BlueprintId { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appprofile-patterns"></a>

##### `Patterns`

```csharp
IReadOnlyList<PatternDescriptor> Patterns { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appprofile-scaffold"></a>

##### `Scaffold`

```csharp
ScaffoldPlan Scaffold { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appprofile-technologies"></a>

##### `Technologies`

```csharp
IReadOnlyList<TechnologyDescriptor> Technologies { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-appprofile-transports"></a>

##### `Transports`

```csharp
IReadOnlyList<TransportDescriptor> Transports { get; }
```

<a id="namespace-cephalon-abstractions-appmodel-scaffolding"></a>

## Namespace Cephalon.Abstractions.AppModel.Scaffolding

<a id="type-cephalon-abstractions-appmodel-scaffolding-projectroles"></a>

### `ProjectRoles`

#### Declaration
```csharp
public static class ProjectRoles
```

#### Fields

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-contracts"></a>

##### `Contracts`

```csharp
const string Contracts
```

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-foundation"></a>

##### `Foundation`

```csharp
const string Foundation
```

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-host"></a>

##### `Host`

```csharp
const string Host
```

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-module"></a>

##### `Module`

```csharp
const string Module
```

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-projectroles-tests"></a>

##### `Tests`

```csharp
const string Tests
```

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder"></a>

### `ScaffoldFolder`

#### Declaration
```csharp
public sealed class ScaffoldFolder
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `ScaffoldFolder`

```csharp
ScaffoldFolder(string pathTemplate, string purpose, string scope, string projectId, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-pathtemplate"></a>

##### `PathTemplate`

```csharp
string PathTemplate { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-projectid"></a>

##### `ProjectId`

```csharp
string ProjectId { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-purpose"></a>

##### `Purpose`

```csharp
string Purpose { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldplan"></a>

### `ScaffoldPlan`

#### Declaration
```csharp
public sealed class ScaffoldPlan
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-system-collections-generic-ireadonlylist-1-cephalon-abstractions-appmodel-scaffolding-scaffoldfolder-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `ScaffoldPlan`

```csharp
ScaffoldPlan(string id, string displayName, string description, IReadOnlyList<ScaffoldProject> projects, IReadOnlyList<ScaffoldFolder> folders, IReadOnlyList<string> conventions, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-conventions"></a>

##### `Conventions`

```csharp
IReadOnlyList<string> Conventions { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-folders"></a>

##### `Folders`

```csharp
IReadOnlyList<ScaffoldFolder> Folders { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldplan-projects"></a>

##### `Projects`

```csharp
IReadOnlyList<ScaffoldProject> Projects { get; }
```

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldproject"></a>

### `ScaffoldProject`

#### Declaration
```csharp
public sealed class ScaffoldProject
```

#### Constructors

<a id="member-m-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `ScaffoldProject`

```csharp
ScaffoldProject(string id, string nameTemplate, string pathTemplate, string scope, string role, string template, IReadOnlyList<string> dependsOn, IReadOnlyList<string> packages, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-dependson"></a>

##### `DependsOn`

```csharp
IReadOnlyList<string> DependsOn { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-nametemplate"></a>

##### `NameTemplate`

```csharp
string NameTemplate { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-packages"></a>

##### `Packages`

```csharp
IReadOnlyList<string> Packages { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-pathtemplate"></a>

##### `PathTemplate`

```csharp
string PathTemplate { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-role"></a>

##### `Role`

```csharp
string Role { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

<a id="member-p-cephalon-abstractions-appmodel-scaffolding-scaffoldproject-template"></a>

##### `Template`

```csharp
string Template { get; }
```

<a id="type-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes"></a>

### `ScaffoldScopes`

#### Declaration
```csharp
public static class ScaffoldScopes
```

#### Fields

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-feature"></a>

##### `Feature`

```csharp
const string Feature
```

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-module"></a>

##### `Module`

```csharp
const string Module
```

<a id="member-f-cephalon-abstractions-appmodel-scaffolding-scaffoldscopes-solution"></a>

##### `Solution`

```csharp
const string Solution
```

<a id="namespace-cephalon-abstractions-capabilities"></a>

## Namespace Cephalon.Abstractions.Capabilities

<a id="type-cephalon-abstractions-capabilities-capability"></a>

### `Capability`

#### Declaration
```csharp
public sealed class Capability
```

#### Constructors

<a id="member-m-cephalon-abstractions-capabilities-capability-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `Capability`

```csharp
Capability(string key, string displayName, string description, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-capabilities-capability-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-capabilities-capability-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-capabilities-capability-key"></a>

##### `Key`

```csharp
string Key { get; }
```

<a id="member-p-cephalon-abstractions-capabilities-capability-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="type-cephalon-abstractions-capabilities-capabilityaccess"></a>

### `CapabilityAccess`

#### Declaration
```csharp
public enum CapabilityAccess
```

#### Fields

<a id="member-f-cephalon-abstractions-capabilities-capabilityaccess-allowed"></a>

##### `Allowed`

```csharp
const CapabilityAccess Allowed
```

<a id="member-f-cephalon-abstractions-capabilities-capabilityaccess-denied"></a>

##### `Denied`

```csharp
const CapabilityAccess Denied
```

<a id="member-f-cephalon-abstractions-capabilities-capabilityaccess-trustedonly"></a>

##### `TrustedOnly`

```csharp
const CapabilityAccess TrustedOnly
```

<a id="type-cephalon-abstractions-capabilities-icapabilityregistry"></a>

### `ICapabilityRegistry`

#### Declaration
```csharp
public interface ICapabilityRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-capabilities-icapabilityregistry-add-cephalon-abstractions-capabilities-capability"></a>

##### `Add`

```csharp
void Add(Capability capability)
```

<a id="namespace-cephalon-abstractions-health"></a>

## Namespace Cephalon.Abstractions.Health

<a id="type-cephalon-abstractions-health-dependencyhealthreport"></a>

### `DependencyHealthReport`

#### Declaration
```csharp
public sealed class DependencyHealthReport
```

#### Constructors

<a id="member-m-cephalon-abstractions-health-dependencyhealthreport-ctor-system-string-system-string-cephalon-abstractions-health-healthstate-system-string-system-boolean-system-string"></a>

##### `DependencyHealthReport`

```csharp
DependencyHealthReport(string Id, string DisplayName, HealthState State, string Description, bool Required, string Source)
```

#### Properties

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-required"></a>

##### `Required`

```csharp
bool Required { get; set; }
```

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

<a id="member-p-cephalon-abstractions-health-dependencyhealthreport-state"></a>

##### `State`

```csharp
HealthState State { get; set; }
```

<a id="type-cephalon-abstractions-health-healthstate"></a>

### `HealthState`

#### Declaration
```csharp
public enum HealthState
```

#### Fields

<a id="member-f-cephalon-abstractions-health-healthstate-degraded"></a>

##### `Degraded`

```csharp
const HealthState Degraded
```

<a id="member-f-cephalon-abstractions-health-healthstate-healthy"></a>

##### `Healthy`

```csharp
const HealthState Healthy
```

<a id="member-f-cephalon-abstractions-health-healthstate-unhealthy"></a>

##### `Unhealthy`

```csharp
const HealthState Unhealthy
```

<a id="type-cephalon-abstractions-health-idependencyhealthcontributor"></a>

### `IDependencyHealthContributor`

#### Declaration
```csharp
public interface IDependencyHealthContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-health-idependencyhealthcontributor-getdependencyhealth"></a>

##### `GetDependencyHealth`

```csharp
IReadOnlyList<DependencyHealthReport> GetDependencyHealth()
```

<a id="namespace-cephalon-abstractions-localization"></a>

## Namespace Cephalon.Abstractions.Localization

<a id="type-cephalon-abstractions-localization-ilocalizedresourcecontributor"></a>

### `ILocalizedResourceContributor`

#### Declaration
```csharp
public interface ILocalizedResourceContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-localization-ilocalizedresourcecontributor-registerresources-cephalon-abstractions-localization-ilocalizedresourceregistry"></a>

##### `RegisterResources`

```csharp
void RegisterResources(ILocalizedResourceRegistry resources)
```

<a id="type-cephalon-abstractions-localization-ilocalizedresourceregistry"></a>

### `ILocalizedResourceRegistry`

#### Declaration
```csharp
public interface ILocalizedResourceRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-localization-ilocalizedresourceregistry-add-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `Add`

```csharp
void Add(string culture, IReadOnlyDictionary<string, string> resources)
```

<a id="member-m-cephalon-abstractions-localization-ilocalizedresourceregistry-add-system-string-system-string-system-string"></a>

##### `Add`

```csharp
void Add(string culture, string key, string value)
```

<a id="type-cephalon-abstractions-localization-ilocalizedtextcatalog"></a>

### `ILocalizedTextCatalog`

#### Declaration
```csharp
public interface ILocalizedTextCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-localization-ilocalizedtextcatalog-defaultculture"></a>

##### `DefaultCulture`

```csharp
string DefaultCulture { get; }
```

<a id="member-p-cephalon-abstractions-localization-ilocalizedtextcatalog-supportedcultures"></a>

##### `SupportedCultures`

```csharp
IReadOnlyList<string> SupportedCultures { get; }
```

#### Methods

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-createsnapshot-system-string"></a>

##### `CreateSnapshot`

```csharp
LocalizedResourcesSnapshot CreateSnapshot(string culture)
```

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-getresources-system-string"></a>

##### `GetResources`

```csharp
IReadOnlyDictionary<string, string> GetResources(string culture)
```

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-resolvetext-system-string-system-string-system-string"></a>

##### `ResolveText`

```csharp
string ResolveText(string key, string culture, string fallback)
```

<a id="member-m-cephalon-abstractions-localization-ilocalizedtextcatalog-tryget-system-string-system-string-system-string"></a>

##### `TryGet`

```csharp
bool TryGet(string key, string culture, out string value)
```

<a id="type-cephalon-abstractions-localization-localizedresourcessnapshot"></a>

### `LocalizedResourcesSnapshot`

#### Declaration
```csharp
public sealed class LocalizedResourcesSnapshot
```

#### Constructors

<a id="member-m-cephalon-abstractions-localization-localizedresourcessnapshot-ctor-system-string-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `LocalizedResourcesSnapshot`

```csharp
LocalizedResourcesSnapshot(string defaultCulture, string resolvedCulture, IReadOnlyList<string> supportedCultures, IReadOnlyDictionary<string, string> resources)
```

#### Properties

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-defaultculture"></a>

##### `DefaultCulture`

```csharp
string DefaultCulture { get; }
```

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-resolvedculture"></a>

##### `ResolvedCulture`

```csharp
string ResolvedCulture { get; }
```

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-resources"></a>

##### `Resources`

```csharp
IReadOnlyDictionary<string, string> Resources { get; }
```

<a id="member-p-cephalon-abstractions-localization-localizedresourcessnapshot-supportedcultures"></a>

##### `SupportedCultures`

```csharp
IReadOnlyList<string> SupportedCultures { get; }
```

<a id="namespace-cephalon-abstractions-modules"></a>

## Namespace Cephalon.Abstractions.Modules

<a id="type-cephalon-abstractions-modules-imodule"></a>

### `IModule`

#### Declaration
```csharp
public interface IModule
```

#### Properties

<a id="member-p-cephalon-abstractions-modules-imodule-descriptor"></a>

##### `Descriptor`

```csharp
ModuleDescriptor Descriptor { get; }
```

#### Methods

<a id="member-m-cephalon-abstractions-modules-imodule-configureservices-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `ConfigureServices`

```csharp
void ConfigureServices(IServiceCollection services)
```

<a id="member-m-cephalon-abstractions-modules-imodule-registercapabilities-cephalon-abstractions-capabilities-icapabilityregistry"></a>

##### `RegisterCapabilities`

```csharp
void RegisterCapabilities(ICapabilityRegistry capabilities)
```

<a id="type-cephalon-abstractions-modules-imodulelifecycle"></a>

### `IModuleLifecycle`

#### Declaration
```csharp
public interface IModuleLifecycle
```

#### Methods

<a id="member-m-cephalon-abstractions-modules-imodulelifecycle-initializeasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `InitializeAsync`

```csharp
Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-abstractions-modules-imodulelifecycle-startasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-abstractions-modules-imodulelifecycle-stopasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
```

<a id="type-cephalon-abstractions-modules-modulebase"></a>

### `ModuleBase`

#### Declaration
```csharp
public abstract class ModuleBase
```

#### Properties

<a id="member-p-cephalon-abstractions-modules-modulebase-descriptor"></a>

##### `Descriptor`

```csharp
ModuleDescriptor Descriptor { get; }
```

#### Methods

<a id="member-m-cephalon-abstractions-modules-modulebase-configureservices-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `ConfigureServices`

```csharp
void ConfigureServices(IServiceCollection services)
```

<a id="member-m-cephalon-abstractions-modules-modulebase-initializeasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `InitializeAsync`

```csharp
Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-abstractions-modules-modulebase-registercapabilities-cephalon-abstractions-capabilities-icapabilityregistry"></a>

##### `RegisterCapabilities`

```csharp
void RegisterCapabilities(ICapabilityRegistry capabilities)
```

<a id="member-m-cephalon-abstractions-modules-modulebase-startasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
```

<a id="member-m-cephalon-abstractions-modules-modulebase-stopasync-cephalon-abstractions-modules-modulecontext-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
```

<a id="type-cephalon-abstractions-modules-modulecontext"></a>

### `ModuleContext`

#### Declaration
```csharp
public sealed class ModuleContext
```

#### Constructors

<a id="member-m-cephalon-abstractions-modules-modulecontext-ctor-system-iserviceprovider"></a>

##### `ModuleContext`

```csharp
ModuleContext(IServiceProvider services)
```

#### Properties

<a id="member-p-cephalon-abstractions-modules-modulecontext-services"></a>

##### `Services`

```csharp
IServiceProvider Services { get; }
```

<a id="type-cephalon-abstractions-modules-moduledescriptor"></a>

### `ModuleDescriptor`

#### Declaration
```csharp
public sealed class ModuleDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-modules-moduledescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ienumerable-1-system-type-system-collections-generic-ienumerable-1-system-string-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `ModuleDescriptor`

```csharp
ModuleDescriptor(string id, string displayName, string description, IEnumerable<Type> dependsOn, IEnumerable<string> tags, string version, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-dependson"></a>

##### `DependsOn`

```csharp
IReadOnlyList<Type> DependsOn { get; }
```

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

<a id="member-p-cephalon-abstractions-modules-moduledescriptor-version"></a>

##### `Version`

```csharp
string Version { get; }
```

<a id="namespace-cephalon-abstractions-patterns"></a>

## Namespace Cephalon.Abstractions.Patterns

<a id="type-cephalon-abstractions-patterns-patterndescriptor"></a>

### `PatternDescriptor`

#### Declaration
```csharp
public sealed class PatternDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-patterns-patterndescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-patterns-patternkind-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `PatternDescriptor`

```csharp
PatternDescriptor(string id, string displayName, string description, PatternKind kind, IReadOnlyList<string> tags, IReadOnlyList<string> requires, IReadOnlyList<string> conflictsWith, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-conflictswith"></a>

##### `ConflictsWith`

```csharp
IReadOnlyList<string> ConflictsWith { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-kind"></a>

##### `Kind`

```csharp
PatternKind Kind { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-requires"></a>

##### `Requires`

```csharp
IReadOnlyList<string> Requires { get; }
```

<a id="member-p-cephalon-abstractions-patterns-patterndescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

<a id="type-cephalon-abstractions-patterns-patternkind"></a>

### `PatternKind`

#### Declaration
```csharp
public enum PatternKind
```

#### Fields

<a id="member-f-cephalon-abstractions-patterns-patternkind-composition"></a>

##### `Composition`

```csharp
const PatternKind Composition
```

<a id="member-f-cephalon-abstractions-patterns-patternkind-deployment"></a>

##### `Deployment`

```csharp
const PatternKind Deployment
```

<a id="member-f-cephalon-abstractions-patterns-patternkind-design"></a>

##### `Design`

```csharp
const PatternKind Design
```

<a id="member-f-cephalon-abstractions-patterns-patternkind-foundation"></a>

##### `Foundation`

```csharp
const PatternKind Foundation
```

<a id="member-f-cephalon-abstractions-patterns-patternkind-organization"></a>

##### `Organization`

```csharp
const PatternKind Organization
```

<a id="namespace-cephalon-abstractions-technologies"></a>

## Namespace Cephalon.Abstractions.Technologies

<a id="type-cephalon-abstractions-technologies-itechnologycapabilitycontributor"></a>

### `ITechnologyCapabilityContributor`

#### Declaration
```csharp
public interface ITechnologyCapabilityContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologycapabilitycontributor-registertechnologycapabilities-cephalon-abstractions-capabilities-icapabilityregistry-cephalon-abstractions-technologies-technologyselection"></a>

##### `RegisterTechnologyCapabilities`

```csharp
void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
```

<a id="type-cephalon-abstractions-technologies-itechnologycontributor"></a>

### `ITechnologyContributor`

#### Declaration
```csharp
public interface ITechnologyContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologycontributor-registertechnologies-cephalon-abstractions-technologies-itechnologyregistry"></a>

##### `RegisterTechnologies`

```csharp
void RegisterTechnologies(ITechnologyRegistry technologies)
```

<a id="type-cephalon-abstractions-technologies-itechnologyregistry"></a>

### `ITechnologyRegistry`

#### Declaration
```csharp
public interface ITechnologyRegistry
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyregistry-add-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `Add`

```csharp
void Add(TechnologyDescriptor technology)
```

<a id="type-cephalon-abstractions-technologies-itechnologyruntimecatalog"></a>

### `ITechnologyRuntimeCatalog`

Exposes the merged runtime surfaces projected by active technology packs.

#### Declaration
```csharp
public interface ITechnologyRuntimeCatalog
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-itechnologyruntimecatalog-surfaces"></a>

##### `Surfaces`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> Surfaces { get; }
```

Gets all active technology runtime surfaces visible to the current runtime.

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyruntimecatalog-getbytechnology-system-string"></a>

##### `GetByTechnology`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId)
```

Gets the runtime surfaces associated with a specific technology identifier.

Returns: The matching runtime surfaces, or an empty collection when none are active.

Parameters:
- `technologyId`: The technology identifier to filter by.

<a id="type-cephalon-abstractions-technologies-itechnologyruntimecontributor"></a>

### `ITechnologyRuntimeContributor`

#### Declaration
```csharp
public interface ITechnologyRuntimeContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyruntimecontributor-describeruntimesurface"></a>

##### `DescribeRuntimeSurface`

```csharp
TechnologyRuntimeSurface DescribeRuntimeSurface()
```

<a id="type-cephalon-abstractions-technologies-itechnologyservicecontributor"></a>

### `ITechnologyServiceContributor`

#### Declaration
```csharp
public interface ITechnologyServiceContributor
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-itechnologyservicecontributor-configuretechnologyservices-microsoft-extensions-dependencyinjection-iservicecollection-cephalon-abstractions-technologies-technologyselection"></a>

##### `ConfigureTechnologyServices`

```csharp
void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
```

<a id="type-cephalon-abstractions-technologies-technologydescriptor"></a>

### `TechnologyDescriptor`

#### Declaration
```csharp
public sealed class TechnologyDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologydescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-technologies-technologykind-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `TechnologyDescriptor`

```csharp
TechnologyDescriptor(string id, string displayName, string description, TechnologyKind kind, IReadOnlyList<string> tags, IReadOnlyList<string> requiresPatterns, IReadOnlyList<string> requiresTransports, IReadOnlyList<string> requiresTechnologies, IReadOnlyList<string> conflictsWith, IReadOnlyList<string> packageHints, IReadOnlyList<string> guidance, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-conflictswith"></a>

##### `ConflictsWith`

```csharp
IReadOnlyList<string> ConflictsWith { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-guidance"></a>

##### `Guidance`

```csharp
IReadOnlyList<string> Guidance { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-kind"></a>

##### `Kind`

```csharp
TechnologyKind Kind { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-packagehints"></a>

##### `PackageHints`

```csharp
IReadOnlyList<string> PackageHints { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-requirespatterns"></a>

##### `RequiresPatterns`

```csharp
IReadOnlyList<string> RequiresPatterns { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-requirestechnologies"></a>

##### `RequiresTechnologies`

```csharp
IReadOnlyList<string> RequiresTechnologies { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-requirestransports"></a>

##### `RequiresTransports`

```csharp
IReadOnlyList<string> RequiresTransports { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologydescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

<a id="type-cephalon-abstractions-technologies-technologykind"></a>

### `TechnologyKind`

#### Declaration
```csharp
public enum TechnologyKind
```

#### Fields

<a id="member-f-cephalon-abstractions-technologies-technologykind-data"></a>

##### `Data`

```csharp
const TechnologyKind Data
```

<a id="member-f-cephalon-abstractions-technologies-technologykind-deployment"></a>

##### `Deployment`

```csharp
const TechnologyKind Deployment
```

<a id="member-f-cephalon-abstractions-technologies-technologykind-experience"></a>

##### `Experience`

```csharp
const TechnologyKind Experience
```

<a id="member-f-cephalon-abstractions-technologies-technologykind-intelligence"></a>

##### `Intelligence`

```csharp
const TechnologyKind Intelligence
```

<a id="member-f-cephalon-abstractions-technologies-technologykind-messaging"></a>

##### `Messaging`

```csharp
const TechnologyKind Messaging
```

<a id="type-cephalon-abstractions-technologies-technologyruntimeentry"></a>

### `TechnologyRuntimeEntry`

Describes one runtime-visible entry inside a technology surface.

#### Declaration
```csharp
public sealed class TechnologyRuntimeEntry
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologyruntimeentry-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `TechnologyRuntimeEntry`

```csharp
TechnologyRuntimeEntry(string id, string displayName, string description, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the entry.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the entry.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable identifier for the entry.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimeentry-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional metadata projected for the entry.

<a id="type-cephalon-abstractions-technologies-technologyruntimesurface"></a>

### `TechnologyRuntimeSurface`

Describes one operator-facing runtime surface exposed by an active technology pack.

#### Declaration
```csharp
public sealed class TechnologyRuntimeSurface
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologyruntimesurface-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-cephalon-abstractions-technologies-technologyruntimeentry"></a>

##### `TechnologyRuntimeSurface`

```csharp
TechnologyRuntimeSurface(string technologyId, string surfaceId, string displayName, string description, IReadOnlyList<TechnologyRuntimeEntry> entries)
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the surface.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the surface.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-entries"></a>

##### `Entries`

```csharp
IReadOnlyList<TechnologyRuntimeEntry> Entries { get; }
```

Gets the entries currently projected by this surface.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-surfaceid"></a>

##### `SurfaceId`

```csharp
string SurfaceId { get; }
```

Gets the stable identifier of this surface within the owning technology.

<a id="member-p-cephalon-abstractions-technologies-technologyruntimesurface-technologyid"></a>

##### `TechnologyId`

```csharp
string TechnologyId { get; }
```

Gets the identifier of the technology profile that owns this surface.

<a id="type-cephalon-abstractions-technologies-technologyselection"></a>

### `TechnologySelection`

#### Declaration
```csharp
public sealed class TechnologySelection
```

#### Constructors

<a id="member-m-cephalon-abstractions-technologies-technologyselection-ctor-system-collections-generic-ireadonlylist-1-cephalon-abstractions-technologies-technologydescriptor-system-collections-generic-ireadonlylist-1-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TechnologySelection`

```csharp
TechnologySelection(IReadOnlyList<TechnologyDescriptor> selected, IReadOnlyList<TechnologyDescriptor> catalog)
```

#### Properties

<a id="member-p-cephalon-abstractions-technologies-technologyselection-catalog"></a>

##### `Catalog`

```csharp
IReadOnlyList<TechnologyDescriptor> Catalog { get; }
```

<a id="member-p-cephalon-abstractions-technologies-technologyselection-selected"></a>

##### `Selected`

```csharp
IReadOnlyList<TechnologyDescriptor> Selected { get; }
```

#### Methods

<a id="member-m-cephalon-abstractions-technologies-technologyselection-isavailable-system-string"></a>

##### `IsAvailable`

```csharp
bool IsAvailable(string value)
```

<a id="member-m-cephalon-abstractions-technologies-technologyselection-isselected-system-string"></a>

##### `IsSelected`

```csharp
bool IsSelected(string value)
```

<a id="member-m-cephalon-abstractions-technologies-technologyselection-trygetavailable-system-string-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TryGetAvailable`

```csharp
bool TryGetAvailable(string value, out TechnologyDescriptor technology)
```

<a id="member-m-cephalon-abstractions-technologies-technologyselection-trygetselected-system-string-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TryGetSelected`

```csharp
bool TryGetSelected(string value, out TechnologyDescriptor technology)
```

<a id="namespace-cephalon-abstractions-transports"></a>

## Namespace Cephalon.Abstractions.Transports

<a id="type-cephalon-abstractions-transports-transportdescriptor"></a>

### `TransportDescriptor`

#### Declaration
```csharp
public sealed class TransportDescriptor
```

#### Constructors

<a id="member-m-cephalon-abstractions-transports-transportdescriptor-ctor-system-string-system-string-system-string-cephalon-abstractions-transports-transportfeatures-system-collections-generic-ireadonlylist-1-system-string-system-collections-generic-ireadonlydictionary-2-system-string-system-string"></a>

##### `TransportDescriptor`

```csharp
TransportDescriptor(string id, string displayName, string description, TransportFeatures features, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata)
```

#### Properties

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-features"></a>

##### `Features`

```csharp
TransportFeatures Features { get; }
```

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

<a id="member-p-cephalon-abstractions-transports-transportdescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

<a id="type-cephalon-abstractions-transports-transportfeatures"></a>

### `TransportFeatures`

#### Declaration
```csharp
public enum TransportFeatures
```

#### Fields

<a id="member-f-cephalon-abstractions-transports-transportfeatures-clientstreaming"></a>

##### `ClientStreaming`

```csharp
const TransportFeatures ClientStreaming
```

<a id="member-f-cephalon-abstractions-transports-transportfeatures-duplexstreaming"></a>

##### `DuplexStreaming`

```csharp
const TransportFeatures DuplexStreaming
```

<a id="member-f-cephalon-abstractions-transports-transportfeatures-none"></a>

##### `None`

```csharp
const TransportFeatures None
```

<a id="member-f-cephalon-abstractions-transports-transportfeatures-requestresponse"></a>

##### `RequestResponse`

```csharp
const TransportFeatures RequestResponse
```

<a id="member-f-cephalon-abstractions-transports-transportfeatures-serverstreaming"></a>

##### `ServerStreaming`

```csharp
const TransportFeatures ServerStreaming
```
