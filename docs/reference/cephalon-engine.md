# Cephalon.Engine

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Engine)
## Namespaces

- `Cephalon.Engine.AppModel`
- `Cephalon.Engine.AppModel.Scaffolding`
- `Cephalon.Engine.Composition`
- `Cephalon.Engine.Configuration`
- `Cephalon.Engine.Diagnostics`
- `Cephalon.Engine.Localization`
- `Cephalon.Engine.Manifest`
- `Cephalon.Engine.Patterns`
- `Cephalon.Engine.Runtime`
- `Cephalon.Engine.Technologies`
- `Cephalon.Engine.Transports`
- `Cephalon.Engine.Trust`

<a id="namespace-cephalon-engine-appmodel"></a>

## Namespace Cephalon.Engine.AppModel

<a id="type-cephalon-engine-appmodel-appprofilefactory"></a>

### `AppProfileFactory`

Builds app profiles from `EngineSettings` values.

#### Declaration
```csharp
public static class AppProfileFactory
```

#### Methods

<a id="member-m-cephalon-engine-appmodel-appprofilefactory-create-cephalon-engine-configuration-enginesettings"></a>

##### `Create`

```csharp
AppProfile Create(EngineSettings settings)
```

Creates an app profile from the configured engine settings.

Returns: The built app profile.

Parameters:
- `settings`: The engine settings that describe the blueprint, patterns, transports, and technologies.

<a id="type-cephalon-engine-appmodel-builtinblueprints"></a>

### `BuiltInBlueprints`

Provides the built-in Cephalon app blueprints.

#### Declaration
```csharp
public static class BuiltInBlueprints
```

#### Properties

<a id="member-p-cephalon-engine-appmodel-builtinblueprints-all"></a>

##### `All`

```csharp
IReadOnlyList<AppBlueprint> All { get; }
```

Gets all built-in blueprints.

<a id="member-p-cephalon-engine-appmodel-builtinblueprints-microservice"></a>

##### `Microservice`

```csharp
AppBlueprint Microservice { get; }
```

Gets the built-in microservice blueprint.

<a id="member-p-cephalon-engine-appmodel-builtinblueprints-modularmonolith"></a>

##### `ModularMonolith`

```csharp
AppBlueprint ModularMonolith { get; }
```

Gets the built-in modular-monolith blueprint.

<a id="member-p-cephalon-engine-appmodel-builtinblueprints-modularverticalslice"></a>

##### `ModularVerticalSlice`

```csharp
AppBlueprint ModularVerticalSlice { get; }
```

Gets the built-in modular-vertical-slice blueprint.

#### Methods

<a id="member-m-cephalon-engine-appmodel-builtinblueprints-resolve-system-string"></a>

##### `Resolve`

```csharp
AppBlueprint Resolve(string value)
```

Resolves a blueprint identifier, display name, or alias.

Returns: The resolved blueprint.

Parameters:
- `value`: The blueprint identifier, display name, or alias to resolve.

<a id="member-m-cephalon-engine-appmodel-builtinblueprints-tryresolve-system-string-cephalon-abstractions-appmodel-appblueprint"></a>

##### `TryResolve`

```csharp
bool TryResolve(string value, out AppBlueprint blueprint)
```

Attempts to resolve a blueprint identifier, display name, or alias.

Returns: `true` when the blueprint was resolved; otherwise, `false`.

Parameters:
- `value`: The blueprint identifier, display name, or alias to resolve.
- `blueprint`: The resolved blueprint when the lookup succeeds.

<a id="namespace-cephalon-engine-appmodel-scaffolding"></a>

## Namespace Cephalon.Engine.AppModel.Scaffolding

<a id="type-cephalon-engine-appmodel-scaffolding-builtinscaffolds"></a>

### `BuiltInScaffolds`

Provides the built-in scaffold plans that back the shipped Cephalon blueprints.

#### Declaration
```csharp
public static class BuiltInScaffolds
```

#### Properties

<a id="member-p-cephalon-engine-appmodel-scaffolding-builtinscaffolds-microservice"></a>

##### `Microservice`

```csharp
ScaffoldPlan Microservice { get; }
```

Gets the scaffold plan for the microservice blueprint.

<a id="member-p-cephalon-engine-appmodel-scaffolding-builtinscaffolds-modularmonolith"></a>

##### `ModularMonolith`

```csharp
ScaffoldPlan ModularMonolith { get; }
```

Gets the scaffold plan for the modular-monolith blueprint.

<a id="member-p-cephalon-engine-appmodel-scaffolding-builtinscaffolds-modularverticalslice"></a>

##### `ModularVerticalSlice`

```csharp
ScaffoldPlan ModularVerticalSlice { get; }
```

Gets the scaffold plan for the modular-vertical-slice blueprint.

<a id="namespace-cephalon-engine-composition"></a>

## Namespace Cephalon.Engine.Composition

<a id="type-cephalon-engine-composition-enginebuilder"></a>

### `EngineBuilder`

Builds a Cephalon runtime by composing the application model, module set, and policy inputs into a single `EngineRuntime`.

Remarks: `EngineBuilder` is the main code-first entry point for configuring Cephalon. It can be driven from configuration, composed in code, or use both approaches together.

At build time the builder resolves discovery inputs, package loading, application profile selection, localization, trust policy, failure policy, and capability filtering into a deterministic runtime snapshot.

#### Declaration
```csharp
public sealed class EngineBuilder
```

#### Constructors

<a id="member-m-cephalon-engine-composition-enginebuilder-ctor-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `EngineBuilder`

```csharp
EngineBuilder(IServiceCollection services)
```

Creates a new builder over the supplied service collection.

Parameters:
- `services`: The service collection that receives runtime services, catalogs, policies, and module- or technology-provided registrations.

#### Properties

<a id="member-p-cephalon-engine-composition-enginebuilder-services"></a>

##### `Services`

```csharp
IServiceCollection Services { get; }
```

Gets the service collection that the builder mutates while composing the engine.

#### Methods

<a id="member-m-cephalon-engine-composition-enginebuilder-addlanguageresources-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `AddLanguageResources`

```csharp
EngineBuilder AddLanguageResources(string culture, IReadOnlyDictionary<string, string> resources)
```

Adds resource overrides for a specific culture without replacing the existing localization contract.

Remarks: This method is typically used by hosts that want to layer project-owned localization on top of engine defaults and any package-provided language packs.

Returns: The same builder instance.

Parameters:
- `culture`: The culture name to extend, such as `en` or `th-TH`.
- `resources`: The key/value resource set to merge for that culture.

<a id="member-m-cephalon-engine-composition-enginebuilder-addmodule-1"></a>

##### `AddModule`

```csharp
EngineBuilder AddModule<TModule>()
```

Adds a module by type using its parameterless constructor.

Returns: The same builder instance.

Type parameters:
- `TModule`: The concrete module type to instantiate and register.

<a id="member-m-cephalon-engine-composition-enginebuilder-addmodule-cephalon-abstractions-modules-imodule"></a>

##### `AddModule`

```csharp
EngineBuilder AddModule(IModule module)
```

Adds a concrete module instance to the runtime composition graph.

Returns: The same builder instance.

Parameters:
- `module`: The module instance to register.

<a id="member-m-cephalon-engine-composition-enginebuilder-addmodulesfromassemblies-system-collections-generic-ienumerable-system-reflection-assembly-system-func-system-type-system-boolean"></a>

##### `AddModulesFromAssemblies`

```csharp
EngineBuilder AddModulesFromAssemblies(IEnumerable<Assembly> assemblies, Func<Type, bool> filter)
```

Discovers and adds modules from a sequence of assemblies.

Returns: The same builder instance.

Parameters:
- `assemblies`: The assemblies to scan for modules.
- `filter`: An optional predicate that can opt specific candidate types in or out before they are instantiated.

<a id="member-m-cephalon-engine-composition-enginebuilder-addmodulesfromassembly-system-reflection-assembly-system-func-system-type-system-boolean"></a>

##### `AddModulesFromAssembly`

```csharp
EngineBuilder AddModulesFromAssembly(Assembly assembly, Func<Type, bool> filter)
```

Discovers and adds modules from an assembly.

Returns: The same builder instance.

Parameters:
- `assembly`: The assembly to scan.
- `filter`: An optional predicate that can opt specific candidate types in or out before they are instantiated.

<a id="member-m-cephalon-engine-composition-enginebuilder-addmodulesfromassemblycontaining-1-system-func-system-type-system-boolean"></a>

##### `AddModulesFromAssemblyContaining`

```csharp
EngineBuilder AddModulesFromAssemblyContaining<TMarker>(Func<Type, bool> filter)
```

Discovers and adds modules from the assembly that contains `TMarker`.

Returns: The same builder instance.

Type parameters:
- `TMarker`: A type used only to identify the source assembly.

Parameters:
- `filter`: An optional predicate that can opt specific candidate types in or out before they are instantiated.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackage-cephalon-engine-configuration-modulepackagereference"></a>

##### `AddPackage`

```csharp
EngineBuilder AddPackage(ModulePackageReference package)
```

Adds an explicit package reference to the builder.

Returns: The same builder instance.

Parameters:
- `package`: The package reference to register.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackageassembly-system-string-system-string"></a>

##### `AddPackageAssembly`

```csharp
EngineBuilder AddPackageAssembly(string path, string id)
```

Adds a package reference that points directly to a module assembly.

Returns: The same builder instance.

Parameters:
- `path`: The path to the package assembly.
- `id`: An optional stable package identifier. When omitted, the identifier is derived from the reference.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackagedirectories-system-collections-generic-ienumerable-cephalon-engine-configuration-modulepackagedirectory"></a>

##### `AddPackageDirectories`

```csharp
EngineBuilder AddPackageDirectories(IEnumerable<ModulePackageDirectory> directories)
```

Adds multiple package-directory discovery rules.

Returns: The same builder instance.

Parameters:
- `directories`: The package directories to register.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackagedirectory-cephalon-engine-configuration-modulepackagedirectory"></a>

##### `AddPackageDirectory`

```csharp
EngineBuilder AddPackageDirectory(ModulePackageDirectory directory)
```

Adds a package-directory discovery rule to the builder.

Returns: The same builder instance.

Parameters:
- `directory`: The directory discovery descriptor to register.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackagedirectory-system-string-system-string-system-boolean"></a>

##### `AddPackageDirectory`

```csharp
EngineBuilder AddPackageDirectory(string path, string manifestFileName, bool includeSubdirectories)
```

Adds a directory that should be scanned for package manifests.

Returns: The same builder instance.

Parameters:
- `path`: The directory path to scan.
- `manifestFileName`: The manifest file name to search for. When omitted, the engine uses the default package manifest name.
- `includeSubdirectories`: `true` to recurse into child directories; otherwise only the top-level directory is scanned.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackagemanifest-system-string-system-string"></a>

##### `AddPackageManifest`

```csharp
EngineBuilder AddPackageManifest(string manifestPath, string id)
```

Adds a package by its `cephalon.package.json` manifest file.

Returns: The same builder instance.

Parameters:
- `manifestPath`: The path to the package manifest file.
- `id`: An optional stable package identifier. When omitted, the identifier is resolved from the manifest.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpackages-system-collections-generic-ienumerable-cephalon-engine-configuration-modulepackagereference"></a>

##### `AddPackages`

```csharp
EngineBuilder AddPackages(IEnumerable<ModulePackageReference> packages)
```

Adds multiple explicit package references.

Returns: The same builder instance.

Parameters:
- `packages`: The package references to register.

<a id="member-m-cephalon-engine-composition-enginebuilder-addpattern-cephalon-abstractions-patterns-patterndescriptor"></a>

##### `AddPattern`

```csharp
EngineBuilder AddPattern(PatternDescriptor pattern)
```

Adds an application or design pattern to the current app profile.

Returns: The same builder instance.

Parameters:
- `pattern`: The pattern descriptor to add.

<a id="member-m-cephalon-engine-composition-enginebuilder-addtechnology-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `AddTechnology`

```csharp
EngineBuilder AddTechnology(TechnologyDescriptor technology)
```

Selects a technology profile for the current app profile.

Returns: The same builder instance.

Parameters:
- `technology`: The technology descriptor to activate.

<a id="member-m-cephalon-engine-composition-enginebuilder-addtransport-cephalon-abstractions-transports-transportdescriptor"></a>

##### `AddTransport`

```csharp
EngineBuilder AddTransport(TransportDescriptor transport)
```

Adds a transport to the current app profile selection.

Returns: The same builder instance.

Parameters:
- `transport`: The transport descriptor to add.

<a id="member-m-cephalon-engine-composition-enginebuilder-build"></a>

##### `Build`

```csharp
EngineRuntime Build()
```

Materializes the configured engine into a runnable `EngineRuntime`.

Remarks: Build is the point where Cephalon becomes deterministic. The builder validates duplicate modules, resolves package-loaded assemblies, orders modules by dependency, applies capability and trust policy, creates the runtime manifest, and registers the runtime-facing catalogs used by hosts and tooling.

Returns: The fully built runtime.

<a id="member-m-cephalon-engine-composition-enginebuilder-registertechnology-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `RegisterTechnology`

```csharp
EngineBuilder RegisterTechnology(TechnologyDescriptor technology)
```

Registers a technology descriptor in the available catalog without implicitly selecting it.

Remarks: This is useful when a project wants to extend the catalog and let configuration decide whether the technology is active.

Returns: The same builder instance.

Parameters:
- `technology`: The technology descriptor to register.

<a id="member-m-cephalon-engine-composition-enginebuilder-useblueprint-cephalon-abstractions-appmodel-appblueprint"></a>

##### `UseBlueprint`

```csharp
EngineBuilder UseBlueprint(AppBlueprint blueprint)
```

Selects the base application blueprint that should shape the runtime.

Returns: The same builder instance.

Parameters:
- `blueprint`: The blueprint descriptor to activate.

<a id="member-m-cephalon-engine-composition-enginebuilder-useconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `UseConfiguration`

```csharp
EngineBuilder UseConfiguration(IConfiguration configuration, string sectionPath)
```

Reads engine settings from configuration and merges them into the current builder state.

Remarks: This method is the preferred entry point when a host should stay configuration-driven. It binds the selected blueprint, transports, technologies, options, localization, trust policy, failure policy, and discovery settings before any code-level overrides are applied.

Returns: The same builder instance.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The configuration path that should be interpreted as the engine settings section. The default value is `SectionName`.

<a id="member-m-cephalon-engine-composition-enginebuilder-usefailurepolicy-cephalon-engine-configuration-failurepolicy"></a>

##### `UseFailurePolicy`

```csharp
EngineBuilder UseFailurePolicy(FailurePolicy policy)
```

Replaces the failure policy used by the runtime lifecycle state machine.

Returns: The same builder instance.

Parameters:
- `policy`: The lifecycle failure policy to apply.

<a id="member-m-cephalon-engine-composition-enginebuilder-uselocalization-cephalon-engine-configuration-localizationsettings"></a>

##### `UseLocalization`

```csharp
EngineBuilder UseLocalization(LocalizationSettings settings)
```

Merges localization settings into the current builder state.

Returns: The same builder instance.

Parameters:
- `settings`: The localization settings to merge, including default culture, supported cultures, and per-culture resource overrides.

<a id="member-m-cephalon-engine-composition-enginebuilder-useoptions-cephalon-engine-configuration-engineoptions"></a>

##### `UseOptions`

```csharp
EngineBuilder UseOptions(EngineOptions options)
```

Merges engine option overrides such as module enablement and capability toggles.

Returns: The same builder instance.

Parameters:
- `options`: The option overrides to merge.

<a id="member-m-cephalon-engine-composition-enginebuilder-usepackagepolicy-cephalon-engine-configuration-packagepolicy"></a>

##### `UsePackagePolicy`

```csharp
EngineBuilder UsePackagePolicy(PackagePolicy policy)
```

Replaces the package-governance policy used when loading independently shipped module packages.

Returns: The same builder instance.

Parameters:
- `policy`: The package policy to apply.

<a id="member-m-cephalon-engine-composition-enginebuilder-usesettings-cephalon-engine-configuration-enginesettings"></a>

##### `UseSettings`

```csharp
EngineBuilder UseSettings(EngineSettings settings)
```

Applies a preconstructed `EngineSettings` instance to the builder.

Remarks: Discovery settings are resolved eagerly, which means referenced assemblies or package manifests are validated before the runtime is built.

Returns: The same builder instance.

Parameters:
- `settings`: The settings object to merge into the current builder state.

<a id="member-m-cephalon-engine-composition-enginebuilder-usetrustpolicy-cephalon-engine-configuration-trustpolicy"></a>

##### `UseTrustPolicy`

```csharp
EngineBuilder UseTrustPolicy(TrustPolicy policy)
```

Merges trust and capability-governance settings into the builder.

Returns: The same builder instance.

Parameters:
- `policy`: The trust policy to apply.

<a id="type-cephalon-engine-composition-engineservicecollectionextensions"></a>

### `EngineServiceCollectionExtensions`

Adds the Cephalon runtime and its supporting services to an `IServiceCollection`.

#### Declaration
```csharp
public static class EngineServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-engine-composition-engineservicecollectionextensions-addcephalon-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-engine-composition-enginebuilder"></a>

##### `AddCephalon`

```csharp
IServiceCollection AddCephalon(this IServiceCollection services, Action<EngineBuilder> configure)
```

Adds Cephalon using code-first configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: The callback that configures the engine builder.

<a id="member-m-cephalon-engine-composition-engineservicecollectionextensions-addcephalon-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-engine-composition-enginebuilder-system-string"></a>

##### `AddCephalon`

```csharp
IServiceCollection AddCephalon(this IServiceCollection services, IConfiguration configuration, Action<EngineBuilder> configure, string sectionPath)
```

Adds Cephalon using configuration as the primary source of engine settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven engine setup.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-engine-configuration"></a>

## Namespace Cephalon.Engine.Configuration

<a id="type-cephalon-engine-configuration-engineoptions"></a>

### `EngineOptions`

Captures module and capability enablement overrides for the runtime.

#### Declaration
```csharp
public sealed class EngineOptions
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-engineoptions-ctor-system-collections-generic-ireadonlydictionary-system-string-system-boolean-system-collections-generic-ireadonlydictionary-system-string-system-boolean"></a>

