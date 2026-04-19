using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Audit;
using Cephalon.Engine.Authorization;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Data;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Execution;
using Cephalon.Engine.Features;
using Cephalon.Engine.Localization;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Composition.Packages;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Technologies;
using Cephalon.Engine.Trust;
using Cephalon.Engine.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Abstractions.Localization;

namespace Cephalon.Engine.Composition;

/// <summary>
/// Builds a Cephalon runtime by composing the application model, module set, and policy inputs
/// into a single <see cref="EngineRuntime" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EngineBuilder" /> is the main code-first entry point for configuring Cephalon.
/// It can be driven from configuration, composed in code, or use both approaches together.
/// </para>
/// <para>
/// At build time the builder resolves discovery inputs, package loading, application profile
/// selection, localization, trust policy, failure policy, and capability filtering into a
/// deterministic runtime snapshot.
/// </para>
/// </remarks>
public sealed class EngineBuilder
{
    private readonly List<IModule> modules = [];
    private readonly List<ModulePackageReference> packages = [];
    private readonly List<ModulePackageDirectory> packageDirectories = [];
    private readonly List<CellBoundaryDescriptor> cellBoundaries = [];
    private readonly List<BackendForFrontendClientBindingDescriptor> backendForFrontendBindings = [];
    private readonly List<StranglerFigRouteDescriptor> stranglerFigRoutes = [];
    private readonly List<FeatureFlagDescriptor> featureFlags = [];
    private readonly AppProfileBuilder appProfileBuilder = new();
    private EngineOptions engineOptions = EngineOptions.Empty;
    private LocalizationSettings localizationSettings = LocalizationSettings.Empty;
    private FailurePolicy failurePolicy = FailurePolicy.Default;
    private TrustPolicy trustPolicy = TrustPolicy.Default;
    private PackagePolicy packagePolicy = PackagePolicy.Default;
    private MigrationSettings migrationSettings = MigrationSettings.Empty;
    private BackendForFrontendSettings backendForFrontendSettings = BackendForFrontendSettings.Empty;
    private FeatureSettings featureSettings = FeatureSettings.Empty;

