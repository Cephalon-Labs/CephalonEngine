using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Compatibility;
using Cephalon.Behaviors.Configuration;
using Cephalon.Behaviors.Runtime;
using Cephalon.Behaviors.Services;
using Cephalon.Behaviors.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Modules;

/// <summary>
/// The Cephalon behavior module. Registers behavior topology infrastructure and
/// exposes the five standard interaction pattern capabilities to the runtime.
/// </summary>
internal sealed class BehaviorModule(
    IConfiguration? configuration,
    Action<BehaviorOptions>? configureOptions,
    Action<IBehaviorCollectionBuilder>? configureBehaviors)
    : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "behaviors",
        displayName: "Behaviors",
        description: "ABT M1 Foundation: behavior topology, dispatch, and compatibility matrix for Cephalon runtimes.",
        tags: ["behaviors", "abt", "topology", "dispatch", "runtime"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "behaviors"
        });

    /// <summary>
    /// Gets the module descriptor used for discovery, ordering, and manifest output.
    /// </summary>
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <summary>
    /// Registers behavior infrastructure services into the service collection.
    /// </summary>
    /// <param name="services">The service collection receiving module services.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BehaviorOptions();

        // Bind from configuration first (Engine:Behaviors section)
        configuration?.GetSection("Engine:Behaviors")?.Bind(options);

        // Then apply code-level overrides (code wins over config)
        configureOptions?.Invoke(options);

        // Type registry — shared singleton populated by BehaviorCollectionBuilder
        var typeRegistry = new BehaviorTypeRegistry();
        services.TryAddSingleton<IBehaviorTypeRegistry>(typeRegistry);

        // Run fluent registrations so contributors and DI types are wired up before catalog build
        if (configureBehaviors is not null)
        {
            var builder = new BehaviorCollectionBuilder(services, typeRegistry);
            configureBehaviors(builder);
        }

        // Auto-register behaviors from assemblies when enabled (default: true)
        if (options.AutoRegister)
        {
            AutoRegisterBehaviors(services, typeRegistry, options);
        }

        services.TryAddSingleton(options);

        // Catalog — collected from all IBehaviorContributor enumerations
        services.TryAddSingleton<IBehaviorCatalog>(static serviceProvider =>
            new BehaviorCatalog(serviceProvider.GetServices<IBehaviorContributor>()));

        // Dispatcher — built after catalog and registry are available
        services.TryAddSingleton<BehaviorDispatcher>();

        // Compatibility rules
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt001SagaStepStatefulTransportRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt003ProcessManagerRequiresInboxRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt004CqrsMultipleTransportsRule>());

        // Compatibility matrix
        services.TryAddSingleton<CompatibilityMatrix>();

        // Validation
        services.TryAddSingleton<BehaviorAllowlistValidator>();

        // Advisory catalog — aggregates from all IBehaviorAdvisoryContributor registrations
        services.TryAddSingleton<IBehaviorAdvisoryCatalog, BehaviorAdvisoryCatalog>();

        // Runtime surface contributor — exposes behavior topology to /engine/snapshot
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, BehaviorRuntimeContributor>());
    }

    private static readonly Type AppBehaviorOpenGeneric = typeof(IAppBehavior<,>);

    /// <summary>
    /// Scans assemblies for behaviors using a two-phase strategy:
    /// <list type="number">
    ///   <item><description>Source-generated path — uses <see cref="ContainsBehaviorsAttribute"/> to find
    ///   pre-compiled registration code (zero reflection).</description></item>
    ///   <item><description>Reflection fallback — scans remaining assemblies for types with
    ///   <see cref="AppBehaviorAttribute"/> via runtime reflection.</description></item>
    /// </list>
    /// </summary>
    private static void AutoRegisterBehaviors(
        IServiceCollection services,
        BehaviorTypeRegistry typeRegistry,
        BehaviorOptions options)
    {
        var assemblies = options.ResolveAutoRegisterAssemblies();
        if (assemblies.Count == 0) return;

        foreach (var assembly in assemblies)
        {
            // Phase 1: Try source-generated registration (zero reflection)
            if (TrySourceGeneratedRegistration(services, typeRegistry, assembly))
                continue;

            // Phase 2: Reflection fallback for assemblies without source generation
            ReflectionScanAssembly(services, typeRegistry, assembly);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 1: Source-generated registration (zero-reflection per-type)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks for <c>[assembly: ContainsBehaviors(typeof(RegistrationClass))]</c> and
    /// invokes the generated <c>Register</c> and <c>GetTopologyDescriptors</c> methods.
    /// Returns <see langword="true"/> if this assembly was handled via source generation.
    /// </summary>
    private static bool TrySourceGeneratedRegistration(
        IServiceCollection services,
        BehaviorTypeRegistry typeRegistry,
        Assembly assembly)
    {
        // Single cheap attribute check per assembly — no type scanning needed
        var attr = assembly.GetCustomAttribute<ContainsBehaviorsAttribute>();
        if (attr?.RegistrationType is null)
            return false;

        var regType = attr.RegistrationType;

        // Invoke Register(IServiceCollection, IBehaviorTypeRegistry)
        var registerMethod = regType.GetMethod("Register",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(IServiceCollection), typeof(IBehaviorTypeRegistry)],
            null);

        registerMethod?.Invoke(null, [services, typeRegistry]);

        // Invoke GetTopologyDescriptors() → register as contributors
        var topologyMethod = regType.GetMethod("GetTopologyDescriptors",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (topologyMethod?.Invoke(null, null) is IReadOnlyList<BehaviorTopologyDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (!typeRegistry.TryGetType(descriptor.Id, out var behaviorType) || behaviorType is null)
                {
                    continue;
                }

                var normalizedDescriptor = BehaviorAttributeTopologyResolver.Resolve(
                    descriptor.Id,
                    behaviorType,
                    descriptor);
                if (normalizedDescriptor is not null)
                {
                    services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(normalizedDescriptor));
                }
            }
        }

        // Invoke GetBehaviorsNeedingRuntimeTopology() → fall back to reflection for those
        var runtimeTopologyMethod = regType.GetMethod("GetBehaviorsNeedingRuntimeTopology",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (runtimeTopologyMethod?.Invoke(null, null) is IReadOnlyList<(string Id, Type Type)> runtimeBehaviors)
        {
            foreach (var (id, type) in runtimeBehaviors)
            {
                TryRegisterResolvedTopology(services, type, id);
            }
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2: Reflection fallback
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans a single assembly using runtime reflection to discover behavior types.
    /// Used for assemblies without source-generated registration code (e.g., plugins, NuGet packages).
    /// </summary>
    private static void ReflectionScanAssembly(
        IServiceCollection services,
        BehaviorTypeRegistry typeRegistry,
        Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.DefinedTypes
                .Select(ti => ti.AsType())
                .ToArray();
        }
        catch (ReflectionTypeLoadException)
        {
            return;
        }

        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                continue;

            var attr = (AppBehaviorAttribute?)Attribute.GetCustomAttribute(
                type, typeof(AppBehaviorAttribute));
            if (attr is null) continue;

            // Must implement IAppBehavior<TIn, TOut>
            var appBehaviorInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                    i.GetGenericTypeDefinition() == AppBehaviorOpenGeneric);
            if (appBehaviorInterface is null) continue;

            // Skip if already registered (manual registration takes precedence)
            if (typeRegistry.TryGetType(attr.Id, out _)) continue;

            // Register the type in DI as transient
            services.TryAddTransient(type);

            // Populate the type registry
            typeRegistry.Register(attr.Id, type);

            // Invoke static ConfigureTopology if the concrete type defines one
            TryRegisterResolvedTopology(services, type, attr.Id);
        }
    }

    /// <summary>
    /// Checks whether the concrete behavior type defines a static <c>ConfigureTopology</c>
    /// method and, if so, invokes it to build and register a Layer-4 topology contributor.
    /// </summary>
    private static void TryRegisterResolvedTopology(
        IServiceCollection services,
        Type behaviorType,
        string behaviorId)
    {
        var descriptor = BuildTopologyDescriptorFromStaticMethod(behaviorType, behaviorId);
        descriptor = BehaviorAttributeTopologyResolver.Resolve(behaviorId, behaviorType, descriptor);
        if (descriptor is null)
        {
            return;
        }

        services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(descriptor));
    }

    private static BehaviorTopologyDescriptor? BuildTopologyDescriptorFromStaticMethod(
        Type behaviorType,
        string behaviorId)
    {
        var configMethod = behaviorType.GetMethod(
            "ConfigureTopology",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy,
            null,
            [typeof(IBehaviorTopologyBuilder)],
            null);

        if (configMethod is null || configMethod.DeclaringType != behaviorType)
            return null;

        var builder = new BehaviorTopologyBuilder();
        configMethod.Invoke(null, [builder]);
        return builder.Build(behaviorId);
    }

    /// <summary>
    /// Registers the five standard interaction pattern capabilities exposed by this module.
    /// </summary>
    /// <param name="capabilities">The capability registry receiving module capabilities.</param>
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "behaviors.cqrs",
            displayName: "CQRS Behaviors",
            description: "Command/query responsibility segregation behavior topology."));

        capabilities.Add(new Capability(
            key: "behaviors.event-driven",
            displayName: "Event-Driven Behaviors",
            description: "Event-driven behavior topology with stateful transport support."));

        capabilities.Add(new Capability(
            key: "behaviors.saga",
            displayName: "Saga Behaviors",
            description: "Saga step behavior topology with stateful transport enforcement."));

        capabilities.Add(new Capability(
            key: "behaviors.process-manager",
            displayName: "Process Manager Behaviors",
            description: "Process manager behavior topology for long-running orchestrations."));

        capabilities.Add(new Capability(
            key: "behaviors.direct",
            displayName: "Direct Behaviors",
            description: "Direct synchronous behavior topology with request-response semantics."));
    }
}