##### `EngineOptions`

```csharp
EngineOptions(IReadOnlyDictionary<string, bool> modules, IReadOnlyDictionary<string, bool> capabilities)
```

Initializes a new instance of the `EngineOptions` class.

Parameters:
- `modules`: Module enablement overrides keyed by module identifier.
- `capabilities`: Capability enablement overrides keyed by capability key.

#### Properties

<a id="member-p-cephalon-engine-configuration-engineoptions-capabilities"></a>

##### `Capabilities`

```csharp
IReadOnlyDictionary<string, bool> Capabilities { get; }
```

Gets capability enablement overrides keyed by capability key.

<a id="member-p-cephalon-engine-configuration-engineoptions-empty"></a>

##### `Empty`

```csharp
EngineOptions Empty { get; }
```

Gets an empty options instance with no explicit overrides.

<a id="member-p-cephalon-engine-configuration-engineoptions-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any explicit option overrides are present.

<a id="member-p-cephalon-engine-configuration-engineoptions-modules"></a>

##### `Modules`

```csharp
IReadOnlyDictionary<string, bool> Modules { get; }
```

Gets module enablement overrides keyed by module identifier.

#### Methods

<a id="member-m-cephalon-engine-configuration-engineoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
EngineOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads engine options from configuration.

Returns: The parsed engine options.

Parameters:
- `configuration`: The configuration source that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="member-m-cephalon-engine-configuration-engineoptions-iscapabilityenabled-system-string"></a>

##### `IsCapabilityEnabled`

```csharp
bool IsCapabilityEnabled(string capabilityKey)
```

Determines whether a capability is enabled under the current option set.

Returns: `true` when the capability is enabled; otherwise, `false`.

Parameters:
- `capabilityKey`: The capability key to evaluate.

<a id="member-m-cephalon-engine-configuration-engineoptions-ismoduleenabled-system-string"></a>

##### `IsModuleEnabled`

```csharp
bool IsModuleEnabled(string moduleId)
```

Determines whether a module is enabled under the current option set.

Returns: `true` when the module is enabled; otherwise, `false`.

Parameters:
- `moduleId`: The module identifier to evaluate.

<a id="member-m-cephalon-engine-configuration-engineoptions-merge-cephalon-engine-configuration-engineoptions"></a>

##### `Merge`

```csharp
EngineOptions Merge(EngineOptions other)
```

Merges another option set into the current instance.

Returns: A merged option set.

Parameters:
- `other`: The option set to overlay on top of the current values.

<a id="type-cephalon-engine-configuration-enginesettings"></a>

### `EngineSettings`

Represents the configuration-driven app-model and runtime policy settings for Cephalon.

#### Declaration
```csharp
public sealed class EngineSettings
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-enginesettings-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-cephalon-engine-configuration-engineoptions-cephalon-engine-configuration-modulediscoverysettings-cephalon-engine-configuration-localizationsettings-cephalon-engine-configuration-failurepolicy-cephalon-engine-configuration-trustpolicy-cephalon-engine-configuration-packagepolicy"></a>

##### `EngineSettings`

```csharp
EngineSettings(string blueprint, IReadOnlyList<string> patterns, IReadOnlyList<string> transports, IReadOnlyList<string> technologies, EngineOptions options, ModuleDiscoverySettings discovery, LocalizationSettings localization, FailurePolicy failurePolicy, TrustPolicy trustPolicy, PackagePolicy packagePolicy)
```

Initializes a new instance of the `EngineSettings` class.

Parameters:
- `blueprint`: The selected blueprint identifier, if any.
- `patterns`: The selected pattern identifiers.
- `transports`: The selected transport identifiers.
- `technologies`: The selected technology identifiers.
- `options`: Module and capability option overrides.
- `discovery`: Module discovery inputs.
- `localization`: Localization configuration values.
- `failurePolicy`: Runtime failure policy values.
- `trustPolicy`: Capability and package trust policy values.
- `packagePolicy`: Package metadata and integrity policy values.

#### Fields

<a id="member-f-cephalon-engine-configuration-enginesettings-sectionname"></a>

##### `SectionName`

```csharp
const string SectionName
```

Gets the default root configuration section name for engine settings.

#### Properties

<a id="member-p-cephalon-engine-configuration-enginesettings-blueprint"></a>

##### `Blueprint`

```csharp
string Blueprint { get; }
```

Gets the selected blueprint identifier.

<a id="member-p-cephalon-engine-configuration-enginesettings-discovery"></a>

##### `Discovery`

```csharp
ModuleDiscoverySettings Discovery { get; }
```

Gets module discovery inputs.

<a id="member-p-cephalon-engine-configuration-enginesettings-failurepolicy"></a>

##### `FailurePolicy`

```csharp
FailurePolicy FailurePolicy { get; }
```

Gets runtime failure policy values.

<a id="member-p-cephalon-engine-configuration-enginesettings-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any engine settings were explicitly supplied.

<a id="member-p-cephalon-engine-configuration-enginesettings-localization"></a>

##### `Localization`

```csharp
LocalizationSettings Localization { get; }
```

Gets localization configuration values.

<a id="member-p-cephalon-engine-configuration-enginesettings-options"></a>

##### `Options`

```csharp
EngineOptions Options { get; }
```

Gets module and capability option overrides.

<a id="member-p-cephalon-engine-configuration-enginesettings-packagepolicy"></a>

##### `PackagePolicy`

```csharp
PackagePolicy PackagePolicy { get; }
```

Gets package metadata and integrity policy values.

<a id="member-p-cephalon-engine-configuration-enginesettings-patterns"></a>

##### `Patterns`

```csharp
IReadOnlyList<string> Patterns { get; }
```

Gets the selected pattern identifiers.

<a id="member-p-cephalon-engine-configuration-enginesettings-technologies"></a>

##### `Technologies`

```csharp
IReadOnlyList<string> Technologies { get; }
```

Gets the selected technology identifiers.

<a id="member-p-cephalon-engine-configuration-enginesettings-transports"></a>

##### `Transports`

```csharp
IReadOnlyList<string> Transports { get; }
```

Gets the selected transport identifiers.

<a id="member-p-cephalon-engine-configuration-enginesettings-trustpolicy"></a>

##### `TrustPolicy`

```csharp
TrustPolicy TrustPolicy { get; }
```

Gets capability and package trust policy values.

#### Methods

<a id="member-m-cephalon-engine-configuration-enginesettings-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
EngineSettings FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads engine settings from configuration.

Returns: The parsed engine settings.

Parameters:
- `configuration`: The configuration source that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="type-cephalon-engine-configuration-failurepolicy"></a>

### `FailurePolicy`

Describes how the runtime reacts to startup, stop, and restart failures.

#### Declaration
```csharp
public sealed class FailurePolicy
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-failurepolicy-ctor-cephalon-engine-configuration-startupfailurebehavior-cephalon-engine-configuration-stopfailurebehavior-system-boolean-system-int32-system-timespan-system-timespan-system-timespan"></a>

##### `FailurePolicy`

```csharp
FailurePolicy(StartupFailureBehavior startupFailureBehavior, StopFailureBehavior stopFailureBehavior, bool allowManualRestart, int maxRestartAttempts, TimeSpan startupReadinessDelay, TimeSpan shutdownLivenessGracePeriod, TimeSpan manualRestartBackoff)
```

Initializes a new instance of the `FailurePolicy` class.

Parameters:
- `startupFailureBehavior`: How startup failures are handled.
- `stopFailureBehavior`: How stop failures are handled.
- `allowManualRestart`: Whether operators can manually restart the runtime after supported failures.
- `maxRestartAttempts`: The maximum number of manual restarts, where `-1` allows unlimited restarts.
- `startupReadinessDelay`: The optional warmup window that keeps readiness unhealthy after startup succeeds.
- `shutdownLivenessGracePeriod`: The optional drain window that keeps liveness healthy while shutdown is in progress.
- `manualRestartBackoff`: The optional backoff window that delays manual restarts after a restartable failure.

#### Properties

<a id="member-p-cephalon-engine-configuration-failurepolicy-allowmanualrestart"></a>

##### `AllowManualRestart`

```csharp
bool AllowManualRestart { get; }
```

Gets a value indicating whether manual restart is allowed after supported failures.

<a id="member-p-cephalon-engine-configuration-failurepolicy-default"></a>

##### `Default`

```csharp
FailurePolicy Default { get; }
```

Gets the default failure policy used when no explicit configuration is supplied.

<a id="member-p-cephalon-engine-configuration-failurepolicy-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether this policy differs from `Default`.

<a id="member-p-cephalon-engine-configuration-failurepolicy-manualrestartbackoff"></a>

##### `ManualRestartBackoff`

```csharp
TimeSpan ManualRestartBackoff { get; }
```

Gets the cooldown window that must elapse before a manual restart may run after a restartable failure.

<a id="member-p-cephalon-engine-configuration-failurepolicy-maxrestartattempts"></a>

##### `MaxRestartAttempts`

```csharp
int MaxRestartAttempts { get; }
```

Gets the maximum number of manual restarts.

<a id="member-p-cephalon-engine-configuration-failurepolicy-shutdownlivenessgraceperiod"></a>

##### `ShutdownLivenessGracePeriod`

```csharp
TimeSpan ShutdownLivenessGracePeriod { get; }
```

Gets the liveness grace window that applies while shutdown is still draining.

<a id="member-p-cephalon-engine-configuration-failurepolicy-startupfailurebehavior"></a>

##### `StartupFailureBehavior`

```csharp
StartupFailureBehavior StartupFailureBehavior { get; }
```

Gets how startup failures are handled.

<a id="member-p-cephalon-engine-configuration-failurepolicy-startupreadinessdelay"></a>

##### `StartupReadinessDelay`

```csharp
TimeSpan StartupReadinessDelay { get; }
```

Gets the readiness warmup window that applies after startup succeeds.

<a id="member-p-cephalon-engine-configuration-failurepolicy-stopfailurebehavior"></a>

##### `StopFailureBehavior`

```csharp
StopFailureBehavior StopFailureBehavior { get; }
```

Gets how stop failures are handled.

#### Methods

<a id="member-m-cephalon-engine-configuration-failurepolicy-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
FailurePolicy FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads the failure policy from configuration.

Returns: The parsed failure policy.

Parameters:
- `configuration`: The configuration source that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="type-cephalon-engine-configuration-localizationsettings"></a>

### `LocalizationSettings`

Describes localization configuration for the runtime and module resources.

#### Declaration
```csharp
public sealed class LocalizationSettings
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-localizationsettings-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `LocalizationSettings`

```csharp
LocalizationSettings(string defaultCulture, IReadOnlyList<string> supportedCultures, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> resources)
```

Initializes a new instance of the `LocalizationSettings` class.

Parameters:
- `defaultCulture`: The default culture to use when no explicit culture is requested.
- `supportedCultures`: The supported culture identifiers.
- `resources`: Localized resource entries keyed by culture and resource key.

#### Properties

<a id="member-p-cephalon-engine-configuration-localizationsettings-defaultculture"></a>

##### `DefaultCulture`

```csharp
string DefaultCulture { get; }
```

Gets the default culture to use when no explicit culture is requested.

<a id="member-p-cephalon-engine-configuration-localizationsettings-empty"></a>

##### `Empty`

```csharp
LocalizationSettings Empty { get; }
```

Gets an empty localization configuration instance.

<a id="member-p-cephalon-engine-configuration-localizationsettings-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any localization settings were explicitly supplied.

<a id="member-p-cephalon-engine-configuration-localizationsettings-resources"></a>

##### `Resources`

```csharp
IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Resources { get; }
```

Gets localized resource entries keyed by culture and resource key.

<a id="member-p-cephalon-engine-configuration-localizationsettings-supportedcultures"></a>

##### `SupportedCultures`

```csharp
IReadOnlyList<string> SupportedCultures { get; }
```

Gets the supported culture identifiers.

#### Methods

<a id="member-m-cephalon-engine-configuration-localizationsettings-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
LocalizationSettings FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads localization settings from configuration.

Returns: The parsed localization settings.

Parameters:
- `configuration`: The configuration source that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="member-m-cephalon-engine-configuration-localizationsettings-merge-cephalon-engine-configuration-localizationsettings"></a>

##### `Merge`

```csharp
LocalizationSettings Merge(LocalizationSettings other)
```

Merges another localization settings instance into the current instance.

Returns: A merged localization settings instance.

Parameters:
- `other`: The localization settings to overlay on top of the current values.

<a id="type-cephalon-engine-configuration-modulediscoverysettings"></a>

### `ModuleDiscoverySettings`

Describes how the engine discovers modules from assemblies, package references, and package directories.

#### Declaration
```csharp
public sealed class ModuleDiscoverySettings
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-modulediscoverysettings-ctor-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-cephalon-engine-configuration-modulepackagereference-system-collections-generic-ireadonlylist-cephalon-engine-configuration-modulepackagedirectory"></a>

##### `ModuleDiscoverySettings`

```csharp
ModuleDiscoverySettings(IReadOnlyList<string> assemblies, IReadOnlyList<ModulePackageReference> packages, IReadOnlyList<ModulePackageDirectory> packageDirectories)
```

Initializes a new instance of the `ModuleDiscoverySettings` class.

Parameters:
- `assemblies`: Assembly names or paths to scan for modules.
- `packages`: Explicit package references to load.
- `packageDirectories`: Package directories to scan for manifests.

#### Properties

<a id="member-p-cephalon-engine-configuration-modulediscoverysettings-assemblies"></a>

##### `Assemblies`

```csharp
IReadOnlyList<string> Assemblies { get; }
```

Gets assembly names or paths to scan for modules.

<a id="member-p-cephalon-engine-configuration-modulediscoverysettings-empty"></a>

##### `Empty`

```csharp
ModuleDiscoverySettings Empty { get; }
```

Gets an empty module discovery settings instance.

<a id="member-p-cephalon-engine-configuration-modulediscoverysettings-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any discovery inputs were explicitly supplied.

<a id="member-p-cephalon-engine-configuration-modulediscoverysettings-packagedirectories"></a>

##### `PackageDirectories`

```csharp
IReadOnlyList<ModulePackageDirectory> PackageDirectories { get; }
```

Gets package directories to scan for manifests.

<a id="member-p-cephalon-engine-configuration-modulediscoverysettings-packages"></a>

##### `Packages`

```csharp
IReadOnlyList<ModulePackageReference> Packages { get; }
```

Gets explicit package references to load.

#### Methods

<a id="member-m-cephalon-engine-configuration-modulediscoverysettings-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
ModuleDiscoverySettings FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads module discovery settings from configuration.

Returns: The parsed module discovery settings.

Parameters:
- `configuration`: The configuration source that contains the engine section.
- `sectionPath`: The root configuration section path to read from.

<a id="type-cephalon-engine-configuration-modulepackagedirectory"></a>

### `ModulePackageDirectory`

Describes a directory that should be scanned for Cephalon package manifests.

#### Declaration
```csharp
public sealed class ModulePackageDirectory
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-modulepackagedirectory-ctor-system-string-system-string-system-boolean"></a>

##### `ModulePackageDirectory`

```csharp
ModulePackageDirectory(string path, string manifestFileName, bool includeSubdirectories)
```

Initializes a new instance of the `ModulePackageDirectory` class.

Parameters:
- `path`: The directory path to scan.
- `manifestFileName`: The manifest file name to look for inside the directory.
- `includeSubdirectories`: Whether nested directories should also be scanned.

#### Fields

<a id="member-f-cephalon-engine-configuration-modulepackagedirectory-defaultmanifestfilename"></a>

##### `DefaultManifestFileName`

```csharp
const string DefaultManifestFileName
```

Gets the default manifest file name expected inside package directories.

#### Properties

<a id="member-p-cephalon-engine-configuration-modulepackagedirectory-includesubdirectories"></a>

##### `IncludeSubdirectories`

```csharp
bool IncludeSubdirectories { get; }
```

Gets a value indicating whether nested directories should also be scanned.

<a id="member-p-cephalon-engine-configuration-modulepackagedirectory-manifestfilename"></a>

##### `ManifestFileName`

```csharp
string ManifestFileName { get; }
```

Gets the manifest file name to look for inside the directory.

<a id="member-p-cephalon-engine-configuration-modulepackagedirectory-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the directory path to scan.

<a id="type-cephalon-engine-configuration-modulepackagereference"></a>

### `ModulePackageReference`

Describes a package input that can be loaded into the engine runtime.

#### Declaration
```csharp
public sealed class ModulePackageReference
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-modulepackagereference-ctor-system-string-system-string-system-string"></a>

##### `ModulePackageReference`

```csharp
ModulePackageReference(string path, string id, string kind)
```

Initializes a new instance of the `ModulePackageReference` class.

Parameters:
- `path`: The assembly or manifest path.
- `id`: The optional package identifier override.
- `kind`: The package input kind.

#### Fields

<a id="member-f-cephalon-engine-configuration-modulepackagereference-assemblypathkind"></a>

##### `AssemblyPathKind`

```csharp
const string AssemblyPathKind
```

Identifies a package input that points directly to an assembly path.

<a id="member-f-cephalon-engine-configuration-modulepackagereference-directorymanifestkind"></a>

##### `DirectoryManifestKind`

```csharp
const string DirectoryManifestKind
```

Identifies a package input that was discovered from a directory manifest scan.

<a id="member-f-cephalon-engine-configuration-modulepackagereference-manifestfilekind"></a>

##### `ManifestFileKind`

```csharp
const string ManifestFileKind
```

Identifies a package input that points directly to a manifest file.

#### Properties

<a id="member-p-cephalon-engine-configuration-modulepackagereference-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the optional package identifier override.

<a id="member-p-cephalon-engine-configuration-modulepackagereference-isassemblypath"></a>

##### `IsAssemblyPath`

```csharp
bool IsAssemblyPath { get; }
```

Gets a value indicating whether this reference points directly to an assembly path.

<a id="member-p-cephalon-engine-configuration-modulepackagereference-ismanifestfile"></a>

##### `IsManifestFile`

```csharp
bool IsManifestFile { get; }
```