    /// <summary>
    /// Creates a new builder over the supplied service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection that receives runtime services, catalogs, policies, and
    /// module- or technology-provided registrations.
    /// </param>
    public EngineBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Gets the service collection that the builder mutates while composing the engine.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Reads engine settings from configuration and merges them into the current builder state.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">
    /// The configuration path that should be interpreted as the engine settings section.
    /// The default value is <see cref="EngineSettings.SectionName" />.
    /// </param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>
    /// This method is the preferred entry point when a host should stay configuration-driven.
    /// It binds the selected blueprint, transports, technologies, options, localization, trust
    /// policy, failure policy, and discovery settings before any code-level overrides are applied.
    /// </remarks>
    public EngineBuilder UseConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return UseSettings(EngineSettings.FromConfiguration(configuration, sectionPath));
    }

    /// <summary>
    /// Applies a preconstructed <see cref="EngineSettings" /> instance to the builder.
    /// </summary>
    /// <param name="settings">The settings object to merge into the current builder state.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>
    /// Discovery settings are resolved eagerly, which means referenced assemblies or package
    /// manifests are validated before the runtime is built.
    /// </remarks>
    public EngineBuilder UseSettings(EngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.HasValues)
        {
            return this;
        }

        AppProfileFactory.ApplySettings(appProfileBuilder, settings);

        if (settings.Discovery.HasValues)
        {
            AddModulesFromAssemblies(ModuleDiscovery.ResolveAssemblies(settings.Discovery.Assemblies));
            AddPackages(settings.Discovery.Packages);
            AddPackageDirectories(settings.Discovery.PackageDirectories);
        }

        UseOptions(settings.Options);
        UseLocalization(settings.Localization);
        UseFailurePolicy(settings.FailurePolicy);
        UseTrustPolicy(settings.TrustPolicy);
        UsePackagePolicy(settings.PackagePolicy);
        UseMigrationSettings(settings.Migration);
        UseBackendForFrontendSettings(settings.BackendForFrontend);
        UseFeatureSettings(settings.Features);

        return this;
    }

    /// <summary>
    /// Selects the base application blueprint that should shape the runtime.
    /// </summary>
    /// <param name="blueprint">The blueprint descriptor to activate.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseBlueprint(AppBlueprint blueprint)
    {
        appProfileBuilder.UseBlueprint(blueprint);
        return this;
    }

    /// <summary>
    /// Adds an application or design pattern to the current app profile.
    /// </summary>
    /// <param name="pattern">The pattern descriptor to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPattern(PatternDescriptor pattern)
    {
        appProfileBuilder.AddPattern(pattern);
        return this;
    }

    /// <summary>
    /// Adds a transport to the current app profile selection.
    /// </summary>
    /// <param name="transport">The transport descriptor to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddTransport(TransportDescriptor transport)
    {
        appProfileBuilder.AddTransport(transport);
        return this;
    }

    /// <summary>
    /// Selects a technology profile for the current app profile.
    /// </summary>
    /// <param name="technology">The technology descriptor to activate.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddTechnology(TechnologyDescriptor technology)
    {
        appProfileBuilder.AddTechnology(technology);
        return this;
    }

    /// <summary>
    /// Adds a cell boundary to the current runtime composition.
    /// </summary>
    /// <param name="cellBoundary">The cell-boundary descriptor to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddCellBoundary(CellBoundaryDescriptor cellBoundary)
    {
        ArgumentNullException.ThrowIfNull(cellBoundary);

        cellBoundaries.Add(cellBoundary);
        return this;
    }

    /// <summary>
    /// Adds multiple cell boundaries to the current runtime composition.
    /// </summary>
    /// <param name="cellBoundaries">The cell-boundary descriptors to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddCellBoundaries(IEnumerable<CellBoundaryDescriptor> cellBoundaries)
    {
        ArgumentNullException.ThrowIfNull(cellBoundaries);

        foreach (var cellBoundary in cellBoundaries)
        {
            AddCellBoundary(cellBoundary);
        }

        return this;
    }

    /// <summary>
    /// Adds a backend-for-frontend client binding to the current runtime composition.
    /// </summary>
    /// <param name="binding">The client binding descriptor to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddBackendForFrontendClientBinding(BackendForFrontendClientBindingDescriptor binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        backendForFrontendBindings.Add(binding);
        return this;
    }

    /// <summary>
    /// Adds multiple backend-for-frontend client bindings to the current runtime composition.
    /// </summary>
    /// <param name="bindings">The client binding descriptors to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddBackendForFrontendClientBindings(IEnumerable<BackendForFrontendClientBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        foreach (var binding in bindings)
        {
            AddBackendForFrontendClientBinding(binding);
        }

        return this;
    }

    /// <summary>
    /// Adds a strangler-fig route to the current runtime composition.
    /// </summary>
    /// <param name="route">The route descriptor to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddStranglerFigRoute(StranglerFigRouteDescriptor route)
    {
        ArgumentNullException.ThrowIfNull(route);

        stranglerFigRoutes.Add(route);
        return this;
    }

    /// <summary>
    /// Adds multiple strangler-fig routes to the current runtime composition.
    /// </summary>
    /// <param name="routes">The route descriptors to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddStranglerFigRoutes(IEnumerable<StranglerFigRouteDescriptor> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        foreach (var route in routes)
        {
            AddStranglerFigRoute(route);
        }

        return this;
    }

    /// <summary>
    /// Adds a feature flag to the current runtime composition.
    /// </summary>
    /// <param name="featureFlag">The feature-flag descriptor to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddFeatureFlag(FeatureFlagDescriptor featureFlag)
    {
        ArgumentNullException.ThrowIfNull(featureFlag);

        featureFlags.Add(featureFlag);
        return this;
    }

    /// <summary>
    /// Adds multiple feature flags to the current runtime composition.
    /// </summary>
    /// <param name="featureFlags">The feature-flag descriptors to add.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddFeatureFlags(IEnumerable<FeatureFlagDescriptor> featureFlags)
    {
        ArgumentNullException.ThrowIfNull(featureFlags);

        foreach (var featureFlag in featureFlags)
        {
            AddFeatureFlag(featureFlag);
        }

        return this;
    }

    /// <summary>
    /// Registers one feature-flag provider in the engine service collection.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation type.</typeparam>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddFeatureFlagProvider<TProvider>()
        where TProvider : class, IFeatureFlagProvider
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IFeatureFlagProvider, TProvider>());
        return this;
    }

    /// <summary>
    /// Registers one feature-flag provider instance in the engine service collection.
    /// </summary>
    /// <param name="provider">The provider instance to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddFeatureFlagProvider(IFeatureFlagProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IFeatureFlagProvider>(provider));
        return this;
    }

    /// <summary>
    /// Registers multiple feature-flag provider instances in the engine service collection.
    /// </summary>
    /// <param name="providers">The provider instances to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddFeatureFlagProviders(IEnumerable<IFeatureFlagProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        foreach (var provider in providers)
        {
            AddFeatureFlagProvider(provider);
        }

        return this;
    }

    /// <summary>
    /// Registers a technology descriptor in the available catalog without implicitly selecting it.
    /// </summary>
    /// <param name="technology">The technology descriptor to register.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>
    /// This is useful when a project wants to extend the catalog and let configuration decide
    /// whether the technology is active.
    /// </remarks>
    public EngineBuilder RegisterTechnology(TechnologyDescriptor technology)
    {
        appProfileBuilder.RegisterTechnology(technology);
        return this;
    }

    /// <summary>
    /// Merges engine option overrides such as module enablement and capability toggles.
    /// </summary>
    /// <param name="options">The option overrides to merge.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseOptions(EngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        engineOptions = engineOptions.Merge(options);
        return this;
    }

    /// <summary>
    /// Merges localization settings into the current builder state.
    /// </summary>
    /// <param name="settings">
    /// The localization settings to merge, including default culture, supported cultures,
    /// and per-culture resource overrides.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseLocalization(LocalizationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        localizationSettings = localizationSettings.Merge(settings);
        return this;
    }

    /// <summary>
    /// Replaces the failure policy used by the runtime lifecycle state machine.
    /// </summary>
    /// <param name="policy">The lifecycle failure policy to apply.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseFailurePolicy(FailurePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        failurePolicy = policy;
        return this;
    }

    /// <summary>
    /// Merges trust and capability-governance settings into the builder.
    /// </summary>
    /// <param name="policy">The trust policy to apply.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseTrustPolicy(TrustPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        trustPolicy = trustPolicy.Merge(policy);
        return this;
    }

    /// <summary>
    /// Replaces the package-governance policy used when loading independently shipped module packages.
    /// </summary>
    /// <param name="policy">The package policy to apply.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UsePackagePolicy(PackagePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        packagePolicy = policy;
        return this;
    }

    /// <summary>
    /// Merges feature-flag settings into the current builder state.
    /// </summary>
    /// <param name="settings">The feature-flag settings to merge.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseFeatureSettings(FeatureSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.HasValues)
        {
            return this;
        }

        featureSettings = new FeatureSettings(
            featureSettings.Flags
                .Concat(settings.Flags)
                .ToArray());
        return this;
    }

    /// <summary>
    /// Replaces the migration-policy settings used by the runtime migration catalogs.
    /// </summary>
    /// <param name="migration">The migration settings to apply.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseMigrationSettings(MigrationSettings migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        migrationSettings = migration;
        return this;
    }

    /// <summary>
    /// Replaces the backend-for-frontend settings used by the runtime client-binding catalog.
    /// </summary>
    /// <param name="settings">The backend-for-frontend settings to apply.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder UseBackendForFrontendSettings(BackendForFrontendSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        backendForFrontendSettings = settings;
        return this;
    }

    /// <summary>
    /// Adds resource overrides for a specific culture without replacing the existing localization contract.
    /// </summary>
    /// <param name="culture">The culture name to extend, such as <c>en</c> or <c>th-TH</c>.</param>
    /// <param name="resources">The key/value resource set to merge for that culture.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>
    /// This method is typically used by hosts that want to layer project-owned localization on top of
    /// engine defaults and any package-provided language packs.
    /// </remarks>
    public EngineBuilder AddLanguageResources(
        string culture,
        IReadOnlyDictionary<string, string> resources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentNullException.ThrowIfNull(resources);

        return UseLocalization(new LocalizationSettings(
            supportedCultures: [culture],
            resources: new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                [culture.Trim()] = resources
            }));
    }

    /// <summary>
    /// Adds a module by type using its parameterless constructor.
    /// </summary>
    /// <typeparam name="TModule">The concrete module type to instantiate and register.</typeparam>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddModule<TModule>()
        where TModule : class, IModule, new()
    {
        return AddModule(new TModule());
    }

    /// <summary>
    /// Adds a concrete module instance to the runtime composition graph.
    /// </summary>
    /// <param name="module">The module instance to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddModule(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        modules.Add(module);
        return this;
    }

    /// <summary>
    /// Adds a package reference that points directly to a module assembly.
    /// </summary>
    /// <param name="path">The path to the package assembly.</param>
    /// <param name="id">
    /// An optional stable package identifier. When omitted, the identifier is derived from the reference.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackageAssembly(string path, string? id = null)
    {
        return AddPackage(new ModulePackageReference(path, id));
    }

    /// <summary>
    /// Adds an explicit package reference to the builder.
    /// </summary>
    /// <param name="package">The package reference to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackage(ModulePackageReference package)
    {
        ArgumentNullException.ThrowIfNull(package);

        packages.Add(package);
        return this;
    }

    /// <summary>
    /// Adds a package by its <c>cephalon.package.json</c> manifest file.
    /// </summary>
    /// <param name="manifestPath">The path to the package manifest file.</param>
    /// <param name="id">
    /// An optional stable package identifier. When omitted, the identifier is resolved from the manifest.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackageManifest(string manifestPath, string? id = null)
    {
        return AddPackage(ModulePackageReference.FromManifest(manifestPath, id));
    }

    /// <summary>
    /// Adds a directory that should be scanned for package manifests.
    /// </summary>
    /// <param name="path">The directory path to scan.</param>
    /// <param name="manifestFileName">
    /// The manifest file name to search for. When omitted, the engine uses the default package manifest name.
    /// </param>
    /// <param name="includeSubdirectories">
    /// <see langword="true" /> to recurse into child directories; otherwise only the top-level directory is scanned.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackageDirectory(
        string path,
        string? manifestFileName = null,
        bool includeSubdirectories = true)
    {
        return AddPackageDirectory(new ModulePackageDirectory(path, manifestFileName, includeSubdirectories));
    }

    /// <summary>
    /// Adds a package-directory discovery rule to the builder.
    /// </summary>
    /// <param name="directory">The directory discovery descriptor to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackageDirectory(ModulePackageDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        packageDirectories.Add(directory);
        return this;
    }

    /// <summary>
    /// Adds multiple package-directory discovery rules.
    /// </summary>
    /// <param name="directories">The package directories to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackageDirectories(IEnumerable<ModulePackageDirectory> directories)
    {
        ArgumentNullException.ThrowIfNull(directories);

        foreach (var directory in directories)
        {
            AddPackageDirectory(directory);
        }

        return this;
    }

    /// <summary>
    /// Adds multiple explicit package references.
    /// </summary>
    /// <param name="packages">The package references to register.</param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddPackages(IEnumerable<ModulePackageReference> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        foreach (var package in packages)
        {
            AddPackage(package);
        }

        return this;
    }

    /// <summary>
    /// Discovers and adds modules from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="filter">
    /// An optional predicate that can opt specific candidate types in or out before they are instantiated.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddModulesFromAssembly(
        Assembly assembly,
        Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return AddModulesFromAssemblies([assembly], filter);
    }

    /// <summary>
    /// Discovers and adds modules from the assembly that contains <typeparamref name="TMarker" />.
    /// </summary>
    /// <typeparam name="TMarker">A type used only to identify the source assembly.</typeparam>
    /// <param name="filter">
    /// An optional predicate that can opt specific candidate types in or out before they are instantiated.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddModulesFromAssemblyContaining<TMarker>(Func<Type, bool>? filter = null)
    {
        return AddModulesFromAssembly(typeof(TMarker).Assembly, filter);
    }

    /// <summary>
    /// Discovers and adds modules from a sequence of assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for modules.</param>
    /// <param name="filter">
    /// An optional predicate that can opt specific candidate types in or out before they are instantiated.
    /// </param>
    /// <returns>The same builder instance.</returns>
    public EngineBuilder AddModulesFromAssemblies(
        IEnumerable<Assembly> assemblies,
        Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var module in ModuleDiscovery.DiscoverModules(assemblies, filter))
        {
            AddModule(module);
        }

        return this;
    }

    /// <summary>
    /// Materializes the configured engine into a runnable <see cref="EngineRuntime" />.
    /// </summary>
    /// <returns>The fully built runtime.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configured module graph, package inputs, trust policy, transport selection,
    /// or technology selection is invalid.
    /// </exception>
    /// <remarks>
    /// Build is the point where Cephalon becomes deterministic. The builder validates duplicate modules,
    /// resolves package-loaded assemblies, orders modules by dependency, applies capability and trust policy,
    /// creates the runtime manifest, and registers the runtime-facing catalogs used by hosts and tooling.
    /// </remarks>
    public EngineRuntime Build()
    {
        using var buildActivity = EngineDiagnostics.ActivitySource.StartActivity(
            EngineDiagnostics.BuildActivityName,
            ActivityKind.Internal);

        try
        {
            var loadedPackages = ModulePackageLoader.Load(packages, packageDirectories, packagePolicy, trustPolicy);
            var packageManifests = loadedPackages
                .Select(package => CreatePackageManifest(package, trustPolicy))
                .ToArray();
            var packageManifestByModuleType = new Dictionary<Type, PackageManifest>();
            for (var index = 0; index < loadedPackages.Length; index++)
            {
                var packageManifest = packageManifests[index];
                foreach (var module in loadedPackages[index].Modules)
                {
                    packageManifestByModuleType[module.GetType()] = packageManifest;
                }
            }
            var firstUntrustedPackage = trustPolicy.RequireTrustedPackages
                ? packageManifests.FirstOrDefault(static package => !package.IsTrusted)
                : null;
            if (firstUntrustedPackage is not null)
            {
                throw new InvalidOperationException(
                    $"Package '{firstUntrustedPackage.Id}' is not trusted by the current trust policy and cannot be loaded.");
            }

            var allModules = modules
                .Concat(loadedPackages.SelectMany(static package => package.Modules))
                .ToArray();
            var modulePackages = loadedPackages
                .SelectMany(static package => package.Modules.Select(module => new KeyValuePair<Type, LoadedPackage>(module.GetType(), package)))
                .ToDictionary();

            ValidateModuleIdentity(allModules);

            Services.AddSingleton(engineOptions);
            Services.AddSingleton(failurePolicy);
            Services.AddSingleton(trustPolicy);
            Services.AddSingleton(packagePolicy);
            Services.AddSingleton(migrationSettings);
            Services.AddSingleton(backendForFrontendSettings);
            Services.AddSingleton(featureSettings);

            var activeModules = ModuleActivation.ApplyOptions(allModules, engineOptions);
            var orderedModules = ModuleOrdering.Order(activeModules);
            var ownedBehaviorRegistrations = CollectOwnedBehaviorRegistrations(orderedModules);
            var modulesByType = orderedModules.ToDictionary(module => module.GetType());
            var executionGraphs = new List<ExecutionGraphDescriptor>();
            var hostedExecutions = new List<HostedExecutionDescriptor>();
            var projections = new List<ProjectionDescriptor>();
            var outboxes = new List<OutboxDescriptor>();
            var inboxes = new List<InboxDescriptor>();
            var auditStores = new List<AuditStoreDescriptor>();
            var authorizationPolicies = new List<AuthorizationPolicyDescriptor>();
            var activeCellBoundaries = new List<CellBoundaryDescriptor>(cellBoundaries);
            var activeBackendForFrontendBindings = new List<BackendForFrontendClientBindingDescriptor>(backendForFrontendBindings);
            var activeStranglerFigRoutes = new List<StranglerFigRouteDescriptor>(stranglerFigRoutes);
            var activeFeatureFlags = new List<FeatureFlagDescriptor>(featureFlags);
            var capabilities = new CapabilityManifestCollector();
            var technologyRegistry = new TechnologyRegistryAdapter(appProfileBuilder);
            foreach (var module in orderedModules.OfType<ITechnologyContributor>())
            {
                module.RegisterTechnologies(technologyRegistry);
            }

            foreach (var module in orderedModules.Where(static module => module is IExecutionGraphContributor))
            {
                ((IExecutionGraphContributor)module).RegisterExecutionGraphs(
                    new ExecutionGraphRegistryAdapter(module.Descriptor.Id, executionGraphs));
            }

            foreach (var module in orderedModules.Where(static module => module is IHostedExecutionContributor))
            {
                ((IHostedExecutionContributor)module).RegisterHostedExecutions(
                    new HostedExecutionRegistryAdapter(module.Descriptor.Id, hostedExecutions));
            }

            foreach (var module in orderedModules.Where(static module => module is IProjectionContributor))
            {
                ((IProjectionContributor)module).RegisterProjections(
                    new ProjectionRegistryAdapter(module.Descriptor.Id, projections));
            }

            foreach (var module in orderedModules.Where(static module => module is IOutboxContributor))
            {
                ((IOutboxContributor)module).RegisterOutboxes(
                    new OutboxRegistryAdapter(module.Descriptor.Id, outboxes));
            }

            foreach (var module in orderedModules.Where(static module => module is IInboxContributor))
            {
                ((IInboxContributor)module).RegisterInboxes(
                    new InboxRegistryAdapter(module.Descriptor.Id, inboxes));
            }

            foreach (var module in orderedModules.Where(static module => module is IAuditStoreContributor))
            {
                ((IAuditStoreContributor)module).RegisterAuditStores(
                    new AuditStoreRegistryAdapter(module.Descriptor.Id, auditStores));
            }

            foreach (var module in orderedModules.Where(static module => module is IAuthorizationPolicyContributor))
            {
                ((IAuthorizationPolicyContributor)module).RegisterPolicies(
                    new AuthorizationPolicyRegistryAdapter(module.Descriptor.Id, authorizationPolicies));
            }

            foreach (var module in orderedModules.Where(static module => module is ICellBoundaryContributor))
            {
                ((ICellBoundaryContributor)module).RegisterCellBoundaries(
                    new CellBoundaryRegistryAdapter(module.Descriptor.Id, activeCellBoundaries));
            }

            foreach (var module in orderedModules.Where(static module => module is IFeatureFlagContributor))
            {
                ((IFeatureFlagContributor)module).RegisterFeatureFlags(
                    new FeatureFlagRegistryAdapter(module.Descriptor.Id, activeFeatureFlags));
            }

            foreach (var module in orderedModules.Where(static module => module is IBackendForFrontendClientBindingContributor))
            {
                ((IBackendForFrontendClientBindingContributor)module).RegisterClientBindings(
                    new BackendForFrontendClientBindingRegistryAdapter(module.Descriptor.Id, activeBackendForFrontendBindings));
            }

            activeBackendForFrontendBindings.AddRange(
                backendForFrontendSettings.Bindings.Select(CreateBackendForFrontendClientBindingDescriptor));
            activeFeatureFlags.AddRange(
                featureSettings.Flags.Select(CreateFeatureFlagDescriptor));

            foreach (var module in orderedModules.Where(static module => module is IStranglerFigRouteContributor))
            {
                ((IStranglerFigRouteContributor)module).RegisterRoutes(
                    new StranglerFigRouteRegistryAdapter(module.Descriptor.Id, activeStranglerFigRoutes));
            }

            if (activeBackendForFrontendBindings.Count > 0)
            {
                appProfileBuilder.TryAddPattern(BuiltInPatterns.BackendForFrontendPattern);
            }

            if (activeStranglerFigRoutes.Count > 0)
            {
                appProfileBuilder.TryAddPattern(BuiltInPatterns.StranglerFigPattern);
            }

            ValidateCellBoundaries(activeCellBoundaries, orderedModules);
            if (activeCellBoundaries.Count > 0)
            {
                appProfileBuilder.AddTechnology(BuiltInTechnologies.CellBasedArchitecture);
            }

            var appProfile = appProfileBuilder.Build();
            var technologyCatalog = new TechnologyCatalogSnapshot(appProfileBuilder.GetTechnologyCatalog());
            var technologySelection = new TechnologySelection(appProfile.Technologies, technologyCatalog.Technologies);
            var localizedResources = new LocalizedResourceRegistry();
            foreach (var module in orderedModules.OfType<ILocalizedResourceContributor>())
            {
                module.RegisterResources(localizedResources);
            }

            var effectiveLocalizationSettings = localizedResources.ToSettings().Merge(localizationSettings);

            Services.TryAddSingleton(appProfile);
            Services.TryAddSingleton(technologyCatalog);
            Services.TryAddSingleton(technologySelection);
            Services.TryAddSingleton<IReadOnlyList<OwnedBehaviorRegistration>>(ownedBehaviorRegistrations);
            Services.TryAddSingleton<IReadOnlyList<AuditStoreDescriptor>>(_ => auditStores.ToArray());
            Services.TryAddSingleton<IReadOnlyList<OutboxDescriptor>>(_ => outboxes.ToArray());
            Services.TryAddSingleton<CellBoundaryCatalogSnapshot>(_ =>
                new CellBoundaryCatalogSnapshot(activeCellBoundaries));
            Services.TryAddSingleton<ICellBoundaryCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<CellBoundaryCatalogSnapshot>());
            Services.TryAddSingleton<BackendForFrontendRuntimeCatalogSnapshot>(_ =>
                new BackendForFrontendRuntimeCatalogSnapshot(activeBackendForFrontendBindings));
            Services.TryAddSingleton<IBackendForFrontendRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<BackendForFrontendRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<StranglerFigRuntimeCatalogSnapshot>(_ =>
                new StranglerFigRuntimeCatalogSnapshot(activeStranglerFigRoutes, migrationSettings.StranglerFig));
            Services.TryAddSingleton<IStranglerFigRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<StranglerFigRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<IStranglerFigMigrationRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<StranglerFigRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<IStranglerFigIngressRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<StranglerFigRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<IStranglerFigRouter>(serviceProvider =>
                serviceProvider.GetRequiredService<StranglerFigRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<DatabaseRoleCatalogSnapshot>(serviceProvider =>
                new DatabaseRoleCatalogSnapshot(
                    appProfile,
                    serviceProvider.GetServices<IDatabaseRoleRuntimeContributor>()));
            Services.TryAddSingleton<IDatabaseRoleCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<DatabaseRoleCatalogSnapshot>());
            Services.TryAddSingleton<DatabaseMigrationCatalogSnapshot>(serviceProvider =>
                new DatabaseMigrationCatalogSnapshot(
                    serviceProvider.GetServices<IDatabaseMigrationContributor>()));
            Services.TryAddSingleton<IDatabaseMigrationCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<DatabaseMigrationCatalogSnapshot>());
            Services.TryAddSingleton<IDatabaseMigrationOperationalPlaybookProvider, DatabaseMigrationOperationalPlaybookProvider>();
            Services.TryAddSingleton<IDatabaseTopologyOperationalSnapshotProvider, DatabaseTopologyOperationalSnapshotProvider>();
            Services.TryAddSingleton<IProjectionCatalog>(_ => new ProjectionCatalogSnapshot(projections));
            Services.TryAddSingleton<IOutboxCatalog>(serviceProvider =>
                new OutboxCatalogSnapshot(
                    serviceProvider.GetRequiredService<IReadOnlyList<OutboxDescriptor>>(),
                    serviceProvider.GetService<IOutboxDispatchPolicyCatalog>()));
            Services.TryAddSingleton<IInboxCatalog>(_ => new InboxCatalogSnapshot(inboxes));
            Services.TryAddSingleton<IAuditStoreCatalog>(_ => new AuditStoreCatalogSnapshot(auditStores));
            Services.TryAddSingleton<IAuthorizationPolicyCatalog>(_ => new AuthorizationPolicyCatalogSnapshot(authorizationPolicies));
            Services.TryAddSingleton<FeatureFlagRuntimeCatalogSnapshot>(_ =>
                new FeatureFlagRuntimeCatalogSnapshot(activeFeatureFlags));
            Services.TryAddSingleton<IFeatureFlagRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<FeatureFlagRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<IFeatureToggle>(serviceProvider =>
                new InMemoryFeatureToggle(
                    serviceProvider.GetRequiredService<IFeatureFlagRuntimeCatalog>(),
                    serviceProvider.GetServices<IFeatureFlagProvider>()));
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, CellBoundaryTechnologyRuntimeContributor>());
            Services.TryAddSingleton<TechnologyRuntimeCatalogSnapshot>(serviceProvider =>
                new TechnologyRuntimeCatalogSnapshot(
                    serviceProvider.GetServices<ITechnologyRuntimeContributor>()));
            Services.TryAddSingleton<ITechnologyRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<TechnologyRuntimeCatalogSnapshot>());

            foreach (var module in orderedModules)
            {
                module.ConfigureServices(Services);
            }

            foreach (var technologyServiceContributor in orderedModules.OfType<ITechnologyServiceContributor>())
            {
                technologyServiceContributor.ConfigureTechnologyServices(Services, technologySelection);
            }

            Services.TryAddSingleton(effectiveLocalizationSettings);
            Services.TryAddSingleton<ILocalizedTextCatalog>(serviceProvider =>
                new LocalizedTextCatalog(serviceProvider.GetRequiredService<LocalizationSettings>()));

            foreach (var module in orderedModules)
            {
                module.RegisterCapabilities(capabilities.ForModule(module.Descriptor.Id));

                if (module is ITechnologyCapabilityContributor technologyCapabilityContributor)
                {
                    technologyCapabilityContributor.RegisterTechnologyCapabilities(
                        capabilities.ForModule(module.Descriptor.Id),
                        technologySelection);
                }
            }

            var moduleManifests = orderedModules
                .Select(module => CreateManifest(module, modulesByType, modulePackages, packageManifestByModuleType, trustPolicy))
                .ToArray();
            var configuredCapabilities = ApplyCapabilityOptions(capabilities.Build(), engineOptions);
            var trustSnapshot = CapabilityPolicyEvaluator.CreateSnapshot(
                trustPolicy,
                packageManifests,
                moduleManifests,
                configuredCapabilities);
            var allowedCapabilityKeys = trustSnapshot.Capabilities
                .Where(static decision => decision.IsAllowed)
                .Select(static decision => decision.CapabilityKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var effectiveCapabilities = configuredCapabilities
                .Where(capability => allowedCapabilityKeys.Contains(capability.Key))
                .ToArray();
            var validatedExecutionGraphs = ExecutionGraphValidation.Validate(
                executionGraphs,
                moduleManifests,
                effectiveCapabilities);
            var validatedHostedExecutions = HostedExecutionValidation.Validate(
                hostedExecutions,
                moduleManifests,
                validatedExecutionGraphs);

            Services.AddSingleton(trustSnapshot);
            Services.AddSingleton<CapabilityPolicyEvaluator>();
            Services.TryAddSingleton<ExecutionRuntimeCatalogSnapshot>(_ =>
                new ExecutionRuntimeCatalogSnapshot(validatedExecutionGraphs));
            Services.TryAddSingleton<IExecutionRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<ExecutionRuntimeCatalogSnapshot>());
            Services.TryAddSingleton<HostedExecutionRuntimeCatalogSnapshot>(_ =>
                new HostedExecutionRuntimeCatalogSnapshot(validatedHostedExecutions));
            Services.TryAddSingleton<IHostedExecutionRuntimeCatalog>(serviceProvider =>
                serviceProvider.GetRequiredService<HostedExecutionRuntimeCatalogSnapshot>());
            var manifest = new RuntimeManifest(
                manifestVersion: RuntimeManifest.CurrentVersion,
                engineVersion: GetAssemblyVersion(typeof(EngineBuilder).Assembly),
                generatedAtUtc: DateTimeOffset.UtcNow,
                appProfile: appProfile,
                modules: moduleManifests,
                capabilities: effectiveCapabilities,
                packages: packageManifests);

            buildActivity?.SetTag("cephalon.blueprint", appProfile.BlueprintId);
            buildActivity?.SetTag("cephalon.module.count", manifest.Modules.Count);
            buildActivity?.SetTag("cephalon.capability.count", manifest.Capabilities.Count);

            var buildTags = new TagList
            {
                { "cephalon.blueprint", appProfile.BlueprintId },
                { "cephalon.module.count", manifest.Modules.Count },
                { "cephalon.capability.count", manifest.Capabilities.Count }
            };
            EngineDiagnostics.EngineBuildCounter.Add(1, buildTags);

            return new EngineRuntime(orderedModules, manifest, failurePolicy, validatedExecutionGraphs, validatedHostedExecutions);
        }
        catch (Exception exception)
        {
            buildActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }

    private static CapabilityManifest[] ApplyCapabilityOptions(
        CapabilityManifest[] capabilities,
        EngineOptions options)
    {
        if (!options.HasValues)
        {
            return capabilities;
        }

        return capabilities
            .Where(capability => options.IsCapabilityEnabled(capability.Key))
            .ToArray();
    }

    private static void ValidateCellBoundaries(
        List<CellBoundaryDescriptor> cellBoundaries,
        List<IModule> orderedModules)
    {
        ArgumentNullException.ThrowIfNull(cellBoundaries);
        ArgumentNullException.ThrowIfNull(orderedModules);

        if (cellBoundaries.Count == 0)
        {
            return;
        }

        var knownModuleIds = orderedModules
            .Select(static module => module.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var cellBoundary in cellBoundaries)
        {
            foreach (var moduleId in cellBoundary.ModuleIds)
            {
                if (!knownModuleIds.Contains(moduleId))
                {
                    throw new InvalidOperationException(
                        $"Cell boundary '{cellBoundary.Id}' references unknown module '{moduleId}'.");
                }
            }
        }
    }

    private static BackendForFrontendClientBindingDescriptor CreateBackendForFrontendClientBindingDescriptor(
        BackendForFrontendClientBindingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new BackendForFrontendClientBindingDescriptor(
            id: settings.Id,
            clientId: settings.ClientId,
            sourceModuleId: settings.SourceModuleId,
            displayName: settings.DisplayName,
            description: settings.Description,
            transportId: settings.TransportId,
            entryPoint: settings.EntryPoint,
            behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                includedBehaviorIds: settings.BehaviorFilter.IncludedBehaviorIds,
                excludedBehaviorIds: settings.BehaviorFilter.ExcludedBehaviorIds,
                includedCapabilityKeys: settings.BehaviorFilter.IncludedCapabilityKeys,
                excludedCapabilityKeys: settings.BehaviorFilter.ExcludedCapabilityKeys,
                includedTags: settings.BehaviorFilter.IncludedTags,
                excludedTags: settings.BehaviorFilter.ExcludedTags),
            metadata: settings.Metadata);
    }

    private static FeatureFlagDescriptor CreateFeatureFlagDescriptor(
        FeatureFlagSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new FeatureFlagDescriptor(
            id: settings.Id,
            displayName: settings.DisplayName,
            description: settings.Description,
            enabled: settings.Enabled,
            sourceKind: settings.SourceKind,
            sourceModuleId: settings.SourceModuleId,
            targeting: new FeatureFlagTargetingDescriptor(
                includedModuleIds: settings.Targeting.IncludedModuleIds,
                excludedModuleIds: settings.Targeting.ExcludedModuleIds,
                includedBehaviorIds: settings.Targeting.IncludedBehaviorIds,
                excludedBehaviorIds: settings.Targeting.ExcludedBehaviorIds,
                includedCapabilityKeys: settings.Targeting.IncludedCapabilityKeys,
                excludedCapabilityKeys: settings.Targeting.ExcludedCapabilityKeys,
                includedTransportIds: settings.Targeting.IncludedTransportIds,
                excludedTransportIds: settings.Targeting.ExcludedTransportIds,
                includedEnvironmentNames: settings.Targeting.IncludedEnvironmentNames,
                excludedEnvironmentNames: settings.Targeting.ExcludedEnvironmentNames,
                includedTenantIds: settings.Targeting.IncludedTenantIds,
                excludedTenantIds: settings.Targeting.ExcludedTenantIds,
                includedSubjectIds: settings.Targeting.IncludedSubjectIds,
                excludedSubjectIds: settings.Targeting.ExcludedSubjectIds,
                includedTags: settings.Targeting.IncludedTags,
                excludedTags: settings.Targeting.ExcludedTags),
            providerBindings: settings.ProviderBindings
                .Select(static binding => new FeatureFlagProviderBindingDescriptor(
                    providerId: binding.ProviderId,
                    providerFeatureId: binding.ProviderFeatureId,
                    metadata: binding.Metadata))
                .ToArray(),
            metadata: settings.Metadata);
    }

    private static OwnedBehaviorRegistration[] CollectOwnedBehaviorRegistrations(
        List<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        if (modules.Count == 0)
        {
            return [];
        }

        var registrations = new List<OwnedBehaviorRegistration>();

        foreach (var module in modules.OfType<IBehaviorOwnerModule>())
        {
            var builder = new OwnedBehaviorModuleBuilder(module.Descriptor.Id);
            module.ConfigureBehaviors(builder);
            registrations.AddRange(builder.Build());
        }

        ValidateOwnedBehaviorRegistrations(registrations);
        return [.. registrations];
    }

    private static void ValidateOwnedBehaviorRegistrations(
        IReadOnlyList<OwnedBehaviorRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        var duplicateBehaviorId = registrations
            .GroupBy(static registration => registration.BehaviorId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group
                .Select(static registration => registration.SourceModuleId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Skip(1)
                .Any());

        if (duplicateBehaviorId is not null)
        {
            var sourceModules = duplicateBehaviorId
                .Select(static registration => registration.SourceModuleId)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            throw new InvalidOperationException(
                $"Behavior '{duplicateBehaviorId.Key}' is owned by multiple modules: {string.Join(", ", sourceModules)}.");
        }

        var duplicateBehaviorType = registrations
            .GroupBy(static registration => registration.BehaviorType)
            .FirstOrDefault(static group => group
                .Select(static registration => registration.SourceModuleId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Skip(1)
                .Any());

        if (duplicateBehaviorType is not null)
        {
            var sourceModules = duplicateBehaviorType
                .Select(static registration => registration.SourceModuleId)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            throw new InvalidOperationException(
                $"Behavior type '{duplicateBehaviorType.Key.FullName}' is owned by multiple modules: {string.Join(", ", sourceModules)}.");
        }
    }

    private static ModuleManifest CreateManifest(
        IModule module,
        Dictionary<Type, IModule> modulesByType,
        Dictionary<Type, LoadedPackage> modulePackages,
        Dictionary<Type, PackageManifest> packageManifestByModuleType,
        TrustPolicy trustPolicy)
    {
        var moduleType = module.GetType();
        var assembly = moduleType.Assembly.GetName();
        var dependsOn = module.Descriptor.DependsOn
            .Select(dependencyType => modulesByType[dependencyType].Descriptor.Id)
            .ToArray();
        var packageId = modulePackages.TryGetValue(moduleType, out var package)
            ? GetPackageId(package)
            : null;
        var isTrusted = packageManifestByModuleType.TryGetValue(moduleType, out var packageManifest)
            ? packageManifest.IsTrusted
            : IsAssemblyTrusted(assembly.Name, trustPolicy);

        return new ModuleManifest(
            id: module.Descriptor.Id,
            displayName: module.Descriptor.DisplayName,
            description: module.Descriptor.Description,
            version: GetModuleVersion(module),
            assemblyName: assembly.Name ?? moduleType.Assembly.ManifestModule.Name,
            typeName: moduleType.FullName ?? moduleType.Name,
            dependsOn: dependsOn,
            tags: module.Descriptor.Tags.ToArray(),
            metadata: module.Descriptor.Metadata,
            packageId: packageId,
            isTrusted: isTrusted);
    }

    private static PackageManifest CreatePackageManifest(
        LoadedPackage package,
        TrustPolicy trustPolicy)
    {
        var packageId = GetPackageId(package);
        var assemblyName = package.Assembly.GetName().Name ?? package.Assembly.ManifestModule.Name;
        var primarySignatureCertificateThumbprint = GetPrimarySignatureCertificateThumbprint(package.SignatureVerification.Signatures);
        var (isTrusted, trustReason) = ResolvePackageTrust(
            packageId,
            assemblyName,
            package.Request.PublisherId,
            package.Request.Signatures,
            package.SignatureVerification,
            package.ChecksumSha256,
            trustPolicy);

        return new PackageManifest(
            id: packageId,
            kind: package.Request.Kind,
            assemblyName: assemblyName,
            path: package.Request.ResolvedAssemblyPath,
            sourcePath: package.Request.ResolvedSourcePath,
            loadContext: package.LoadContextName,
            modules: package.Modules.Select(static module => module.Descriptor.Id).ToArray(),
            version: package.Request.Version,
            minimumEngineVersion: package.Request.MinimumEngineVersion,
            maximumEngineVersion: package.Request.MaximumEngineVersion,
            supportedTargetFrameworks: package.Request.SupportedTargetFrameworks,
            publisherId: package.Request.PublisherId,
            publisherDisplayName: package.Request.PublisherDisplayName,
            publisherWebsite: package.Request.PublisherWebsite,
            distribution: package.Request.Distribution is null
                ? null
                : new PackageDistributionManifest(
                    channel: package.Request.Distribution.Channel,
                    manifestUri: package.Request.Distribution.ManifestUri,
                    packageUri: package.Request.Distribution.PackageUri),
            provenance: package.Request.Provenance is null
                ? null
                : new PackageProvenanceManifest(
                    sourceRepository: package.Request.Provenance.SourceRepository,
                    sourceRevision: package.Request.Provenance.SourceRevision,
                    buildUri: package.Request.Provenance.BuildUri,
                    statementUri: package.Request.Provenance.StatementUri),
            signatureType: package.Request.SignatureType,
            signatureSigner: package.Request.SignatureSigner,
            signatureKeyId: package.Request.SignatureKeyId,
            signatureFingerprint: package.Request.SignatureFingerprint,
            signatureCertificateThumbprint: primarySignatureCertificateThumbprint,
            signatureAlgorithm: package.Request.SignatureAlgorithm,
            signatures: package.SignatureVerification.Signatures
                .Select(static signature => new PackageSignatureManifest(
                    type: signature.Type,
                    signer: signature.Signer,
                    keyId: signature.KeyId,
                    fingerprint: signature.Fingerprint,
                    algorithm: signature.Algorithm,
                    verificationSource: signature.VerificationSource,
                    certificateThumbprint: signature.CertificateThumbprint,
                    isVerified: signature.IsVerified,
                    verificationReason: signature.Reason))
                .ToArray(),
            isSignatureVerified: package.SignatureVerification.IsVerified,
            signatureVerificationReason: package.SignatureVerification.Reason,
            checksumSha256: package.ChecksumSha256,
            isTrusted: isTrusted,
            trustReason: trustReason)
        {
            Dependencies = package.Request.Dependencies
                .Select(static dependency => new PackageDependencyManifest(
                    id: dependency.Id,
                    minimumVersion: dependency.MinimumVersion,
                    maximumVersion: dependency.MaximumVersion))
                .ToArray()
        };
    }

    private static (bool IsTrusted, string Reason) ResolvePackageTrust(
        string packageId,
        string assemblyName,
        string? publisherId,
        IReadOnlyList<PackageSignatureLoadRequest> signatures,
        PackageSignatureVerificationResult signatureVerification,
        string checksumSha256,
        TrustPolicy trustPolicy)
    {
        if (trustPolicy.TrustedPackages.Contains(packageId, StringComparer.OrdinalIgnoreCase))
        {
            return (true, "Package id is explicitly trusted by the current trust policy.");
        }

        if (trustPolicy.TrustedAssemblies.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
        {
            return (true, "Package assembly is explicitly trusted by the current trust policy.");
        }

        if (signatureVerification.IsVerified)
        {
            return (true, signatureVerification.Reason);
        }

        if (trustPolicy.IsPublisherTrusted(publisherId))
        {
            return (true, "Package publisher is explicitly trusted by the current trust policy.");
        }

        if (signatures.Any(signature => trustPolicy.IsSignerFingerprintTrusted(signature.Fingerprint)))
        {
            return (true, "Package signer fingerprint is explicitly trusted by the current trust policy.");
        }

        if (trustPolicy.IsChecksumAllowed(packageId, checksumSha256))
        {
            return (true, "Package checksum is allow-listed by the current trust policy.");
        }

        return (false, "Package is not trusted by the current trust policy.");
    }

    private static string? GetPrimarySignatureCertificateThumbprint(
        IReadOnlyList<PackageSignatureVerificationEntry> signatures)
    {
        for (var index = 0; index < signatures.Count; index++)
        {
            if (signatures[index].IsVerified &&
                !string.IsNullOrWhiteSpace(signatures[index].CertificateThumbprint))
            {
                return signatures[index].CertificateThumbprint;
            }
        }

        for (var index = 0; index < signatures.Count; index++)
        {
            if (!string.IsNullOrWhiteSpace(signatures[index].CertificateThumbprint))
            {
                return signatures[index].CertificateThumbprint;
            }
        }

        return null;
    }

    private static string GetPackageId(LoadedPackage package)
    {
        return package.Request.Id;
    }

    private static bool IsAssemblyTrusted(string? assemblyName, TrustPolicy trustPolicy)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return !trustPolicy.RequireTrustedPackages;
        }

        if (trustPolicy.TrustedAssemblies.Count == 0)
        {
            return true;
        }

        return trustPolicy.TrustedAssemblies.Contains(assemblyName.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetAssemblyVersion(Assembly assembly)
    {
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }

    private static string GetModuleVersion(IModule module)
    {
        return module.Descriptor.Version ?? GetAssemblyVersion(module.GetType().Assembly);
    }

    private static void ValidateModuleIdentity(IReadOnlyList<IModule> modules)
    {
        var duplicateIds = modules
            .GroupBy(module => module.Descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateIds is not null)
        {
            throw new InvalidOperationException(
                $"Cephalon module id '{duplicateIds.Key}' is registered multiple times.");
        }

        var duplicateTypes = modules
            .GroupBy(module => module.GetType())
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateTypes is not null)
        {
            throw new InvalidOperationException(
                $"Cephalon module type '{duplicateTypes.Key.Name}' is registered multiple times.");
        }
    }
}