Gets a value indicating whether this reference points to a manifest-backed package input.

<a id="member-p-cephalon-engine-configuration-modulepackagereference-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the normalized package input kind.

<a id="member-p-cephalon-engine-configuration-modulepackagereference-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the assembly or manifest path.

#### Methods

<a id="member-m-cephalon-engine-configuration-modulepackagereference-frommanifest-system-string-system-string"></a>

##### `FromManifest`

```csharp
ModulePackageReference FromManifest(string manifestPath, string id)
```

Creates a manifest-backed package reference.

Returns: A manifest-backed package reference.

Parameters:
- `manifestPath`: The manifest path to load.
- `id`: The optional package identifier override.

<a id="type-cephalon-engine-configuration-packagepolicy"></a>

### `PackagePolicy`

Defines governance requirements for independently shipped module packages.

Remarks: `PackagePolicy` lets a host decide whether package loading should remain permissive or require stronger metadata such as declared versions, compatibility ranges, publisher provenance, or integrity hashes.

The policy applies to package assembly-path loads, manifest-file loads, and package-directory discovery. When stricter requirements are enabled, packages that do not declare the required metadata fail fast during engine build instead of loading ambiguously at runtime.

#### Declaration
```csharp
public sealed class PackagePolicy
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-packagepolicy-ctor-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean-system-boolean"></a>

##### `PackagePolicy`

```csharp
PackagePolicy(bool allowAssemblyPathPackages, bool requireVersion, bool requireMinimumEngineVersion, bool requireMaximumEngineVersion, bool requireSupportedTargetFrameworks, bool requirePublisherId, bool requireSignatureFingerprint, bool requireSignatureKeyId, bool requireSignatureValue, bool requireSignatureVerification, bool requireIntegritySha256)
```

Creates a package policy.

Parameters:
- `allowAssemblyPathPackages`: `true` to allow raw assembly-path packages; otherwise package loads must come through a manifest-driven flow.
- `requireVersion`: `true` to require a declared package version in `cephalon.package.json`.
- `requireMinimumEngineVersion`: `true` to require `compatibility.minimumEngineVersion`.
- `requireMaximumEngineVersion`: `true` to require `compatibility.maximumEngineVersion`.
- `requireSupportedTargetFrameworks`: `true` to require `compatibility.supportedTargetFrameworks`.
- `requirePublisherId`: `true` to require `publisher.id`.
- `requireSignatureFingerprint`: `true` to require at least one declared signature entry to provide a signer fingerprint.
- `requireSignatureKeyId`: `true` to require at least one declared signature entry to provide a signature key identifier.
- `requireSignatureValue`: `true` to require at least one declared signature entry to provide a detached signature value.
- `requireSignatureVerification`: `true` to require a successful cryptographic signature verification against a trusted public key.
- `requireIntegritySha256`: `true` to require `integrity.sha256`.

#### Properties

<a id="member-p-cephalon-engine-configuration-packagepolicy-allowassemblypathpackages"></a>

##### `AllowAssemblyPathPackages`

```csharp
bool AllowAssemblyPathPackages { get; }
```

Gets a value indicating whether raw assembly-path packages are allowed.

<a id="member-p-cephalon-engine-configuration-packagepolicy-default"></a>

##### `Default`

```csharp
PackagePolicy Default { get; }
```

Gets the default package policy.

<a id="member-p-cephalon-engine-configuration-packagepolicy-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether the policy differs from the default baseline.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requireintegritysha256"></a>

##### `RequireIntegritySha256`

```csharp
bool RequireIntegritySha256 { get; }
```

Gets a value indicating whether package manifests must declare an integrity SHA-256 value.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requiremaximumengineversion"></a>

##### `RequireMaximumEngineVersion`

```csharp
bool RequireMaximumEngineVersion { get; }
```

Gets a value indicating whether package manifests must declare a maximum supported engine version.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requireminimumengineversion"></a>

##### `RequireMinimumEngineVersion`

```csharp
bool RequireMinimumEngineVersion { get; }
```

Gets a value indicating whether package manifests must declare a minimum supported engine version.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requirepublisherid"></a>

##### `RequirePublisherId`

```csharp
bool RequirePublisherId { get; }
```

Gets a value indicating whether package manifests must declare a stable publisher identifier.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requiresignaturefingerprint"></a>

##### `RequireSignatureFingerprint`

```csharp
bool RequireSignatureFingerprint { get; }
```

Gets a value indicating whether package manifests must declare a signer fingerprint on at least one signature entry.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requiresignaturekeyid"></a>

##### `RequireSignatureKeyId`

```csharp
bool RequireSignatureKeyId { get; }
```

Gets a value indicating whether package manifests must declare a signature key identifier on at least one signature entry.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requiresignaturevalue"></a>

##### `RequireSignatureValue`

```csharp
bool RequireSignatureValue { get; }
```

Gets a value indicating whether package manifests must declare a detached signature value on at least one signature entry.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requiresignatureverification"></a>

##### `RequireSignatureVerification`

```csharp
bool RequireSignatureVerification { get; }
```

Gets a value indicating whether package signatures must verify against a trusted public key.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requiresupportedtargetframeworks"></a>

##### `RequireSupportedTargetFrameworks`

```csharp
bool RequireSupportedTargetFrameworks { get; }
```

Gets a value indicating whether package manifests must declare supported target frameworks.

<a id="member-p-cephalon-engine-configuration-packagepolicy-requireversion"></a>

##### `RequireVersion`

```csharp
bool RequireVersion { get; }
```

Gets a value indicating whether package manifests must declare a version.

#### Methods

<a id="member-m-cephalon-engine-configuration-packagepolicy-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
PackagePolicy FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads package policy from configuration.

Returns: The configured package policy, or `Default` when no values are supplied.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The configuration path that should be interpreted as the engine settings section. The default value is `SectionName`.

<a id="type-cephalon-engine-configuration-projectconfigurationbuilderextensions"></a>

### `ProjectConfigurationBuilderExtensions`

Adds Cephalon project-configuration conventions to a configuration builder.

Remarks: Cephalon keeps configuration-driven features friendly to large projects by supporting a split-file convention under a project's `Configurations` folder.

The current convention loads root-level `Add*.json` files first, then loads every `{Environment}.json` file found under the folder tree. This allows teams to keep concerns such as engine settings, OpenAPI settings, or CORS settings in separate folders without forcing everything into one large `appsettings.json` file.

#### Declaration
```csharp
public static class ProjectConfigurationBuilderExtensions
```

#### Fields

<a id="member-f-cephalon-engine-configuration-projectconfigurationbuilderextensions-defaultrootfoldername"></a>

##### `DefaultRootFolderName`

```csharp
const string DefaultRootFolderName
```

Gets the default root folder name used for split project configuration files.

#### Methods

<a id="member-m-cephalon-engine-configuration-projectconfigurationbuilderextensions-addcephalonprojectconfigurations-microsoft-extensions-configuration-iconfigurationbuilder-system-string-system-string-system-string"></a>

##### `AddCephalonProjectConfigurations`

```csharp
IConfigurationBuilder AddCephalonProjectConfigurations(this IConfigurationBuilder configuration, string contentRootPath, string environmentName, string rootFolderName)
```

Adds Cephalon project-configuration conventions to the supplied configuration builder.

Remarks: The convention currently loads: `Configurations/Add*.json``Configurations/**/{Environment}.json` Existing JSON configuration sources are not duplicated when this method is called more than once.

Returns: The same configuration builder for fluent composition.

Parameters:
- `configuration`: The configuration builder to extend.
- `contentRootPath`: The project content root that owns the `Configurations` folder.
- `environmentName`: The current host environment name, such as `Development` or `Local`.
- `rootFolderName`: The split-configuration root folder name. The default value is `DefaultRootFolderName`.

<a id="type-cephalon-engine-configuration-startupfailurebehavior"></a>

### `StartupFailureBehavior`

Describes how startup failures are handled.

#### Declaration
```csharp
public enum StartupFailureBehavior
```

#### Fields

<a id="member-f-cephalon-engine-configuration-startupfailurebehavior-captureonly"></a>

##### `CaptureOnly`

```csharp
const StartupFailureBehavior CaptureOnly
```

Capture the failure in runtime status without rethrowing it to the host.

<a id="member-f-cephalon-engine-configuration-startupfailurebehavior-failfast"></a>

##### `FailFast`

```csharp
const StartupFailureBehavior FailFast
```

Stop startup immediately and rethrow the failure.

<a id="type-cephalon-engine-configuration-stopfailurebehavior"></a>

### `StopFailureBehavior`

Describes how stop failures are handled.

#### Declaration
```csharp
public enum StopFailureBehavior
```

#### Fields

<a id="member-f-cephalon-engine-configuration-stopfailurebehavior-besteffortcontinue"></a>

##### `BestEffortContinue`

```csharp
const StopFailureBehavior BestEffortContinue
```

Continue stopping remaining modules and report failures afterward.

<a id="member-f-cephalon-engine-configuration-stopfailurebehavior-failfast"></a>

##### `FailFast`

```csharp
const StopFailureBehavior FailFast
```

Stop shutdown immediately and rethrow the failure.

<a id="type-cephalon-engine-configuration-trustpolicy"></a>

### `TrustPolicy`

Defines package-trust and capability-governance rules for a Cephalon runtime.

Remarks: `TrustPolicy` is the engine's host-owned trust contract. It decides whether independently shipped packages must be explicitly trusted, how capability access is resolved, and which publishers, signer fingerprints, public keys, signing certificates, or assembly checksums are accepted.

Package-loading decisions use this policy together with package metadata from `cephalon.package.json`, cryptographic signature verification results, and the active package policy. Capability access decisions then flow into runtime introspection and optional HTTP request-time enforcement through the ASP.NET Core host adapters.

#### Declaration
```csharp
public sealed class TrustPolicy
```

#### Constructors

<a id="member-m-cephalon-engine-configuration-trustpolicy-ctor-system-boolean-cephalon-abstractions-capabilities-capabilityaccess-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-cephalon-abstractions-capabilities-capabilityaccess-system-collections-generic-ireadonlydictionary-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `TrustPolicy`

```csharp
TrustPolicy(bool requireTrustedPackages, CapabilityAccess defaultCapabilityAccess, IReadOnlyList<string> trustedPackages, IReadOnlyList<string> trustedAssemblies, IReadOnlyList<string> trustedPublishers, IReadOnlyList<string> trustedSignerFingerprints, IReadOnlyDictionary<string, string> trustedSignaturePublicKeys, IReadOnlyDictionary<string, string> trustedSignatureCertificates, IReadOnlyList<string> trustedSignatureCertificateAuthorities, IReadOnlyDictionary<string, CapabilityAccess> capabilities, IReadOnlyDictionary<string, IReadOnlyList<string>> allowedPackageChecksums)
```

Creates a trust policy.

Parameters:
- `requireTrustedPackages`: `true` to require independently loaded packages to match at least one trust rule; otherwise package loads may proceed without an explicit trust match.
- `defaultCapabilityAccess`: The default access applied when a capability key does not appear in `capabilities`.
- `trustedPackages`: Package identifiers that should be treated as trusted when package-level allow-listing is in use.
- `trustedAssemblies`: Assembly names that should be treated as trusted when assembly-level allow-listing is in use.
- `trustedPublishers`: Stable publisher identifiers that should be treated as trusted for independently shipped packages.
- `trustedSignerFingerprints`: Signer fingerprints that should be treated as trusted for detached-signature provenance checks.
- `trustedSignaturePublicKeys`: Public keys keyed by signing identity or signer fingerprint, used for cryptographic signature verification.
- `trustedSignatureCertificates`: Signing certificates keyed by signing identity or signer fingerprint, used for certificate-backed cryptographic signature verification.
- `trustedSignatureCertificateAuthorities`: Root or intermediate certificate authorities used to validate configured signing certificates when certificate-chain verification is enabled.
- `capabilities`: Explicit per-capability access overrides keyed by capability key.
- `allowedPackageChecksums`: Explicit package checksum allow-lists keyed by package identifier.

#### Properties

<a id="member-p-cephalon-engine-configuration-trustpolicy-allowedpackagechecksums"></a>

##### `AllowedPackageChecksums`

```csharp
IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedPackageChecksums { get; }
```

Gets the package checksum allow-lists keyed by package identifier.

<a id="member-p-cephalon-engine-configuration-trustpolicy-capabilities"></a>

##### `Capabilities`

```csharp
IReadOnlyDictionary<string, CapabilityAccess> Capabilities { get; }
```

Gets the explicit per-capability access rules.

<a id="member-p-cephalon-engine-configuration-trustpolicy-default"></a>

##### `Default`

```csharp
TrustPolicy Default { get; }
```

Gets the default trust policy.

<a id="member-p-cephalon-engine-configuration-trustpolicy-defaultcapabilityaccess"></a>

##### `DefaultCapabilityAccess`

```csharp
CapabilityAccess DefaultCapabilityAccess { get; }
```

Gets the default access applied to capability keys without an explicit override.

<a id="member-p-cephalon-engine-configuration-trustpolicy-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether the policy differs from the default baseline.

<a id="member-p-cephalon-engine-configuration-trustpolicy-requiretrustedpackages"></a>

##### `RequireTrustedPackages`

```csharp
bool RequireTrustedPackages { get; }
```

Gets a value indicating whether explicitly discovered packages must satisfy a trust rule.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedassemblies"></a>

##### `TrustedAssemblies`

```csharp
IReadOnlyList<string> TrustedAssemblies { get; }
```

Gets the trusted assembly-name allow-list.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedpackages"></a>

##### `TrustedPackages`

```csharp
IReadOnlyList<string> TrustedPackages { get; }
```

Gets the trusted package identifier allow-list.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedpublishers"></a>

##### `TrustedPublishers`

```csharp
IReadOnlyList<string> TrustedPublishers { get; }
```

Gets the trusted publisher identifier allow-list.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedsignaturecertificateauthorities"></a>

##### `TrustedSignatureCertificateAuthorities`

```csharp
IReadOnlyList<string> TrustedSignatureCertificateAuthorities { get; }
```

Gets the configured certificate authorities used to validate trusted signing certificate chains.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedsignaturecertificates"></a>

##### `TrustedSignatureCertificates`

```csharp
IReadOnlyDictionary<string, string> TrustedSignatureCertificates { get; }
```

Gets the configured trusted signing certificates used for certificate-backed detached-signature verification.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedsignaturepublickeys"></a>

##### `TrustedSignaturePublicKeys`

```csharp
IReadOnlyDictionary<string, string> TrustedSignaturePublicKeys { get; }
```

Gets the configured trusted public keys used for detached-signature verification.

<a id="member-p-cephalon-engine-configuration-trustpolicy-trustedsignerfingerprints"></a>

##### `TrustedSignerFingerprints`

```csharp
IReadOnlyList<string> TrustedSignerFingerprints { get; }
```

Gets the trusted signer fingerprint allow-list.

#### Methods

<a id="member-m-cephalon-engine-configuration-trustpolicy-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
TrustPolicy FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads a trust policy from configuration.

Returns: The configured trust policy, or `Default` when no values are supplied.

Parameters:
- `configuration`: The root configuration that contains the engine section.
- `sectionPath`: The configuration path that should be interpreted as the engine settings section. The default value is `SectionName`.

<a id="member-m-cephalon-engine-configuration-trustpolicy-merge-cephalon-engine-configuration-trustpolicy"></a>

##### `Merge`

```csharp
TrustPolicy Merge(TrustPolicy other)
```

Merges another trust policy into the current policy.

Returns: A merged trust policy where allow-lists are unioned, keyed rules are overwritten by `other`, and stricter package-trust requirements remain enabled.

Parameters:
- `other`: The policy to merge on top of the current instance.

<a id="member-m-cephalon-engine-configuration-trustpolicy-resolvecapabilityaccess-system-string"></a>

##### `ResolveCapabilityAccess`

```csharp
CapabilityAccess ResolveCapabilityAccess(string capabilityKey)
```

Resolves the effective access for a capability key.

Returns: The explicit access configured for `capabilityKey`, or `DefaultCapabilityAccess` when no override exists.

Parameters:
- `capabilityKey`: The capability key to evaluate.

<a id="namespace-cephalon-engine-diagnostics"></a>

## Namespace Cephalon.Engine.Diagnostics

<a id="type-cephalon-engine-diagnostics-diagnosticeventdefinition"></a>

### `DiagnosticEventDefinition`

Describes one published diagnostics event id together with its intended meaning.

#### Declaration
```csharp
public sealed class DiagnosticEventDefinition
```

#### Constructors

<a id="member-m-cephalon-engine-diagnostics-diagnosticeventdefinition-ctor-system-int32-system-string-cephalon-engine-diagnostics-diagnosticseverity-system-string-system-string"></a>

##### `DiagnosticEventDefinition`

```csharp
DiagnosticEventDefinition(int Id, string Name, DiagnosticSeverity Severity, string MessageTemplate, string Description)
```

Describes one published diagnostics event id together with its intended meaning.

Parameters:
- `Id`: The stable numeric event identifier.
- `Name`: The stable event name paired with the numeric identifier.
- `Severity`: The intended severity for the event.
- `MessageTemplate`: The structured message template emitted by the logger.
- `Description`: The operator-facing explanation of when the event is emitted.

#### Properties

<a id="member-p-cephalon-engine-diagnostics-diagnosticeventdefinition-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

The operator-facing explanation of when the event is emitted.

<a id="member-p-cephalon-engine-diagnostics-diagnosticeventdefinition-id"></a>

##### `Id`

```csharp
int Id { get; set; }
```

The stable numeric event identifier.

<a id="member-p-cephalon-engine-diagnostics-diagnosticeventdefinition-messagetemplate"></a>

##### `MessageTemplate`

```csharp
string MessageTemplate { get; set; }
```

The structured message template emitted by the logger.

<a id="member-p-cephalon-engine-diagnostics-diagnosticeventdefinition-name"></a>

##### `Name`

```csharp
string Name { get; set; }
```

The stable event name paired with the numeric identifier.

<a id="member-p-cephalon-engine-diagnostics-diagnosticeventdefinition-severity"></a>

##### `Severity`

```csharp
DiagnosticSeverity Severity { get; set; }
```

The intended severity for the event.

<a id="type-cephalon-engine-diagnostics-diagnosticsconvention"></a>

### `DiagnosticsConvention`

Describes the diagnostics convention published by one engine package or companion package.

#### Declaration
```csharp
public sealed class DiagnosticsConvention
```

#### Constructors

<a id="member-m-cephalon-engine-diagnostics-diagnosticsconvention-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-engine-diagnostics-diagnosticeventdefinition"></a>

##### `DiagnosticsConvention`

```csharp
DiagnosticsConvention(string Source, string LoggerCategoryPrefix, string Description, IReadOnlyList<DiagnosticEventDefinition> Events)
```

Describes the diagnostics convention published by one engine package or companion package.

Parameters:
- `Source`: The stable package or subsystem identifier that owns the convention.
- `LoggerCategoryPrefix`: The logger category prefix used by the convention.
- `Description`: The operator-facing explanation of what the convention covers.
- `Events`: The published event definitions that belong to the convention.

#### Properties

<a id="member-p-cephalon-engine-diagnostics-diagnosticsconvention-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

The operator-facing explanation of what the convention covers.

<a id="member-p-cephalon-engine-diagnostics-diagnosticsconvention-events"></a>

##### `Events`

```csharp
IReadOnlyList<DiagnosticEventDefinition> Events { get; set; }
```

The published event definitions that belong to the convention.

<a id="member-p-cephalon-engine-diagnostics-diagnosticsconvention-loggercategoryprefix"></a>

##### `LoggerCategoryPrefix`

```csharp
string LoggerCategoryPrefix { get; set; }
```

The logger category prefix used by the convention.

<a id="member-p-cephalon-engine-diagnostics-diagnosticsconvention-maximumeventid"></a>

##### `MaximumEventId`

```csharp
int? MaximumEventId { get; }
```

Gets the largest event id published by this convention.

<a id="member-p-cephalon-engine-diagnostics-diagnosticsconvention-minimumeventid"></a>

##### `MinimumEventId`

```csharp
int? MinimumEventId { get; }
```

Gets the smallest event id published by this convention.

<a id="member-p-cephalon-engine-diagnostics-diagnosticsconvention-source"></a>

##### `Source`

```csharp
string Source { get; set; }
```

The stable package or subsystem identifier that owns the convention.

<a id="type-cephalon-engine-diagnostics-diagnosticseverity"></a>

### `DiagnosticSeverity`

Describes the severity attached to a published diagnostics event definition.

#### Declaration
```csharp
public enum DiagnosticSeverity
```

#### Fields

<a id="member-f-cephalon-engine-diagnostics-diagnosticseverity-critical"></a>

##### `Critical`

```csharp
const DiagnosticSeverity Critical
```

The event indicates a critical condition that usually requires immediate attention.

<a id="member-f-cephalon-engine-diagnostics-diagnosticseverity-debug"></a>

##### `Debug`

```csharp
const DiagnosticSeverity Debug
```

The event is intended for debug-oriented diagnostics.

<a id="member-f-cephalon-engine-diagnostics-diagnosticseverity-error"></a>

##### `Error`

```csharp
const DiagnosticSeverity Error
```

The event indicates an error condition.

<a id="member-f-cephalon-engine-diagnostics-diagnosticseverity-information"></a>

##### `Information`

```csharp
const DiagnosticSeverity Information
```

The event describes expected informational runtime behavior.

<a id="member-f-cephalon-engine-diagnostics-diagnosticseverity-trace"></a>

##### `Trace`

```csharp
const DiagnosticSeverity Trace
```

The event is useful only for highly detailed trace output.

<a id="member-f-cephalon-engine-diagnostics-diagnosticseverity-warning"></a>

##### `Warning`

```csharp
const DiagnosticSeverity Warning
```

The event highlights a warning condition or degraded behavior.

<a id="type-cephalon-engine-diagnostics-enginediagnostics"></a>

### `EngineDiagnostics`

Defines the stable meter, activity source, and counter names emitted by the engine runtime.

#### Declaration
```csharp
public static class EngineDiagnostics
```

#### Fields

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-activitysourcename"></a>

##### `ActivitySourceName`

```csharp
const string ActivitySourceName
```

Gets the activity-source name emitted by the engine.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-buildactivityname"></a>

##### `BuildActivityName`

```csharp
const string BuildActivityName
```

Gets the activity name used while building the runtime.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-enginebuildcountername"></a>

##### `EngineBuildCounterName`

```csharp
const string EngineBuildCounterName
```

Gets the counter name for completed engine builds.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-metername"></a>

##### `MeterName`

```csharp
const string MeterName
```

Gets the meter name emitted by the engine.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-modulefailurecountername"></a>

##### `ModuleFailureCounterName`

```csharp
const string ModuleFailureCounterName
```

Gets the counter name for module lifecycle failures.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-moduletransitioncountername"></a>

##### `ModuleTransitionCounterName`

```csharp
const string ModuleTransitionCounterName
```

Gets the counter name for module lifecycle transitions.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-runtimefailurecountername"></a>

##### `RuntimeFailureCounterName`

```csharp
const string RuntimeFailureCounterName
```

Gets the counter name for runtime lifecycle failures.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-runtimerestartcountername"></a>

##### `RuntimeRestartCounterName`

```csharp
const string RuntimeRestartCounterName
```

Gets the counter name for runtime restart attempts.

<a id="member-f-cephalon-engine-diagnostics-enginediagnostics-runtimetransitioncountername"></a>

##### `RuntimeTransitionCounterName`

```csharp
const string RuntimeTransitionCounterName
```

Gets the counter name for runtime lifecycle transitions.

<a id="type-cephalon-engine-diagnostics-idiagnosticsconventioncontributor"></a>

### `IDiagnosticsConventionContributor`

Contributes a diagnostics convention to the runtime-facing diagnostics catalog.

#### Declaration
```csharp
public interface IDiagnosticsConventionContributor
```

#### Methods

<a id="member-m-cephalon-engine-diagnostics-idiagnosticsconventioncontributor-describediagnosticsconvention"></a>

##### `DescribeDiagnosticsConvention`

```csharp
DiagnosticsConvention DescribeDiagnosticsConvention()
```

Describes the diagnostics convention owned by the contributor.

Returns: The diagnostics convention published by the contributor.

<a id="type-cephalon-engine-diagnostics-iruntimediagnosticscatalog"></a>

### `IRuntimeDiagnosticsCatalog`

Exposes the merged diagnostics conventions visible to the current runtime.

#### Declaration
```csharp
public interface IRuntimeDiagnosticsCatalog
```

#### Properties

<a id="member-p-cephalon-engine-diagnostics-iruntimediagnosticscatalog-conventions"></a>

##### `Conventions`

```csharp
IReadOnlyList<DiagnosticsConvention> Conventions { get; }
```

Gets the published diagnostics conventions visible to the runtime.

#### Methods

<a id="member-m-cephalon-engine-diagnostics-iruntimediagnosticscatalog-getbysource-system-string"></a>

##### `GetBySource`

```csharp
IReadOnlyList<DiagnosticsConvention> GetBySource(string source)
```

Gets the diagnostics conventions owned by the requested source.

Returns: The matching conventions, or an empty list when the source is not active.

Parameters:
- `source`: The diagnostics source or package identifier.

<a id="namespace-cephalon-engine-localization"></a>

## Namespace Cephalon.Engine.Localization

<a id="type-cephalon-engine-localization-localizedtextcatalog"></a>

### `LocalizedTextCatalog`

Resolves localized resources from built-in and configuration-supplied resource catalogs.

#### Declaration
```csharp
public sealed class LocalizedTextCatalog
```

#### Constructors

<a id="member-m-cephalon-engine-localization-localizedtextcatalog-ctor-cephalon-engine-configuration-localizationsettings"></a>

##### `LocalizedTextCatalog`

```csharp
LocalizedTextCatalog(LocalizationSettings settings)
```

Initializes a new instance of the `LocalizedTextCatalog` class.

Parameters:
- `settings`: The localization settings that supply culture and resource overrides.

#### Properties

<a id="member-p-cephalon-engine-localization-localizedtextcatalog-defaultculture"></a>

##### `DefaultCulture`

```csharp
string DefaultCulture { get; }
```

Gets the default culture used when no explicit culture is requested.

<a id="member-p-cephalon-engine-localization-localizedtextcatalog-supportedcultures"></a>

##### `SupportedCultures`

```csharp
IReadOnlyList<string> SupportedCultures { get; }
```

Gets the supported cultures available from the merged resource catalog.

#### Methods

<a id="member-m-cephalon-engine-localization-localizedtextcatalog-createsnapshot-system-string"></a>

##### `CreateSnapshot`

```csharp
LocalizedResourcesSnapshot CreateSnapshot(string culture)
```

Creates a serialization-friendly snapshot of the merged localized resources.

Returns: A snapshot of the resolved localization view.

Parameters:
- `culture`: The preferred culture to resolve from.

<a id="member-m-cephalon-engine-localization-localizedtextcatalog-getresources-system-string"></a>

##### `GetResources`

```csharp
IReadOnlyDictionary<string, string> GetResources(string culture)
```

Gets the merged resources visible for the specified culture.

Returns: The merged resource dictionary for the resolved culture chain.

Parameters:
- `culture`: The preferred culture to resolve from.

<a id="member-m-cephalon-engine-localization-localizedtextcatalog-resolvetext-system-string-system-string-system-string"></a>

##### `ResolveText`

```csharp
string ResolveText(string key, string culture, string fallback)
```

Resolves a localized value or returns the provided fallback.

Returns: The resolved localized value or the fallback.

Parameters:
- `key`: The resource key to resolve.
- `culture`: The preferred culture to resolve from.
- `fallback`: The fallback value to return when the resource cannot be resolved.

<a id="member-m-cephalon-engine-localization-localizedtextcatalog-tryget-system-string-system-string-system-string"></a>

##### `TryGet`

```csharp
bool TryGet(string key, string culture, out string value)
```

Attempts to resolve a localized value for the specified key and culture.

Returns: `true` when a value was resolved; otherwise, `false`.

Parameters:
- `key`: The resource key to resolve.
- `culture`: The preferred culture to resolve from.
- `value`: The resolved localized value when found.

<a id="namespace-cephalon-engine-manifest"></a>

## Namespace Cephalon.Engine.Manifest

<a id="type-cephalon-engine-manifest-capabilitymanifest"></a>

### `CapabilityManifest`

Describes a capability exposed by a module in the runtime manifest.

#### Declaration
```csharp
public sealed class CapabilityManifest
```

#### Constructors

<a id="member-m-cephalon-engine-manifest-capabilitymanifest-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CapabilityManifest`

```csharp
CapabilityManifest(string key, string displayName, string description, string sourceModuleId, IReadOnlyDictionary<string, string> metadata)
```

Initializes a new instance of the `CapabilityManifest` class.

Parameters:
- `key`: The stable capability key.
- `displayName`: The operator-facing capability name.
- `description`: A description of the capability behavior.
- `sourceModuleId`: The identifier of the module that contributed the capability.
- `metadata`: Additional capability metadata.

#### Properties

<a id="member-p-cephalon-engine-manifest-capabilitymanifest-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the capability description.

<a id="member-p-cephalon-engine-manifest-capabilitymanifest-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing capability name.

<a id="member-p-cephalon-engine-manifest-capabilitymanifest-key"></a>

##### `Key`

```csharp
string Key { get; }
```

Gets the stable capability key.

<a id="member-p-cephalon-engine-manifest-capabilitymanifest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional capability metadata.

<a id="member-p-cephalon-engine-manifest-capabilitymanifest-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; }
```

Gets the identifier of the module that contributed the capability.

<a id="type-cephalon-engine-manifest-modulemanifest"></a>

### `ModuleManifest`

Describes a single module that participates in the built runtime.

#### Declaration
```csharp
public sealed class ModuleManifest
```

#### Constructors

<a id="member-m-cephalon-engine-manifest-modulemanifest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-boolean"></a>

##### `ModuleManifest`

```csharp
ModuleManifest(string id, string displayName, string description, string version, string assemblyName, string typeName, IReadOnlyList<string> dependsOn, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> metadata, string packageId, bool isTrusted)
```

Creates a new module manifest entry.

Parameters:
- `id`: The stable module identifier.
- `displayName`: The operator-facing module name.
- `description`: A human-readable description of the module's role.
- `version`: The effective module version.
- `assemblyName`: The assembly that contains the module implementation.
- `typeName`: The fully qualified CLR type name for the module implementation.
- `dependsOn`: The identifiers of modules this module depends on.
- `tags`: The descriptive tags published by the module descriptor.
- `metadata`: Additional descriptor metadata published by the module.
- `packageId`: The package identifier that supplied the module, if it was package-loaded.
- `isTrusted`: Whether the module is currently considered trusted under the active trust policy.

#### Properties

<a id="member-p-cephalon-engine-manifest-modulemanifest-assemblyname"></a>

##### `AssemblyName`

```csharp
string AssemblyName { get; }
```

Gets the assembly name that contains the module implementation.

<a id="member-p-cephalon-engine-manifest-modulemanifest-dependson"></a>

##### `DependsOn`

```csharp
IReadOnlyList<string> DependsOn { get; }
```

Gets the identifiers of modules this module depends on.

<a id="member-p-cephalon-engine-manifest-modulemanifest-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the module.

<a id="member-p-cephalon-engine-manifest-modulemanifest-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the module.

<a id="member-p-cephalon-engine-manifest-modulemanifest-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable module identifier.

<a id="member-p-cephalon-engine-manifest-modulemanifest-istrusted"></a>

##### `IsTrusted`

```csharp
bool IsTrusted { get; }
```

Gets a value indicating whether the module is trusted by the current trust policy.

<a id="member-p-cephalon-engine-manifest-modulemanifest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets arbitrary descriptor metadata published by the module.

<a id="member-p-cephalon-engine-manifest-modulemanifest-packageid"></a>

##### `PackageId`

```csharp
string PackageId { get; }
```

Gets the supplying package identifier when the module came from a package load.

<a id="member-p-cephalon-engine-manifest-modulemanifest-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the descriptor tags published by the module.

<a id="member-p-cephalon-engine-manifest-modulemanifest-typename"></a>

##### `TypeName`

```csharp
string TypeName { get; }
```

Gets the CLR type name that implements the module.

<a id="member-p-cephalon-engine-manifest-modulemanifest-version"></a>

##### `Version`

```csharp
string Version { get; }
```

Gets the effective version reported for the module.

<a id="type-cephalon-engine-manifest-packagedependencymanifest"></a>

### `PackageDependencyManifest`

Describes a package-to-package dependency declared by a loaded Cephalon package.

#### Declaration
```csharp
public sealed class PackageDependencyManifest
```

#### Constructors

<a id="member-m-cephalon-engine-manifest-packagedependencymanifest-ctor-system-string-system-string-system-string"></a>

##### `PackageDependencyManifest`

```csharp
PackageDependencyManifest(string id, string minimumVersion, string maximumVersion)
```

Creates a new package dependency manifest entry.

Parameters:
- `id`: The stable identifier of the required package.
- `minimumVersion`: The minimum acceptable version of the required package, when declared.
- `maximumVersion`: The maximum acceptable version of the required package, when declared.

#### Properties

<a id="member-p-cephalon-engine-manifest-packagedependencymanifest-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable identifier of the required package.

<a id="member-p-cephalon-engine-manifest-packagedependencymanifest-maximumversion"></a>

##### `MaximumVersion`

```csharp
string MaximumVersion { get; }
```

Gets the maximum acceptable version of the required package, when declared.

<a id="member-p-cephalon-engine-manifest-packagedependencymanifest-minimumversion"></a>

##### `MinimumVersion`

```csharp
string MinimumVersion { get; }
```

Gets the minimum acceptable version of the required package, when declared.

<a id="type-cephalon-engine-manifest-packagemanifest"></a>

### `PackageManifest`

Describes a package that contributed one or more modules to the built runtime.

#### Declaration
```csharp
public sealed class PackageManifest
```

#### Constructors

<a id="member-m-cephalon-engine-manifest-packagemanifest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-engine-manifest-packagesignaturemanifest-system-boolean-system-string-system-string-system-boolean-system-string"></a>

##### `PackageManifest`

```csharp
PackageManifest(string id, string kind, string assemblyName, string path, string sourcePath, string loadContext, IReadOnlyList<string> modules, string version, string minimumEngineVersion, string maximumEngineVersion, IReadOnlyList<string> supportedTargetFrameworks, string publisherId, string publisherDisplayName, string publisherWebsite, string signatureType, string signatureSigner, string signatureKeyId, string signatureFingerprint, string signatureCertificateThumbprint, string signatureAlgorithm, IReadOnlyList<PackageSignatureManifest> signatures, bool isSignatureVerified, string signatureVerificationReason, string checksumSha256, bool isTrusted, string trustReason)
```

Creates a new package manifest entry.

Parameters:
- `id`: The stable package identifier.
- `kind`: The discovery kind used to resolve the package.
- `assemblyName`: The loaded package assembly name.
- `path`: The resolved assembly path that was loaded.
- `sourcePath`: The original source path that led to the package load.
- `loadContext`: The assembly load context name used for the package.
- `modules`: The identifiers of modules contributed by the package.
- `version`: The package version declared by the package manifest, when available.
- `minimumEngineVersion`: The minimum supported engine version declared by the package manifest, when available.
- `maximumEngineVersion`: The maximum supported engine version declared by the package manifest, when available.
- `supportedTargetFrameworks`: The supported target frameworks declared by the package manifest.
- `publisherId`: The stable publisher identifier declared by the package manifest, when available.
- `publisherDisplayName`: The publisher display name declared by the package manifest, when available.
- `publisherWebsite`: The publisher website declared by the package manifest, when available.
- `signatureType`: The signature metadata type declared by the package manifest, when available.
- `signatureSigner`: The signer identity declared by the package manifest, when available.
- `signatureKeyId`: The trusted-key identifier declared by the package manifest, when available.
- `signatureFingerprint`: The signer fingerprint declared by the package manifest, when available.
- `signatureCertificateThumbprint`: The primary signing certificate thumbprint used during verification, when certificate-backed trust was used.
- `signatureAlgorithm`: The signature algorithm declared by the package manifest, when available.
- `signatures`: The declared package signatures and their individual verification outcomes.
- `isSignatureVerified`: Whether the package signature was cryptographically verified against a trusted public key or signing certificate.
- `signatureVerificationReason`: The verification outcome summary for the package signature.
- `checksumSha256`: The computed SHA-256 checksum of the resolved package assembly.
- `isTrusted`: Whether the package is trusted by the current trust policy.
- `trustReason`: The reason the package is trusted or not trusted.

#### Properties

<a id="member-p-cephalon-engine-manifest-packagemanifest-assemblyname"></a>

##### `AssemblyName`

```csharp
string AssemblyName { get; }
```

Gets the assembly name that was loaded for the package.

<a id="member-p-cephalon-engine-manifest-packagemanifest-checksumsha256"></a>

##### `ChecksumSha256`

```csharp
string ChecksumSha256 { get; }
```

Gets the computed SHA-256 checksum of the resolved package assembly.

<a id="member-p-cephalon-engine-manifest-packagemanifest-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<PackageDependencyManifest> Dependencies { get; set; }
```

Gets the package-to-package dependencies declared by the package manifest.

<a id="member-p-cephalon-engine-manifest-packagemanifest-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable package identifier.

<a id="member-p-cephalon-engine-manifest-packagemanifest-issignatureverified"></a>

##### `IsSignatureVerified`

```csharp
bool IsSignatureVerified { get; }
```

Gets a value indicating whether the package signature was cryptographically verified against a trusted signing identity.

<a id="member-p-cephalon-engine-manifest-packagemanifest-istrusted"></a>

##### `IsTrusted`

```csharp
bool IsTrusted { get; }
```

Gets a value indicating whether the package is trusted by the current trust policy.

<a id="member-p-cephalon-engine-manifest-packagemanifest-kind"></a>

##### `Kind`

```csharp
string Kind { get; }
```

Gets the package discovery kind, such as assembly path or manifest-file loading.

<a id="member-p-cephalon-engine-manifest-packagemanifest-loadcontext"></a>

##### `LoadContext`

```csharp
string LoadContext { get; }
```

Gets the assembly load context name used for the package.

<a id="member-p-cephalon-engine-manifest-packagemanifest-maximumengineversion"></a>

##### `MaximumEngineVersion`

```csharp
string MaximumEngineVersion { get; }
```

Gets the maximum engine version supported by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-minimumengineversion"></a>

##### `MinimumEngineVersion`

```csharp
string MinimumEngineVersion { get; }
```

Gets the minimum engine version required by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-modules"></a>

##### `Modules`

```csharp
IReadOnlyList<string> Modules { get; }
```

Gets the identifiers of modules contributed by the package.

<a id="member-p-cephalon-engine-manifest-packagemanifest-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the resolved assembly path that the engine loaded.

<a id="member-p-cephalon-engine-manifest-packagemanifest-publisherdisplayname"></a>

##### `PublisherDisplayName`

```csharp
string PublisherDisplayName { get; }
```

Gets the publisher display name declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-publisherid"></a>

##### `PublisherId`

```csharp
string PublisherId { get; }
```

Gets the stable publisher identifier declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-publisherwebsite"></a>

##### `PublisherWebsite`

```csharp
string PublisherWebsite { get; }
```

Gets the publisher website declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signaturealgorithm"></a>

##### `SignatureAlgorithm`

```csharp
string SignatureAlgorithm { get; }
```

Gets the signature algorithm declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signaturecertificatethumbprint"></a>

##### `SignatureCertificateThumbprint`

```csharp
string SignatureCertificateThumbprint { get; }
```

Gets the primary signing certificate thumbprint used during verification, when certificate-backed trust was used.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signaturefingerprint"></a>

##### `SignatureFingerprint`

```csharp
string SignatureFingerprint { get; }
```

Gets the signer fingerprint declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signaturekeyid"></a>

##### `SignatureKeyId`

```csharp
string SignatureKeyId { get; }
```

Gets the trusted-key identifier declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signatures"></a>

##### `Signatures`

```csharp
IReadOnlyList<PackageSignatureManifest> Signatures { get; }
```

Gets the declared package signatures and their individual verification outcomes.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signaturesigner"></a>

##### `SignatureSigner`

```csharp
string SignatureSigner { get; }
```

Gets the signer identity declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signaturetype"></a>

##### `SignatureType`

```csharp
string SignatureType { get; }
```

Gets the signature metadata type declared by the package manifest, when available.

<a id="member-p-cephalon-engine-manifest-packagemanifest-signatureverificationreason"></a>

##### `SignatureVerificationReason`

```csharp
string SignatureVerificationReason { get; }
```

Gets the verification outcome summary for the package signature.

<a id="member-p-cephalon-engine-manifest-packagemanifest-sourcepath"></a>

##### `SourcePath`

```csharp
string SourcePath { get; }
```

Gets the original source path used to discover the package.

<a id="member-p-cephalon-engine-manifest-packagemanifest-supportedtargetframeworks"></a>

##### `SupportedTargetFrameworks`

```csharp
IReadOnlyList<string> SupportedTargetFrameworks { get; }
```

Gets the target frameworks declared as compatible by the package manifest.

<a id="member-p-cephalon-engine-manifest-packagemanifest-trustreason"></a>

##### `TrustReason`

```csharp
string TrustReason { get; }
```

Gets the reason the package is trusted or not trusted by the current trust policy.

<a id="member-p-cephalon-engine-manifest-packagemanifest-version"></a>

##### `Version`

```csharp
string Version { get; }
```

Gets the package version declared by the package manifest, when available.

<a id="type-cephalon-engine-manifest-packagesignaturemanifest"></a>

### `PackageSignatureManifest`

Describes a single package signature declared by a package manifest and the outcome of verifying it.

#### Declaration
```csharp
public sealed class PackageSignatureManifest
```

#### Constructors

<a id="member-m-cephalon-engine-manifest-packagesignaturemanifest-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-boolean-system-string"></a>

##### `PackageSignatureManifest`

```csharp
PackageSignatureManifest(string type, string signer, string keyId, string fingerprint, string algorithm, string verificationSource, string certificateThumbprint, bool isVerified, string verificationReason)
```

Creates a package signature manifest entry.

Parameters:
- `type`: The declared signature type.
- `signer`: The declared signer identity.
- `keyId`: The declared signature key identifier.
- `fingerprint`: The declared signer fingerprint.
- `algorithm`: The declared signature algorithm.
- `verificationSource`: The trust source that verified the signature, when available.
- `certificateThumbprint`: The thumbprint of the signing certificate used during verification, when certificate-backed trust was used.
- `isVerified`: Whether this signature was cryptographically verified against a trusted public key or signing certificate.
- `verificationReason`: The verification outcome summary for this signature.

#### Properties

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-algorithm"></a>

##### `Algorithm`

```csharp
string Algorithm { get; }
```

Gets the declared signature algorithm.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-certificatethumbprint"></a>

##### `CertificateThumbprint`

```csharp
string CertificateThumbprint { get; }
```

Gets the signing certificate thumbprint used during verification, when certificate-backed trust was used.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-fingerprint"></a>

##### `Fingerprint`

```csharp
string Fingerprint { get; }
```

Gets the declared signer fingerprint.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-isverified"></a>

##### `IsVerified`

```csharp
bool IsVerified { get; }
```

Gets a value indicating whether this signature was cryptographically verified against a trusted signing identity.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-keyid"></a>

##### `KeyId`

```csharp
string KeyId { get; }
```

Gets the declared signature key identifier.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-signer"></a>

##### `Signer`

```csharp
string Signer { get; }
```

Gets the declared signer identity.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-type"></a>

##### `Type`

```csharp
string Type { get; }
```

Gets the declared signature type.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-verificationreason"></a>

##### `VerificationReason`

```csharp
string VerificationReason { get; }
```

Gets the verification outcome summary for this signature.

<a id="member-p-cephalon-engine-manifest-packagesignaturemanifest-verificationsource"></a>

##### `VerificationSource`

```csharp
string VerificationSource { get; }
```

Gets the trust source that verified the signature, when available.

<a id="type-cephalon-engine-manifest-runtimemanifest"></a>

### `RuntimeManifest`

Represents the immutable manifest produced when a Cephalon runtime is built.

Remarks: The runtime manifest is the main contract for describing the built engine shape. It captures the selected application profile, the effective module set, the published capabilities, and any package-loading metadata that contributed modules to the runtime.

#### Declaration
```csharp
public sealed class RuntimeManifest
```

#### Constructors

<a id="member-m-cephalon-engine-manifest-runtimemanifest-ctor-system-string-system-string-system-datetimeoffset-cephalon-abstractions-appmodel-appprofile-system-collections-generic-ireadonlylist-cephalon-engine-manifest-modulemanifest-system-collections-generic-ireadonlylist-cephalon-engine-manifest-capabilitymanifest-system-collections-generic-ireadonlylist-cephalon-engine-manifest-packagemanifest"></a>

##### `RuntimeManifest`

```csharp
RuntimeManifest(string manifestVersion, string engineVersion, DateTimeOffset generatedAtUtc, AppProfile appProfile, IReadOnlyList<ModuleManifest> modules, IReadOnlyList<CapabilityManifest> capabilities, IReadOnlyList<PackageManifest> packages)
```

Creates a new runtime manifest.

Parameters:
- `manifestVersion`: The manifest schema version.
- `engineVersion`: The version of the engine that produced the manifest.
- `generatedAtUtc`: The UTC timestamp when the manifest was created.
- `appProfile`: The resolved application profile.
- `modules`: The effective ordered module set.
- `capabilities`: The effective capability set after policy has been applied.
- `packages`: The package-loading metadata associated with the runtime.

#### Fields

<a id="member-f-cephalon-engine-manifest-runtimemanifest-currentversion"></a>

##### `CurrentVersion`

```csharp
const string CurrentVersion
```

Gets the current manifest schema version emitted by the engine.

#### Properties

<a id="member-p-cephalon-engine-manifest-runtimemanifest-appprofile"></a>

##### `AppProfile`

```csharp
AppProfile AppProfile { get; }
```

Gets the resolved application profile, including blueprint, patterns, transports, technologies, and any scaffold guidance.

<a id="member-p-cephalon-engine-manifest-runtimemanifest-capabilities"></a>

##### `Capabilities`

```csharp
IReadOnlyList<CapabilityManifest> Capabilities { get; }
```

Gets the effective capability set after capability and trust policy filtering.

<a id="member-p-cephalon-engine-manifest-runtimemanifest-engineversion"></a>

##### `EngineVersion`

```csharp
string EngineVersion { get; }
```

Gets the engine version that produced the manifest.

<a id="member-p-cephalon-engine-manifest-runtimemanifest-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
DateTimeOffset GeneratedAtUtc { get; }
```

Gets the UTC timestamp when the manifest was generated.

<a id="member-p-cephalon-engine-manifest-runtimemanifest-manifestversion"></a>

##### `ManifestVersion`

```csharp
string ManifestVersion { get; }
```

Gets the manifest schema version.

<a id="member-p-cephalon-engine-manifest-runtimemanifest-modules"></a>

##### `Modules`

```csharp
IReadOnlyList<ModuleManifest> Modules { get; }
```

Gets the effective module set after discovery, policy filtering, and dependency ordering.

<a id="member-p-cephalon-engine-manifest-runtimemanifest-packages"></a>

##### `Packages`

```csharp
IReadOnlyList<PackageManifest> Packages { get; }
```

Gets the packages that contributed modules to the runtime, if any were loaded from packages.

<a id="namespace-cephalon-engine-patterns"></a>

## Namespace Cephalon.Engine.Patterns

<a id="type-cephalon-engine-patterns-builtinpatterns"></a>

### `BuiltInPatterns`

Provides the built-in pattern descriptors used by Cephalon app profiles.

#### Declaration
```csharp
public static class BuiltInPatterns
```

#### Properties

<a id="member-p-cephalon-engine-patterns-builtinpatterns-all"></a>

##### `All`

```csharp
IReadOnlyList<PatternDescriptor> All { get; }
```

Gets all built-in pattern descriptors.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-mediatorpattern"></a>

##### `MediatorPattern`

```csharp
PatternDescriptor MediatorPattern { get; }
```

Gets the mediator design pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-microservicetopology"></a>

##### `MicroserviceTopology`

```csharp
PatternDescriptor MicroserviceTopology { get; }
```

Gets the microservice deployment topology pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-modulararchitecture"></a>

##### `ModularArchitecture`

```csharp
PatternDescriptor ModularArchitecture { get; }
```

Gets the modular-architecture composition pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-modulefirstorganization"></a>

##### `ModuleFirstOrganization`

```csharp
PatternDescriptor ModuleFirstOrganization { get; }
```

Gets the module-first organization pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-pipelinepattern"></a>

##### `PipelinePattern`

```csharp
PatternDescriptor PipelinePattern { get; }
```

Gets the pipeline design pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-sharedfoundationpattern"></a>

##### `SharedFoundationPattern`

```csharp
PatternDescriptor SharedFoundationPattern { get; }
```

Gets the shared-foundation pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-singlehosttopology"></a>

##### `SingleHostTopology`

```csharp
PatternDescriptor SingleHostTopology { get; }
```

Gets the single-host deployment topology pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-specificationpattern"></a>

##### `SpecificationPattern`

```csharp
PatternDescriptor SpecificationPattern { get; }
```

Gets the specification design pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-strategypattern"></a>

##### `StrategyPattern`

```csharp
PatternDescriptor StrategyPattern { get; }
```

Gets the strategy design pattern.

<a id="member-p-cephalon-engine-patterns-builtinpatterns-verticalsliceorganization"></a>

##### `VerticalSliceOrganization`

```csharp
PatternDescriptor VerticalSliceOrganization { get; }
```

Gets the vertical-slice organization pattern.

#### Methods

<a id="member-m-cephalon-engine-patterns-builtinpatterns-resolve-system-string"></a>

##### `Resolve`

```csharp
PatternDescriptor Resolve(string value)
```

Resolves a pattern identifier, display name, or alias.

Returns: The resolved pattern descriptor.

Parameters:
- `value`: The pattern identifier, display name, or alias to resolve.

<a id="member-m-cephalon-engine-patterns-builtinpatterns-tryresolve-system-string-cephalon-abstractions-patterns-patterndescriptor"></a>

##### `TryResolve`

```csharp
bool TryResolve(string value, out PatternDescriptor pattern)
```

Attempts to resolve a pattern identifier, display name, or alias.

Returns: `true` when the pattern was resolved; otherwise, `false`.

Parameters:
- `value`: The pattern identifier, display name, or alias to resolve.
- `pattern`: The resolved pattern descriptor when the lookup succeeds.

<a id="namespace-cephalon-engine-runtime"></a>

## Namespace Cephalon.Engine.Runtime

<a id="type-cephalon-engine-runtime-engineruntime"></a>

### `EngineRuntime`

Executes module lifecycle transitions and exposes runtime status, manifest, and failure information.

#### Declaration
```csharp
public sealed class EngineRuntime
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-engineruntime-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-modules-imodule-cephalon-engine-manifest-runtimemanifest-cephalon-engine-configuration-failurepolicy"></a>

##### `EngineRuntime`

```csharp
EngineRuntime(IReadOnlyList<IModule> modules, RuntimeManifest manifest, FailurePolicy failurePolicy)
```

Initializes a new instance of the `EngineRuntime` class.

Parameters:
- `modules`: The modules that participate in runtime lifecycle transitions.
- `manifest`: The runtime manifest that describes the built runtime shape.
- `failurePolicy`: The failure policy that governs startup, stop, and restart behavior.

#### Properties

<a id="member-p-cephalon-engine-runtime-engineruntime-failurepolicy"></a>

##### `FailurePolicy`

```csharp
FailurePolicy FailurePolicy { get; }
```

Gets the failure policy that governs startup, stop, and restart behavior.

<a id="member-p-cephalon-engine-runtime-engineruntime-lastfailure"></a>

##### `LastFailure`

```csharp
RuntimeFailureInfo LastFailure { get; }
```

Gets the last captured lifecycle failure when one is available.

<a id="member-p-cephalon-engine-runtime-engineruntime-manifest"></a>

##### `Manifest`

```csharp
RuntimeManifest Manifest { get; }
```

Gets the runtime manifest that describes the built runtime shape.

<a id="member-p-cephalon-engine-runtime-engineruntime-modules"></a>

##### `Modules`

```csharp
IReadOnlyList<IModule> Modules { get; }
```

Gets the modules that participate in runtime lifecycle transitions.

<a id="member-p-cephalon-engine-runtime-engineruntime-operationalstory"></a>

##### `OperationalStory`

```csharp
RuntimeOperationalStory OperationalStory { get; }
```

Gets the richer operator-facing lifecycle story for the runtime.

<a id="member-p-cephalon-engine-runtime-engineruntime-restartcount"></a>

##### `RestartCount`

```csharp
int RestartCount { get; }
```

Gets the number of completed manual restarts.

<a id="member-p-cephalon-engine-runtime-engineruntime-status"></a>

##### `Status`

```csharp
RuntimeStatus Status { get; }
```

Gets the current lifecycle status.

<a id="member-p-cephalon-engine-runtime-engineruntime-statussnapshot"></a>

##### `StatusSnapshot`

```csharp
RuntimeStatusSnapshot StatusSnapshot { get; }
```

Gets a serialization-friendly snapshot of the current runtime status.

#### Methods

<a id="member-m-cephalon-engine-runtime-engineruntime-dispose"></a>

##### `Dispose`

```csharp
void Dispose()
```

Releases runtime resources.

<a id="member-m-cephalon-engine-runtime-engineruntime-initializeasync-system-iserviceprovider-system-threading-cancellationtoken"></a>

##### `InitializeAsync`

```csharp
Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
```

Initializes the runtime and its modules.

Returns: A task that completes when initialization finishes.

Parameters:
- `services`: The service provider bound to the runtime lifecycle.
- `cancellationToken`: The cancellation token for the initialization operation.

<a id="member-m-cephalon-engine-runtime-engineruntime-restartasync-system-iserviceprovider-system-threading-cancellationtoken"></a>

##### `RestartAsync`

```csharp
Task RestartAsync(IServiceProvider services, CancellationToken cancellationToken)
```

Restarts the runtime when the current failure policy allows it.

Returns: A task that completes when the restart finishes.

Parameters:
- `services`: The service provider bound to the runtime lifecycle.
- `cancellationToken`: The cancellation token for the restart operation.

<a id="member-m-cephalon-engine-runtime-engineruntime-startasync-system-iserviceprovider-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
```

Starts the runtime and its modules.

Returns: A task that completes when startup finishes.

Parameters:
- `services`: The service provider bound to the runtime lifecycle.
- `cancellationToken`: The cancellation token for the startup operation.

<a id="member-m-cephalon-engine-runtime-engineruntime-stopasync-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(CancellationToken cancellationToken)
```

Stops started modules and transitions the runtime to a stopped state.

Returns: A task that completes when shutdown finishes.

Parameters:
- `cancellationToken`: The cancellation token for the stop operation.

<a id="type-cephalon-engine-runtime-iruntime"></a>

### `IRuntime`

Represents a built Cephalon runtime that can be initialized, started, restarted, stopped, and introspected by a host.

#### Declaration
```csharp
public interface IRuntime
```

#### Properties

<a id="member-p-cephalon-engine-runtime-iruntime-failurepolicy"></a>

##### `FailurePolicy`

```csharp
FailurePolicy FailurePolicy { get; }
```

Gets the failure policy that governs startup, stop, and restart behavior.

<a id="member-p-cephalon-engine-runtime-iruntime-lastfailure"></a>

##### `LastFailure`

```csharp
RuntimeFailureInfo LastFailure { get; }
```

Gets the most recent failure captured by the lifecycle state machine, if any.

<a id="member-p-cephalon-engine-runtime-iruntime-manifest"></a>

##### `Manifest`

```csharp
RuntimeManifest Manifest { get; }
```

Gets the immutable manifest that was produced when the runtime was built.

<a id="member-p-cephalon-engine-runtime-iruntime-modules"></a>

##### `Modules`

```csharp
IReadOnlyList<IModule> Modules { get; }
```

Gets the ordered module set that participates in runtime lifecycle execution.

<a id="member-p-cephalon-engine-runtime-iruntime-operationalstory"></a>

##### `OperationalStory`

```csharp
RuntimeOperationalStory OperationalStory { get; }
```

Gets the richer operator-facing runtime story that explains what loaded, started, failed, and why.

<a id="member-p-cephalon-engine-runtime-iruntime-restartcount"></a>

##### `RestartCount`

```csharp
int RestartCount { get; }
```

Gets the number of successful manual restart attempts completed by the runtime.

<a id="member-p-cephalon-engine-runtime-iruntime-status"></a>

##### `Status`

```csharp
RuntimeStatus Status { get; }
```

Gets the current lifecycle status of the runtime.

<a id="member-p-cephalon-engine-runtime-iruntime-statussnapshot"></a>

##### `StatusSnapshot`

```csharp
RuntimeStatusSnapshot StatusSnapshot { get; }
```

Gets the current status as a serializable snapshot.

#### Methods

<a id="member-m-cephalon-engine-runtime-iruntime-initializeasync-system-iserviceprovider-system-threading-cancellationtoken"></a>

##### `InitializeAsync`

```csharp
Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
```

Initializes the runtime and all lifecycle-aware modules.

Parameters:
- `services`: The service provider that modules should use during initialization.
- `cancellationToken`: A token that can cancel the initialization operation.

<a id="member-m-cephalon-engine-runtime-iruntime-restartasync-system-iserviceprovider-system-threading-cancellationtoken"></a>

##### `RestartAsync`

```csharp
Task RestartAsync(IServiceProvider services, CancellationToken cancellationToken)
```

Restarts the runtime when the configured failure policy allows it.

Parameters:
- `services`: The service provider that modules should use during the restart flow.
- `cancellationToken`: A token that can cancel the restart operation.

<a id="member-m-cephalon-engine-runtime-iruntime-startasync-system-iserviceprovider-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
```

Starts the runtime and all lifecycle-aware modules.

Remarks: If the runtime has not been initialized yet, the implementation may initialize it first.

Parameters:
- `services`: The service provider that modules should use during startup.
- `cancellationToken`: A token that can cancel the start operation.

<a id="member-m-cephalon-engine-runtime-iruntime-stopasync-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(CancellationToken cancellationToken)
```

Stops the runtime and all started lifecycle-aware modules.

Parameters:
- `cancellationToken`: A token that can cancel the stop operation.

<a id="type-cephalon-engine-runtime-iruntimeintrospectionsnapshotprovider"></a>

### `IRuntimeIntrospectionSnapshotProvider`

Creates operator-facing runtime introspection snapshots.

#### Declaration
```csharp
public interface IRuntimeIntrospectionSnapshotProvider
```

#### Methods

<a id="member-m-cephalon-engine-runtime-iruntimeintrospectionsnapshotprovider-createsnapshot"></a>

##### `CreateSnapshot`

```csharp
RuntimeIntrospectionSnapshot CreateSnapshot()
```

Creates a new runtime introspection snapshot from the current engine state.

Returns: The composed runtime snapshot.

<a id="type-cephalon-engine-runtime-runtimefailureinfo"></a>

### `RuntimeFailureInfo`

Describes a runtime lifecycle failure in a way that can be surfaced through diagnostics, status endpoints, and operator tooling.

#### Declaration
```csharp
public sealed class RuntimeFailureInfo
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimefailureinfo-ctor-system-string-system-string-system-string-cephalon-engine-runtime-runtimestatus-system-string-system-string-system-datetimeoffset-system-boolean-system-nullable-system-datetimeoffset-cephalon-engine-configuration-startupfailurebehavior-cephalon-engine-configuration-stopfailurebehavior"></a>

##### `RuntimeFailureInfo`

```csharp
RuntimeFailureInfo(string Phase, string ModuleId, string ModuleVersion, RuntimeStatus StatusBeforeFailure, string ExceptionType, string Message, DateTimeOffset OccurredAtUtc, bool CanRestart, DateTimeOffset? RestartAvailableAtUtc, StartupFailureBehavior StartupFailureBehavior, StopFailureBehavior StopFailureBehavior)
```

Describes a runtime lifecycle failure in a way that can be surfaced through diagnostics, status endpoints, and operator tooling.

Parameters:
- `Phase`: The lifecycle phase that failed, such as `initialize`, `start`, or `stop`.
- `ModuleId`: The module identifier that triggered the failure when available.
- `ModuleVersion`: The module version that was active when the failure occurred, if known.
- `StatusBeforeFailure`: The runtime status immediately before the failure was captured.
- `ExceptionType`: The fully qualified exception type that caused the failure.
- `Message`: The failure message surfaced to operators.
- `OccurredAtUtc`: The UTC timestamp when the failure was captured.
- `CanRestart`: Whether the current policy allows a manual restart after this failure.
- `RestartAvailableAtUtc`: The UTC timestamp when a manual restart exits its configured backoff window, if one applies.
- `StartupFailureBehavior`: The startup failure behavior in effect when the failure occurred.
- `StopFailureBehavior`: The stop failure behavior in effect when the failure occurred.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-canrestart"></a>

##### `CanRestart`

```csharp
bool CanRestart { get; set; }
```

Whether the current policy allows a manual restart after this failure.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-exceptiontype"></a>

##### `ExceptionType`

```csharp
string ExceptionType { get; set; }
```

The fully qualified exception type that caused the failure.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

The failure message surfaced to operators.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-moduleid"></a>

##### `ModuleId`

```csharp
string ModuleId { get; set; }
```

The module identifier that triggered the failure when available.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-moduleversion"></a>

##### `ModuleVersion`

```csharp
string ModuleVersion { get; set; }
```

The module version that was active when the failure occurred, if known.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; set; }
```

The UTC timestamp when the failure was captured.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-phase"></a>

##### `Phase`

```csharp
string Phase { get; set; }
```

The lifecycle phase that failed, such as `initialize`, `start`, or `stop`.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-restartavailableatutc"></a>

##### `RestartAvailableAtUtc`

```csharp
DateTimeOffset? RestartAvailableAtUtc { get; set; }
```

The UTC timestamp when a manual restart exits its configured backoff window, if one applies.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-startupfailurebehavior"></a>

##### `StartupFailureBehavior`

```csharp
StartupFailureBehavior StartupFailureBehavior { get; set; }
```

The startup failure behavior in effect when the failure occurred.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-statusbeforefailure"></a>

##### `StatusBeforeFailure`

```csharp
RuntimeStatus StatusBeforeFailure { get; set; }
```

The runtime status immediately before the failure was captured.

<a id="member-p-cephalon-engine-runtime-runtimefailureinfo-stopfailurebehavior"></a>

##### `StopFailureBehavior`

```csharp
StopFailureBehavior StopFailureBehavior { get; set; }
```

The stop failure behavior in effect when the failure occurred.

<a id="type-cephalon-engine-runtime-runtimehealthevaluator"></a>

### `RuntimeHealthEvaluator`

Evaluates runtime liveness, readiness, and dependency health.

#### Declaration
```csharp
public sealed class RuntimeHealthEvaluator
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimehealthevaluator-ctor-cephalon-engine-runtime-iruntime-system-collections-generic-ienumerable-cephalon-abstractions-health-idependencyhealthcontributor"></a>

##### `RuntimeHealthEvaluator`

```csharp
RuntimeHealthEvaluator(IRuntime runtime, IEnumerable<IDependencyHealthContributor> dependencyHealthContributors)
```

Initializes a new instance of the `RuntimeHealthEvaluator` class.

Parameters:
- `runtime`: The runtime to evaluate.
- `dependencyHealthContributors`: The dependency contributors that provide health data.

#### Methods

<a id="member-m-cephalon-engine-runtime-runtimehealthevaluator-evaluatedependencies"></a>

##### `EvaluateDependencies`

```csharp
DependencyHealthReport[] EvaluateDependencies()
```

Evaluates dependency-level health reports without applying probe semantics.

Returns: The dependency-health reports visible to the evaluator.

<a id="member-m-cephalon-engine-runtime-runtimehealthevaluator-evaluateliveness"></a>

##### `EvaluateLiveness`

```csharp
RuntimeHealthReport EvaluateLiveness()
```

Evaluates whether the runtime process is live.

Returns: The liveness report.

<a id="member-m-cephalon-engine-runtime-runtimehealthevaluator-evaluatereadiness"></a>

##### `EvaluateReadiness`

```csharp
RuntimeHealthReport EvaluateReadiness()
```

Evaluates whether the runtime is ready to serve traffic.

Returns: The readiness report.

<a id="type-cephalon-engine-runtime-runtimehealthreport"></a>

### `RuntimeHealthReport`

Captures the health result for a runtime liveness or readiness probe.

#### Declaration
```csharp
public sealed class RuntimeHealthReport
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimehealthreport-ctor-system-string-cephalon-engine-runtime-runtimehealthstate-system-string-cephalon-engine-runtime-runtimestatus-system-int32-cephalon-engine-runtime-runtimefailureinfo-system-collections-generic-ireadonlylist-cephalon-abstractions-health-dependencyhealthreport-system-string-system-nullable-system-datetimeoffset"></a>

##### `RuntimeHealthReport`

```csharp
RuntimeHealthReport(string Probe, RuntimeHealthState State, string Description, RuntimeStatus RuntimeStatus, int RestartCount, RuntimeFailureInfo LastFailure, IReadOnlyList<DependencyHealthReport> Dependencies, string ActiveWindow, DateTimeOffset? ActiveWindowEndsAtUtc)
```

Captures the health result for a runtime liveness or readiness probe.

Parameters:
- `Probe`: The probe name that produced the report.
- `State`: The evaluated runtime health state.
- `Description`: A human-readable description of the evaluated state.
- `RuntimeStatus`: The runtime lifecycle status at the time of evaluation.
- `RestartCount`: The number of completed manual restarts.
- `LastFailure`: The last runtime failure when one is available.
- `Dependencies`: The dependency-health reports visible during evaluation.
- `ActiveWindow`: The active policy-driven lifecycle window, such as startup warmup, shutdown drain, or restart backoff.
- `ActiveWindowEndsAtUtc`: The UTC timestamp when the active lifecycle window ends, if applicable.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-activewindow"></a>

##### `ActiveWindow`

```csharp
string ActiveWindow { get; set; }
```

The active policy-driven lifecycle window, such as startup warmup, shutdown drain, or restart backoff.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-activewindowendsatutc"></a>

##### `ActiveWindowEndsAtUtc`

```csharp
DateTimeOffset? ActiveWindowEndsAtUtc { get; set; }
```

The UTC timestamp when the active lifecycle window ends, if applicable.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-dependencies"></a>

##### `Dependencies`

```csharp
IReadOnlyList<DependencyHealthReport> Dependencies { get; set; }
```

The dependency-health reports visible during evaluation.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

A human-readable description of the evaluated state.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-ishealthy"></a>

##### `IsHealthy`

```csharp
bool IsHealthy { get; }
```

Gets a value indicating whether the report represents a healthy state.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-lastfailure"></a>

##### `LastFailure`

```csharp
RuntimeFailureInfo LastFailure { get; set; }
```

The last runtime failure when one is available.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-probe"></a>

##### `Probe`

```csharp
string Probe { get; set; }
```

The probe name that produced the report.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-restartcount"></a>

##### `RestartCount`

```csharp
int RestartCount { get; set; }
```

The number of completed manual restarts.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-runtimestatus"></a>

##### `RuntimeStatus`

```csharp
RuntimeStatus RuntimeStatus { get; set; }
```

The runtime lifecycle status at the time of evaluation.

<a id="member-p-cephalon-engine-runtime-runtimehealthreport-state"></a>

##### `State`

```csharp
RuntimeHealthState State { get; set; }
```

The evaluated runtime health state.

<a id="type-cephalon-engine-runtime-runtimehealthstate"></a>

### `RuntimeHealthState`

Represents the overall health state of the runtime.

#### Declaration
```csharp
public enum RuntimeHealthState
```

#### Fields

<a id="member-f-cephalon-engine-runtime-runtimehealthstate-degraded"></a>

##### `Degraded`

```csharp
const RuntimeHealthState Degraded
```

The runtime is available, but one or more dependencies need attention.

<a id="member-f-cephalon-engine-runtime-runtimehealthstate-healthy"></a>

##### `Healthy`

```csharp
const RuntimeHealthState Healthy
```

The runtime and its dependencies are healthy.

<a id="member-f-cephalon-engine-runtime-runtimehealthstate-unhealthy"></a>

##### `Unhealthy`

```csharp
const RuntimeHealthState Unhealthy
```

The runtime is not healthy enough to serve traffic.

<a id="type-cephalon-engine-runtime-runtimeintrospectionsnapshot"></a>

### `RuntimeIntrospectionSnapshot`

Combines the main operator-facing runtime views into a single payload.

Remarks: This snapshot is intended for tooling and operator surfaces that need one coherent view of the runtime without issuing separate requests for manifest, status, technology-pack details, diagnostics conventions, and lifecycle story data.

#### Declaration
```csharp
public sealed class RuntimeIntrospectionSnapshot
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimeintrospectionsnapshot-ctor-cephalon-engine-manifest-runtimemanifest-cephalon-engine-runtime-runtimestatussnapshot-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologyruntimesurface-system-collections-generic-ireadonlylist-cephalon-engine-diagnostics-diagnosticsconvention-cephalon-engine-runtime-runtimeoperationalstory"></a>

##### `RuntimeIntrospectionSnapshot`

```csharp
RuntimeIntrospectionSnapshot(RuntimeManifest Manifest, RuntimeStatusSnapshot Status, IReadOnlyList<TechnologyRuntimeSurface> TechnologySurfaces, IReadOnlyList<DiagnosticsConvention> DiagnosticsConventions, RuntimeOperationalStory OperationalStory)
```

Combines the main operator-facing runtime views into a single payload.

Remarks: This snapshot is intended for tooling and operator surfaces that need one coherent view of the runtime without issuing separate requests for manifest, status, technology-pack details, diagnostics conventions, and lifecycle story data.

Parameters:
- `Manifest`: The immutable manifest that describes the built runtime shape.
- `Status`: The current lifecycle status of the runtime.
- `TechnologySurfaces`: The active technology-pack runtime surfaces visible to the runtime at the time the snapshot was created.
- `DiagnosticsConventions`: The diagnostics conventions and published event-id catalogs visible to the runtime at the time the snapshot was created.
- `OperationalStory`: The richer operator-facing lifecycle story that combines loaded packages, module state, and the ordered runtime timeline.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimeintrospectionsnapshot-diagnosticsconventions"></a>

##### `DiagnosticsConventions`

```csharp
IReadOnlyList<DiagnosticsConvention> DiagnosticsConventions { get; set; }
```

The diagnostics conventions and published event-id catalogs visible to the runtime at the time the snapshot was created.

<a id="member-p-cephalon-engine-runtime-runtimeintrospectionsnapshot-manifest"></a>

##### `Manifest`

```csharp
RuntimeManifest Manifest { get; set; }
```

The immutable manifest that describes the built runtime shape.

<a id="member-p-cephalon-engine-runtime-runtimeintrospectionsnapshot-operationalstory"></a>

##### `OperationalStory`

```csharp
RuntimeOperationalStory OperationalStory { get; set; }
```

The richer operator-facing lifecycle story that combines loaded packages, module state, and the ordered runtime timeline.

<a id="member-p-cephalon-engine-runtime-runtimeintrospectionsnapshot-status"></a>

##### `Status`

```csharp
RuntimeStatusSnapshot Status { get; set; }
```

The current lifecycle status of the runtime.

<a id="member-p-cephalon-engine-runtime-runtimeintrospectionsnapshot-technologysurfaces"></a>

##### `TechnologySurfaces`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> TechnologySurfaces { get; set; }
```

The active technology-pack runtime surfaces visible to the runtime at the time the snapshot was created.

<a id="type-cephalon-engine-runtime-runtimelifecycleevent"></a>

### `RuntimeLifecycleEvent`

Captures one operator-facing lifecycle event in the runtime story timeline.

#### Declaration
```csharp
public sealed class RuntimeLifecycleEvent
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimelifecycleevent-ctor-system-datetimeoffset-cephalon-engine-runtime-runtimelifecycleeventscope-system-string-cephalon-engine-runtime-runtimelifecycleeventoutcome-cephalon-engine-runtime-runtimestatus-system-string-system-string-system-string-system-string"></a>

##### `RuntimeLifecycleEvent`

```csharp
RuntimeLifecycleEvent(DateTimeOffset OccurredAtUtc, RuntimeLifecycleEventScope Scope, string Phase, RuntimeLifecycleEventOutcome Outcome, RuntimeStatus RuntimeStatus, string SubjectId, string SubjectVersion, string Message, string ExceptionType)
```

Captures one operator-facing lifecycle event in the runtime story timeline.

Parameters:
- `OccurredAtUtc`: The UTC timestamp when the event was recorded.
- `Scope`: The runtime surface that emitted the event.
- `Phase`: The lifecycle phase or story phase, such as `load`, `initialize`, `start`, `stop`, or `restart`.
- `Outcome`: The completion outcome for the event.
- `RuntimeStatus`: The runtime status visible when the event was recorded.
- `SubjectId`: The runtime, module, or package identifier associated with the event when available.
- `SubjectVersion`: The version associated with the event subject when available.
- `Message`: The operator-facing narrative for the event.
- `ExceptionType`: The exception type captured for failed events when available.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-exceptiontype"></a>

##### `ExceptionType`

```csharp
string ExceptionType { get; set; }
```

The exception type captured for failed events when available.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-message"></a>

##### `Message`

```csharp
string Message { get; set; }
```

The operator-facing narrative for the event.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; set; }
```

The UTC timestamp when the event was recorded.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-outcome"></a>

##### `Outcome`

```csharp
RuntimeLifecycleEventOutcome Outcome { get; set; }
```

The completion outcome for the event.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-phase"></a>

##### `Phase`

```csharp
string Phase { get; set; }
```

The lifecycle phase or story phase, such as `load`, `initialize`, `start`, `stop`, or `restart`.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-runtimestatus"></a>

##### `RuntimeStatus`

```csharp
RuntimeStatus RuntimeStatus { get; set; }
```

The runtime status visible when the event was recorded.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-scope"></a>

##### `Scope`

```csharp
RuntimeLifecycleEventScope Scope { get; set; }
```

The runtime surface that emitted the event.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-subjectid"></a>

##### `SubjectId`

```csharp
string SubjectId { get; set; }
```

The runtime, module, or package identifier associated with the event when available.

<a id="member-p-cephalon-engine-runtime-runtimelifecycleevent-subjectversion"></a>

##### `SubjectVersion`

```csharp
string SubjectVersion { get; set; }
```

The version associated with the event subject when available.

<a id="type-cephalon-engine-runtime-runtimelifecycleeventoutcome"></a>

### `RuntimeLifecycleEventOutcome`

Describes whether one lifecycle event completed successfully or failed.

#### Declaration
```csharp
public enum RuntimeLifecycleEventOutcome
```

#### Fields

<a id="member-f-cephalon-engine-runtime-runtimelifecycleeventoutcome-failed"></a>

##### `Failed`

```csharp
const RuntimeLifecycleEventOutcome Failed
```

The lifecycle event failed.

<a id="member-f-cephalon-engine-runtime-runtimelifecycleeventoutcome-succeeded"></a>

##### `Succeeded`

```csharp
const RuntimeLifecycleEventOutcome Succeeded
```

The lifecycle event completed successfully.

<a id="type-cephalon-engine-runtime-runtimelifecycleeventscope"></a>

### `RuntimeLifecycleEventScope`

Identifies which runtime surface produced one operator-facing lifecycle event.

#### Declaration
```csharp
public enum RuntimeLifecycleEventScope
```

#### Fields

<a id="member-f-cephalon-engine-runtime-runtimelifecycleeventscope-module"></a>

##### `Module`

```csharp
const RuntimeLifecycleEventScope Module
```

The event belongs to one module lifecycle transition.

<a id="member-f-cephalon-engine-runtime-runtimelifecycleeventscope-package"></a>

##### `Package`

```csharp
const RuntimeLifecycleEventScope Package
```

The event belongs to package loading and package-origin visibility.

<a id="member-f-cephalon-engine-runtime-runtimelifecycleeventscope-runtime"></a>

##### `Runtime`

```csharp
const RuntimeLifecycleEventScope Runtime
```

The event belongs to the overall runtime lifecycle.

<a id="type-cephalon-engine-runtime-runtimemodulelifecyclestate"></a>

### `RuntimeModuleLifecycleState`

Describes the current operator-facing lifecycle state for one loaded module.

#### Declaration
```csharp
public sealed class RuntimeModuleLifecycleState
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimemodulelifecyclestate-ctor-system-string-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-string-system-nullable-system-datetimeoffset-cephalon-engine-runtime-runtimefailureinfo"></a>

##### `RuntimeModuleLifecycleState`

```csharp
RuntimeModuleLifecycleState(string ModuleId, string DisplayName, string Version, string AssemblyName, string PackageId, DateTimeOffset? LoadedAtUtc, DateTimeOffset? InitializedAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? StoppedAtUtc, string LastObservedPhase, DateTimeOffset? LastObservedAtUtc, RuntimeFailureInfo LastFailure)
```

Describes the current operator-facing lifecycle state for one loaded module.

Parameters:
- `ModuleId`: The stable module identifier.
- `DisplayName`: The operator-facing module display name.
- `Version`: The effective module version.
- `AssemblyName`: The assembly that contains the module implementation.
- `PackageId`: The package that supplied the module when package loading was used.
- `LoadedAtUtc`: The UTC timestamp when the module became part of the built runtime story.
- `InitializedAtUtc`: The UTC timestamp when module initialization last completed successfully.
- `StartedAtUtc`: The UTC timestamp when module startup last completed successfully.
- `StoppedAtUtc`: The UTC timestamp when module shutdown last completed successfully.
- `LastObservedPhase`: The last lifecycle phase recorded for the module.
- `LastObservedAtUtc`: The UTC timestamp when the last lifecycle phase was recorded for the module.
- `LastFailure`: The last failure recorded for the module when one is still relevant to the current runtime story.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-assemblyname"></a>

##### `AssemblyName`

```csharp
string AssemblyName { get; set; }
```

The assembly that contains the module implementation.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

The operator-facing module display name.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-initializedatutc"></a>

##### `InitializedAtUtc`

```csharp
DateTimeOffset? InitializedAtUtc { get; set; }
```

The UTC timestamp when module initialization last completed successfully.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-isinitialized"></a>

##### `IsInitialized`

```csharp
bool IsInitialized { get; }
```

Gets a value indicating whether the module completed initialization successfully.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-isloaded"></a>

##### `IsLoaded`

```csharp
bool IsLoaded { get; }
```

Gets a value indicating whether the module is present in the built runtime.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-isstarted"></a>

##### `IsStarted`

```csharp
bool IsStarted { get; }
```

Gets a value indicating whether the module most recently completed startup without a later successful stop.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-isstopped"></a>

##### `IsStopped`

```csharp
bool IsStopped { get; }
```

Gets a value indicating whether the module most recently completed shutdown.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-lastfailure"></a>

##### `LastFailure`

```csharp
RuntimeFailureInfo LastFailure { get; set; }
```

The last failure recorded for the module when one is still relevant to the current runtime story.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the last lifecycle phase was recorded for the module.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-lastobservedphase"></a>

##### `LastObservedPhase`

```csharp
string LastObservedPhase { get; set; }
```

The last lifecycle phase recorded for the module.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-loadedatutc"></a>

##### `LoadedAtUtc`

```csharp
DateTimeOffset? LoadedAtUtc { get; set; }
```

The UTC timestamp when the module became part of the built runtime story.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-moduleid"></a>

##### `ModuleId`

```csharp
string ModuleId { get; set; }
```

The stable module identifier.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-packageid"></a>

##### `PackageId`

```csharp
string PackageId { get; set; }
```

The package that supplied the module when package loading was used.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-startedatutc"></a>

##### `StartedAtUtc`

```csharp
DateTimeOffset? StartedAtUtc { get; set; }
```

The UTC timestamp when module startup last completed successfully.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-stoppedatutc"></a>

##### `StoppedAtUtc`

```csharp
DateTimeOffset? StoppedAtUtc { get; set; }
```

The UTC timestamp when module shutdown last completed successfully.

<a id="member-p-cephalon-engine-runtime-runtimemodulelifecyclestate-version"></a>

##### `Version`

```csharp
string Version { get; set; }
```

The effective module version.

<a id="type-cephalon-engine-runtime-runtimeoperationalstory"></a>

### `RuntimeOperationalStory`

Combines the operator-facing runtime story into one payload that explains what loaded, started, failed, and why.

#### Declaration
```csharp
public sealed class RuntimeOperationalStory
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimeoperationalstory-ctor-system-datetimeoffset-cephalon-engine-runtime-runtimestatussnapshot-system-collections-generic-ireadonlylist-cephalon-engine-manifest-packagemanifest-system-collections-generic-ireadonlylist-cephalon-engine-runtime-runtimemodulelifecyclestate-system-collections-generic-ireadonlylist-cephalon-engine-runtime-runtimelifecycleevent"></a>

##### `RuntimeOperationalStory`

```csharp
RuntimeOperationalStory(DateTimeOffset GeneratedAtUtc, RuntimeStatusSnapshot Status, IReadOnlyList<PackageManifest> LoadedPackages, IReadOnlyList<RuntimeModuleLifecycleState> Modules, IReadOnlyList<RuntimeLifecycleEvent> Timeline)
```

Combines the operator-facing runtime story into one payload that explains what loaded, started, failed, and why.

Parameters:
- `GeneratedAtUtc`: The UTC timestamp when the story snapshot was created.
- `Status`: The current runtime lifecycle status snapshot.
- `LoadedPackages`: The packages currently visible to the runtime story.
- `Modules`: The current lifecycle state for each loaded module.
- `Timeline`: The ordered lifecycle narrative for package load, module transitions, runtime transitions, and failures.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimeoperationalstory-generatedatutc"></a>

##### `GeneratedAtUtc`

```csharp
DateTimeOffset GeneratedAtUtc { get; set; }
```

The UTC timestamp when the story snapshot was created.

<a id="member-p-cephalon-engine-runtime-runtimeoperationalstory-loadedpackages"></a>

##### `LoadedPackages`

```csharp
IReadOnlyList<PackageManifest> LoadedPackages { get; set; }
```

The packages currently visible to the runtime story.

<a id="member-p-cephalon-engine-runtime-runtimeoperationalstory-modules"></a>

##### `Modules`

```csharp
IReadOnlyList<RuntimeModuleLifecycleState> Modules { get; set; }
```

The current lifecycle state for each loaded module.

<a id="member-p-cephalon-engine-runtime-runtimeoperationalstory-status"></a>

##### `Status`

```csharp
RuntimeStatusSnapshot Status { get; set; }
```

The current runtime lifecycle status snapshot.

<a id="member-p-cephalon-engine-runtime-runtimeoperationalstory-timeline"></a>

##### `Timeline`

```csharp
IReadOnlyList<RuntimeLifecycleEvent> Timeline { get; set; }
```

The ordered lifecycle narrative for package load, module transitions, runtime transitions, and failures.

<a id="type-cephalon-engine-runtime-runtimestatus"></a>

### `RuntimeStatus`

Represents the current lifecycle phase of the runtime.

#### Declaration
```csharp
public enum RuntimeStatus
```

#### Fields

<a id="member-f-cephalon-engine-runtime-runtimestatus-created"></a>

##### `Created`

```csharp
const RuntimeStatus Created
```

The runtime has been created but not initialized.

<a id="member-f-cephalon-engine-runtime-runtimestatus-failed"></a>

##### `Failed`

```csharp
const RuntimeStatus Failed
```

The runtime captured a lifecycle failure.

<a id="member-f-cephalon-engine-runtime-runtimestatus-initialized"></a>

##### `Initialized`

```csharp
const RuntimeStatus Initialized
```

The runtime finished initialization but has not started.

<a id="member-f-cephalon-engine-runtime-runtimestatus-initializing"></a>

##### `Initializing`

```csharp
const RuntimeStatus Initializing
```

The runtime is initializing modules.

<a id="member-f-cephalon-engine-runtime-runtimestatus-started"></a>

##### `Started`

```csharp
const RuntimeStatus Started
```

The runtime is fully started.

<a id="member-f-cephalon-engine-runtime-runtimestatus-starting"></a>

##### `Starting`

```csharp
const RuntimeStatus Starting
```

The runtime is starting modules.

<a id="member-f-cephalon-engine-runtime-runtimestatus-stopped"></a>

##### `Stopped`

```csharp
const RuntimeStatus Stopped
```

The runtime is stopped.

<a id="member-f-cephalon-engine-runtime-runtimestatus-stopping"></a>

##### `Stopping`

```csharp
const RuntimeStatus Stopping
```

The runtime is stopping started modules.

<a id="type-cephalon-engine-runtime-runtimestatussnapshot"></a>

### `RuntimeStatusSnapshot`

Captures the current runtime lifecycle state in a serialization-friendly form.

#### Declaration
```csharp
public sealed class RuntimeStatusSnapshot
```

#### Constructors

<a id="member-m-cephalon-engine-runtime-runtimestatussnapshot-ctor-cephalon-engine-runtime-runtimestatus-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-int32-cephalon-engine-runtime-runtimefailureinfo"></a>

##### `RuntimeStatusSnapshot`

```csharp
RuntimeStatusSnapshot(RuntimeStatus Status, DateTimeOffset? InitializedAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? StoppingAtUtc, DateTimeOffset? StoppedAtUtc, int RestartCount, RuntimeFailureInfo LastFailure)
```

Captures the current runtime lifecycle state in a serialization-friendly form.

Parameters:
- `Status`: The current lifecycle status.
- `InitializedAtUtc`: The UTC timestamp when initialization completed, if it has completed.
- `StartedAtUtc`: The UTC timestamp when startup completed, if it has completed.
- `StoppingAtUtc`: The UTC timestamp when shutdown most recently entered the stopping phase, if any.
- `StoppedAtUtc`: The UTC timestamp when the runtime last transitioned to a stopped state, if any.
- `RestartCount`: The number of completed manual restarts.
- `LastFailure`: The last captured failure, if the runtime has faulted.

#### Properties

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-initializedatutc"></a>

##### `InitializedAtUtc`

```csharp
DateTimeOffset? InitializedAtUtc { get; set; }
```

The UTC timestamp when initialization completed, if it has completed.

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-lastfailure"></a>

##### `LastFailure`

```csharp
RuntimeFailureInfo LastFailure { get; set; }
```

The last captured failure, if the runtime has faulted.

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-restartcount"></a>

##### `RestartCount`

```csharp
int RestartCount { get; set; }
```

The number of completed manual restarts.

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-startedatutc"></a>

##### `StartedAtUtc`

```csharp
DateTimeOffset? StartedAtUtc { get; set; }
```

The UTC timestamp when startup completed, if it has completed.

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-status"></a>

##### `Status`

```csharp
RuntimeStatus Status { get; set; }
```

The current lifecycle status.

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-stoppedatutc"></a>

##### `StoppedAtUtc`

```csharp
DateTimeOffset? StoppedAtUtc { get; set; }
```

The UTC timestamp when the runtime last transitioned to a stopped state, if any.

<a id="member-p-cephalon-engine-runtime-runtimestatussnapshot-stoppingatutc"></a>

##### `StoppingAtUtc`

```csharp
DateTimeOffset? StoppingAtUtc { get; set; }
```

The UTC timestamp when shutdown most recently entered the stopping phase, if any.

<a id="namespace-cephalon-engine-technologies"></a>

## Namespace Cephalon.Engine.Technologies

<a id="type-cephalon-engine-technologies-builtintechnologies"></a>

### `BuiltInTechnologies`

Provides the built-in technology descriptors used by Cephalon app profiles.

#### Declaration
```csharp
public static class BuiltInTechnologies
```

#### Properties

<a id="member-p-cephalon-engine-technologies-builtintechnologies-agenticworkloads"></a>

##### `AgenticWorkloads`

```csharp
TechnologyDescriptor AgenticWorkloads { get; }
```

Gets the built-in agentic-workloads technology profile.

<a id="member-p-cephalon-engine-technologies-builtintechnologies-all"></a>

##### `All`

```csharp
IReadOnlyList<TechnologyDescriptor> All { get; }
```

Gets all built-in technology descriptors.

<a id="member-p-cephalon-engine-technologies-builtintechnologies-edgenativedelivery"></a>

##### `EdgeNativeDelivery`

```csharp
TechnologyDescriptor EdgeNativeDelivery { get; }
```

Gets the built-in edge-native-delivery technology profile.

<a id="member-p-cephalon-engine-technologies-builtintechnologies-eventdrivenintegration"></a>

##### `EventDrivenIntegration`

```csharp
TechnologyDescriptor EventDrivenIntegration { get; }
```

Gets the built-in event-driven-integration technology profile.

<a id="member-p-cephalon-engine-technologies-builtintechnologies-knowledgeretrieval"></a>

##### `KnowledgeRetrieval`

```csharp
TechnologyDescriptor KnowledgeRetrieval { get; }
```

Gets the built-in knowledge-retrieval technology profile.

<a id="member-p-cephalon-engine-technologies-builtintechnologies-realtimeexperience"></a>

##### `RealtimeExperience`

```csharp
TechnologyDescriptor RealtimeExperience { get; }
```

Gets the built-in realtime-experience technology profile.

#### Methods

<a id="member-m-cephalon-engine-technologies-builtintechnologies-resolve-system-string"></a>

##### `Resolve`

```csharp
TechnologyDescriptor Resolve(string value)
```

Resolves a technology identifier, display name, or alias.

Returns: The resolved technology descriptor.

Parameters:
- `value`: The technology identifier, display name, or alias to resolve.

<a id="member-m-cephalon-engine-technologies-builtintechnologies-tryresolve-system-string-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TryResolve`

```csharp
bool TryResolve(string value, out TechnologyDescriptor technology)
```

Attempts to resolve a technology identifier, display name, or alias.

Returns: `true` when the technology was resolved; otherwise, `false`.

Parameters:
- `value`: The technology identifier, display name, or alias to resolve.
- `technology`: The resolved technology descriptor when the lookup succeeds.

<a id="type-cephalon-engine-technologies-technologycatalogsnapshot"></a>

### `TechnologyCatalogSnapshot`

Captures the built-in and registered technology catalog visible to the runtime.

#### Declaration
```csharp
public sealed class TechnologyCatalogSnapshot
```

#### Constructors

<a id="member-m-cephalon-engine-technologies-technologycatalogsnapshot-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologydescriptor"></a>

##### `TechnologyCatalogSnapshot`

```csharp
TechnologyCatalogSnapshot(IReadOnlyList<TechnologyDescriptor> technologies)
```

Initializes a new instance of the `TechnologyCatalogSnapshot` class.

Parameters:
- `technologies`: The technologies visible in the catalog.

#### Properties

<a id="member-p-cephalon-engine-technologies-technologycatalogsnapshot-technologies"></a>

##### `Technologies`

```csharp
IReadOnlyList<TechnologyDescriptor> Technologies { get; }
```

Gets the technologies visible in the catalog.

<a id="type-cephalon-engine-technologies-technologyruntimecatalogsnapshot"></a>

### `TechnologyRuntimeCatalogSnapshot`

Provides a lookup-friendly view of the active technology runtime surfaces.

#### Declaration
```csharp
public sealed class TechnologyRuntimeCatalogSnapshot
```

#### Constructors

<a id="member-m-cephalon-engine-technologies-technologyruntimecatalogsnapshot-ctor-system-collections-generic-ireadonlylist-cephalon-abstractions-technologies-technologyruntimesurface"></a>

##### `TechnologyRuntimeCatalogSnapshot`

```csharp
TechnologyRuntimeCatalogSnapshot(IReadOnlyList<TechnologyRuntimeSurface> surfaces)
```

Initializes a new instance of the `TechnologyRuntimeCatalogSnapshot` class.

Parameters:
- `surfaces`: The active technology runtime surfaces.

#### Properties

<a id="member-p-cephalon-engine-technologies-technologyruntimecatalogsnapshot-surfaces"></a>

##### `Surfaces`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> Surfaces { get; }
```

Gets the active technology runtime surfaces.

#### Methods

<a id="member-m-cephalon-engine-technologies-technologyruntimecatalogsnapshot-getbytechnology-system-string"></a>

##### `GetByTechnology`

```csharp
IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId)
```

Gets the runtime surfaces for a specific technology.

Returns: The runtime surfaces registered for the specified technology.

Parameters:
- `technologyId`: The technology identifier to resolve.

<a id="namespace-cephalon-engine-transports"></a>

## Namespace Cephalon.Engine.Transports

<a id="type-cephalon-engine-transports-builtintransports"></a>

### `BuiltInTransports`

Provides the built-in transport descriptors used by Cephalon app profiles.

#### Declaration
```csharp
public static class BuiltInTransports
```

#### Properties

<a id="member-p-cephalon-engine-transports-builtintransports-all"></a>

##### `All`

```csharp
IReadOnlyList<TransportDescriptor> All { get; }
```

Gets all built-in transport descriptors.

<a id="member-p-cephalon-engine-transports-builtintransports-graphql"></a>

##### `GraphQL`

```csharp
TransportDescriptor GraphQL { get; }
```

Gets the built-in GraphQL transport descriptor.

<a id="member-p-cephalon-engine-transports-builtintransports-grpc"></a>

##### `Grpc`

```csharp
TransportDescriptor Grpc { get; }
```

Gets the built-in gRPC transport descriptor.

<a id="member-p-cephalon-engine-transports-builtintransports-jsonrpc"></a>

##### `JsonRpc`

```csharp
TransportDescriptor JsonRpc { get; }
```

Gets the built-in JSON-RPC transport descriptor.

<a id="member-p-cephalon-engine-transports-builtintransports-restapi"></a>

##### `RestApi`

```csharp
TransportDescriptor RestApi { get; }
```

Gets the built-in REST transport descriptor.

<a id="member-p-cephalon-engine-transports-builtintransports-serversentevents"></a>

##### `ServerSentEvents`

```csharp
TransportDescriptor ServerSentEvents { get; }
```

Gets the built-in server-sent-events transport descriptor.

<a id="member-p-cephalon-engine-transports-builtintransports-websocket"></a>

##### `WebSocket`

```csharp
TransportDescriptor WebSocket { get; }
```

Gets the built-in WebSocket transport descriptor.

#### Methods

<a id="member-m-cephalon-engine-transports-builtintransports-resolve-system-string"></a>

##### `Resolve`

```csharp
TransportDescriptor Resolve(string value)
```

Resolves a transport identifier, display name, or alias.

Returns: The resolved transport descriptor.

Parameters:
- `value`: The transport identifier, display name, or alias to resolve.

<a id="member-m-cephalon-engine-transports-builtintransports-tryresolve-system-string-cephalon-abstractions-transports-transportdescriptor"></a>

##### `TryResolve`

```csharp
bool TryResolve(string value, out TransportDescriptor transport)
```

Attempts to resolve a transport identifier, display name, or alias.

Returns: `true` when the transport was resolved; otherwise, `false`.

Parameters:
- `value`: The transport identifier, display name, or alias to resolve.
- `transport`: The resolved transport descriptor when the lookup succeeds.

<a id="namespace-cephalon-engine-trust"></a>

## Namespace Cephalon.Engine.Trust

<a id="type-cephalon-engine-trust-capabilitypolicydecision"></a>

### `CapabilityPolicyDecision`

Describes the evaluated trust decision for a single capability.

#### Declaration
```csharp
public sealed class CapabilityPolicyDecision
```

#### Constructors

<a id="member-m-cephalon-engine-trust-capabilitypolicydecision-ctor-system-string-system-string-system-string-cephalon-abstractions-capabilities-capabilityaccess-system-boolean-system-boolean-system-string"></a>

##### `CapabilityPolicyDecision`

```csharp
CapabilityPolicyDecision(string CapabilityKey, string SourceModuleId, string SourcePackageId, CapabilityAccess Access, bool SourceTrusted, bool IsAllowed, string Reason)
```

Describes the evaluated trust decision for a single capability.

Parameters:
- `CapabilityKey`: The capability key that was evaluated.
- `SourceModuleId`: The module that contributed the capability.
- `SourcePackageId`: The package that contributed the capability when one is known.
- `Access`: The effective access mode resolved from policy.
- `SourceTrusted`: Whether the contributing source is trusted.
- `IsAllowed`: Whether the capability is allowed under the resolved policy.
- `Reason`: The human-readable reason for the decision.

#### Properties

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-access"></a>

##### `Access`

```csharp
CapabilityAccess Access { get; set; }
```

The effective access mode resolved from policy.

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-capabilitykey"></a>

##### `CapabilityKey`

```csharp
string CapabilityKey { get; set; }
```

The capability key that was evaluated.

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-isallowed"></a>

##### `IsAllowed`

```csharp
bool IsAllowed { get; set; }
```

Whether the capability is allowed under the resolved policy.

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

The human-readable reason for the decision.

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

The module that contributed the capability.

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-sourcepackageid"></a>

##### `SourcePackageId`

```csharp
string SourcePackageId { get; set; }
```

The package that contributed the capability when one is known.

<a id="member-p-cephalon-engine-trust-capabilitypolicydecision-sourcetrusted"></a>

##### `SourceTrusted`

```csharp
bool SourceTrusted { get; set; }
```

Whether the contributing source is trusted.

<a id="type-cephalon-engine-trust-capabilitypolicyevaluator"></a>

### `CapabilityPolicyEvaluator`

Evaluates capability and package trust decisions against the current trust policy snapshot.

#### Declaration
```csharp
public sealed class CapabilityPolicyEvaluator
```

#### Constructors

<a id="member-m-cephalon-engine-trust-capabilitypolicyevaluator-ctor-cephalon-engine-trust-trustsnapshot"></a>

##### `CapabilityPolicyEvaluator`

```csharp
CapabilityPolicyEvaluator(TrustSnapshot snapshot)
```

Initializes a new instance of the `CapabilityPolicyEvaluator` class.

Parameters:
- `snapshot`: The trust snapshot to evaluate against.

#### Properties

<a id="member-p-cephalon-engine-trust-capabilitypolicyevaluator-snapshot"></a>

##### `Snapshot`

```csharp
TrustSnapshot Snapshot { get; }
```

Gets the trust snapshot being evaluated.

#### Methods

<a id="member-m-cephalon-engine-trust-capabilitypolicyevaluator-createsnapshot-cephalon-engine-configuration-trustpolicy-system-collections-generic-ireadonlylist-cephalon-engine-manifest-packagemanifest-system-collections-generic-ireadonlylist-cephalon-engine-manifest-modulemanifest-system-collections-generic-ireadonlylist-cephalon-engine-manifest-capabilitymanifest"></a>

##### `CreateSnapshot`

```csharp
TrustSnapshot CreateSnapshot(TrustPolicy policy, IReadOnlyList<PackageManifest> packages, IReadOnlyList<ModuleManifest> modules, IReadOnlyList<CapabilityManifest> capabilities)
```

Creates a trust snapshot from the supplied policy, packages, modules, and capabilities.

Returns: A computed trust snapshot.

Parameters:
- `policy`: The trust policy to apply.
- `packages`: The package manifests visible to the runtime.
- `modules`: The module manifests visible to the runtime.
- `capabilities`: The capability manifests visible to the runtime.

<a id="member-m-cephalon-engine-trust-capabilitypolicyevaluator-isallowed-system-string"></a>

##### `IsAllowed`

```csharp
bool IsAllowed(string capabilityKey)
```

Determines whether a capability is allowed under the current trust policy.

Returns: `true` when the capability is allowed; otherwise, `false`.

Parameters:
- `capabilityKey`: The capability key to evaluate.

<a id="member-m-cephalon-engine-trust-capabilitypolicyevaluator-trygetdecision-system-string-cephalon-engine-trust-capabilitypolicydecision"></a>

##### `TryGetDecision`

```csharp
bool TryGetDecision(string capabilityKey, out CapabilityPolicyDecision decision)
```

Attempts to resolve the trust decision for a capability.

Returns: `true` when a decision was produced.

Parameters:
- `capabilityKey`: The capability key to evaluate.
- `decision`: The resolved trust decision.

<a id="type-cephalon-engine-trust-packagesignaturetrustdecision"></a>

### `PackageSignatureTrustDecision`

Describes the trust and verification outcome for a single package signature.

#### Declaration
```csharp
public sealed class PackageSignatureTrustDecision
```

#### Constructors

<a id="member-m-cephalon-engine-trust-packagesignaturetrustdecision-ctor-system-string-system-string-system-string-system-string-system-string-system-boolean-system-string"></a>

##### `PackageSignatureTrustDecision`

```csharp
PackageSignatureTrustDecision(string Signer, string KeyId, string Fingerprint, string VerificationSource, string CertificateThumbprint, bool IsVerified, string Reason)
```

Describes the trust and verification outcome for a single package signature.

Parameters:
- `Signer`: The declared signer identity, when available.
- `KeyId`: The declared signing-key identifier, when available.
- `Fingerprint`: The declared signer fingerprint, when available.
- `VerificationSource`: The trust source that verified the signature, when available.
- `CertificateThumbprint`: The signing certificate thumbprint used during verification, when certificate-backed trust was used.
- `IsVerified`: Whether this signature verified successfully against a trusted public key or trusted signing certificate.
- `Reason`: The verification outcome or failure reason for this signature.

#### Properties

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-certificatethumbprint"></a>

##### `CertificateThumbprint`

```csharp
string CertificateThumbprint { get; set; }
```

The signing certificate thumbprint used during verification, when certificate-backed trust was used.

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-fingerprint"></a>

##### `Fingerprint`

```csharp
string Fingerprint { get; set; }
```

The declared signer fingerprint, when available.

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-isverified"></a>

##### `IsVerified`

```csharp
bool IsVerified { get; set; }
```

Whether this signature verified successfully against a trusted public key or trusted signing certificate.

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-keyid"></a>

##### `KeyId`

```csharp
string KeyId { get; set; }
```

The declared signing-key identifier, when available.

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

The verification outcome or failure reason for this signature.

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-signer"></a>

##### `Signer`

```csharp
string Signer { get; set; }
```

The declared signer identity, when available.

<a id="member-p-cephalon-engine-trust-packagesignaturetrustdecision-verificationsource"></a>

##### `VerificationSource`

```csharp
string VerificationSource { get; set; }
```

The trust source that verified the signature, when available.

<a id="type-cephalon-engine-trust-packagetrustdecision"></a>

### `PackageTrustDecision`

Describes the trust outcome for a package after package metadata, signature verification, and host trust rules have been evaluated.

#### Declaration
```csharp
public sealed class PackageTrustDecision
```

#### Constructors

<a id="member-m-cephalon-engine-trust-packagetrustdecision-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-cephalon-engine-trust-packagesignaturetrustdecision-system-boolean-system-string-system-boolean-system-string"></a>

##### `PackageTrustDecision`

```csharp
PackageTrustDecision(string PackageId, string AssemblyName, string Path, string PublisherId, string SignatureKeyId, string SignatureFingerprint, string SignatureCertificateThumbprint, IReadOnlyList<PackageSignatureTrustDecision> Signatures, bool IsSignatureVerified, string SignatureVerificationReason, bool IsTrusted, string Reason)
```

Describes the trust outcome for a package after package metadata, signature verification, and host trust rules have been evaluated.

Parameters:
- `PackageId`: The stable package identifier.
- `AssemblyName`: The resolved assembly name for the package.
- `Path`: The resolved assembly path used for the package load.
- `PublisherId`: The declared publisher identifier, when available.
- `SignatureKeyId`: The primary signature key identifier, when available.
- `SignatureFingerprint`: The primary signature fingerprint, when available.
- `SignatureCertificateThumbprint`: The primary signing certificate thumbprint used during verification, when certificate-backed trust was used.
- `Signatures`: The per-signer trust and verification details declared by the package.
- `IsSignatureVerified`: Whether at least one declared signature verified successfully.
- `SignatureVerificationReason`: The aggregate signature verification outcome summary.
- `IsTrusted`: Whether the package is trusted by the active runtime trust policy.
- `Reason`: The reason the package was trusted or rejected.

#### Properties

<a id="member-p-cephalon-engine-trust-packagetrustdecision-assemblyname"></a>

##### `AssemblyName`

```csharp
string AssemblyName { get; set; }
```

The resolved assembly name for the package.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-issignatureverified"></a>

##### `IsSignatureVerified`

```csharp
bool IsSignatureVerified { get; set; }
```

Whether at least one declared signature verified successfully.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-istrusted"></a>

##### `IsTrusted`

```csharp
bool IsTrusted { get; set; }
```

Whether the package is trusted by the active runtime trust policy.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-packageid"></a>

##### `PackageId`

```csharp
string PackageId { get; set; }
```

The stable package identifier.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-path"></a>

##### `Path`

```csharp
string Path { get; set; }
```

The resolved assembly path used for the package load.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-publisherid"></a>

##### `PublisherId`

```csharp
string PublisherId { get; set; }
```

The declared publisher identifier, when available.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

The reason the package was trusted or rejected.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-signaturecertificatethumbprint"></a>

##### `SignatureCertificateThumbprint`

```csharp
string SignatureCertificateThumbprint { get; set; }
```

The primary signing certificate thumbprint used during verification, when certificate-backed trust was used.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-signaturefingerprint"></a>

##### `SignatureFingerprint`

```csharp
string SignatureFingerprint { get; set; }
```

The primary signature fingerprint, when available.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-signaturekeyid"></a>

##### `SignatureKeyId`

```csharp
string SignatureKeyId { get; set; }
```

The primary signature key identifier, when available.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-signatures"></a>

##### `Signatures`

```csharp
IReadOnlyList<PackageSignatureTrustDecision> Signatures { get; set; }
```

The per-signer trust and verification details declared by the package.

<a id="member-p-cephalon-engine-trust-packagetrustdecision-signatureverificationreason"></a>

##### `SignatureVerificationReason`

```csharp
string SignatureVerificationReason { get; set; }
```

The aggregate signature verification outcome summary.

<a id="type-cephalon-engine-trust-trustsnapshot"></a>

### `TrustSnapshot`

Captures the effective trust policy together with evaluated package and capability decisions.

#### Declaration
```csharp
public sealed class TrustSnapshot
```

#### Constructors

<a id="member-m-cephalon-engine-trust-trustsnapshot-ctor-cephalon-engine-configuration-trustpolicy-system-collections-generic-ireadonlylist-cephalon-engine-trust-packagetrustdecision-system-collections-generic-ireadonlylist-cephalon-engine-trust-capabilitypolicydecision"></a>

##### `TrustSnapshot`

```csharp
TrustSnapshot(TrustPolicy Policy, IReadOnlyList<PackageTrustDecision> Packages, IReadOnlyList<CapabilityPolicyDecision> Capabilities)
```

Captures the effective trust policy together with evaluated package and capability decisions.

Parameters:
- `Policy`: The policy that produced the trust decisions.
- `Packages`: The evaluated package trust decisions.
- `Capabilities`: The evaluated capability trust decisions.

#### Properties

<a id="member-p-cephalon-engine-trust-trustsnapshot-capabilities"></a>

##### `Capabilities`

```csharp
IReadOnlyList<CapabilityPolicyDecision> Capabilities { get; set; }
```

The evaluated capability trust decisions.

<a id="member-p-cephalon-engine-trust-trustsnapshot-packages"></a>

##### `Packages`

```csharp
IReadOnlyList<PackageTrustDecision> Packages { get; set; }
```

The evaluated package trust decisions.

<a id="member-p-cephalon-engine-trust-trustsnapshot-policy"></a>

##### `Policy`

```csharp
TrustPolicy Policy { get; set; }
```

The policy that produced the trust decisions.
