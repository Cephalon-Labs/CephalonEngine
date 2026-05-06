using System.Reflection;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Compatibility;
using Cephalon.Behaviors.Configuration;
using Cephalon.Behaviors.Features;
using Cephalon.Behaviors.Resilience;
using Cephalon.Behaviors.Runtime;
using Cephalon.Resilience;
using Cephalon.Behaviors.Services;
using Cephalon.Behaviors.Validation;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;

namespace Cephalon.Behaviors.Modules;

/// <summary>
/// The Cephalon behavior module. Registers behavior topology infrastructure and
/// exposes the built-in interaction pattern capabilities to the runtime.
/// </summary>
internal sealed class BehaviorModule(
    IConfiguration? configuration,
    Action<BehaviorOptions>? configureOptions,
    Action<IBehaviorCollectionBuilder>? configureBehaviors)
    : ModuleBase
{
    private bool publishesSagaChoreographyRuntimeCatalogCapability;
    private bool publishesSagaChoreographyPublicationStateCapability;

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

        var slotRegistry = new BehaviorExecutionSlotRegistry();
        services.TryAddSingleton(slotRegistry);

        // Run fluent registrations so contributors and DI types are wired up before catalog build
        if (configureBehaviors is not null)
        {
            var builder = new BehaviorCollectionBuilder(services);
            configureBehaviors(builder);
        }

        var ownedBehaviorRegistrations = ResolveOwnedBehaviorRegistrations(services);
        var ownedBehaviorIds = ownedBehaviorRegistrations.Count == 0
            ? null
            : ownedBehaviorRegistrations
                .Select(static registration => registration.BehaviorId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ownedBehaviorRegistrations.Count > 0)
        {
            var builder = new BehaviorCollectionBuilder(services);
            foreach (var registration in ownedBehaviorRegistrations)
            {
                builder.Register(registration);
            }
        }

        // Auto-register behaviors from generated assembly hints when enabled.
        if (options.AutoRegister)
        {
            AutoRegisterBehaviors(services, slotRegistry, options, ownedBehaviorIds);
        }

        RegisterBehaviorResilienceServices(
            services,
            configuration);

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionMiddleware, BehaviorFeatureFlagExecutionMiddleware>());

        // Catalog — collected from all IBehaviorContributor enumerations
        services.TryAddSingleton<IBehaviorCatalog>(static serviceProvider =>
            new BehaviorCatalog(serviceProvider.GetServices<IBehaviorContributor>()));

        // Dispatcher — built after catalog and registry are available
        services.TryAddSingleton<BehaviorDispatcher>();

        // Compatibility rules
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt001SagaStepStatefulTransportRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt003ProcessManagerRequiresInboxRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt004CqrsMultipleTransportsRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt005SagaChoreographyOutboxRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt006DurableExecutionRequiresEventSourcingRule>());

        // Compatibility matrix
        services.TryAddSingleton<CompatibilityMatrix>();

        // Validation
        services.TryAddSingleton<BehaviorAllowlistValidator>();

        // Advisory catalog — aggregates from all IBehaviorAdvisoryContributor registrations
        services.TryAddSingleton<IBehaviorAdvisoryCatalog, BehaviorAdvisoryCatalog>();

        // Runtime surface contributor — exposes behavior topology to /engine/snapshot
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, BehaviorRuntimeContributor>());

        publishesSagaChoreographyRuntimeCatalogCapability =
            IsServiceRegistered(services, typeof(ISagaChoreographyRuntimeCatalog));
        publishesSagaChoreographyPublicationStateCapability =
            IsServiceRegistered(services, typeof(ISagaChoreographyPublicationRuntimeStateCatalog));
    }

    private static void RegisterBehaviorResilienceServices(
        IServiceCollection services,
        IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        var policyCatalog = ResolveBehaviorResiliencePolicyCatalog(services, configuration);
        var circuitBreakerStates = new BehaviorCircuitBreakerStateRegistry();
        services.TryAddSingleton<BehaviorIdempotencyResolver>();
        services.TryAddSingleton(policyCatalog);
        services.TryAddSingleton(circuitBreakerStates);
        services.TryAddSingleton<IBehaviorResilienceExceptionClassifier, DefaultBehaviorResilienceExceptionClassifier>();
        services.TryAddSingleton<Cephalon.Abstractions.Resilience.IBehaviorResilienceRuntimeCatalog>(serviceProvider =>
            new BehaviorResilienceRuntimeCatalog(
                policyCatalog,
                circuitBreakerStates,
                serviceProvider.GetRequiredService<BehaviorIdempotencyResolver>()));

        if (!policyCatalog.HasEnforcedPolicies)
        {
            return;
        }

        foreach (var policy in policyCatalog.EnforcedPolicies)
        {
            var policyId = policy.Id;
            if (policy.Effective.CircuitBreaker.Enabled == true &&
                policy.Effective.CircuitBreaker.HasValues)
            {
                circuitBreakerStates.Ensure(policyId);
            }

            services.AddResiliencePipeline<string>(
                policyId,
                (builder, context) =>
                {
                    var resolvedPolicyCatalog = context.ServiceProvider.GetRequiredService<BehaviorResiliencePolicyCatalog>();
                    var exceptionClassifier = context.ServiceProvider.GetRequiredService<IBehaviorResilienceExceptionClassifier>();
                    var resolvedCircuitBreakerStates = context.ServiceProvider.GetRequiredService<BehaviorCircuitBreakerStateRegistry>();
                    var resolvedPolicy = resolvedPolicyCatalog.GetById(policyId)
                        ?? throw new InvalidOperationException(
                            $"Behavior resilience policy '{policyId}' was not found when building its resilience pipeline.");
                    resolvedPolicy.Configure(
                        builder,
                        exceptionClassifier,
                        resolvedCircuitBreakerStates.Get(policyId));
                });
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorExecutionMiddleware, BehaviorResilienceExecutionMiddleware>());
    }

    private static BehaviorResiliencePolicyCatalog ResolveBehaviorResiliencePolicyCatalog(
        IServiceCollection services,
        IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        var appProfile = ResolveRegisteredSingleton<AppProfile>(services);
        if (appProfile is not null)
        {
            return BehaviorResiliencePolicyResolver.ResolvePolicies(appProfile.Resilience);
        }

        return configuration is null
            ? new BehaviorResiliencePolicyCatalog([])
            : BehaviorResiliencePolicyResolver.ResolvePolicies(ResilienceSettings.FromConfiguration(configuration));
    }

    private static IReadOnlyList<OwnedBehaviorRegistration> ResolveOwnedBehaviorRegistrations(
        IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(IReadOnlyList<OwnedBehaviorRegistration>) &&
                descriptor.ImplementationInstance is IReadOnlyList<OwnedBehaviorRegistration> registrations)
            {
                return registrations;
            }
        }

        return [];
    }

    private static TService? ResolveRegisteredSingleton<TService>(IServiceCollection services)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(TService) &&
                descriptor.ImplementationInstance is TService instance)
            {
                return instance;
            }
        }

        return null;
    }

    private static bool IsServiceRegistered(
        IServiceCollection services,
        Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceType);

        return services.Any(descriptor => descriptor.ServiceType == serviceType);
    }

    private static Dictionary<string, string> CreateSagaChoreographyRuntimeCatalogCapabilityMetadata()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pack"] = "Cephalon.Behaviors.Patterns",
            ["pattern"] = "saga-choreography",
            ["surface"] = "runtime-catalog",
            ["activation"] = "add-behavior-patterns",
            ["serviceContract"] = typeof(ISagaChoreographyRuntimeCatalog).FullName ?? nameof(ISagaChoreographyRuntimeCatalog),
            ["snapshotField"] = "SagaChoreographies",
            ["aspNetCoreRoute"] = "/engine/saga-choreographies"
        };
    }

    private static Dictionary<string, string> CreateSagaChoreographyPublicationStateCapabilityMetadata()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pack"] = "Cephalon.Behaviors.Patterns",
            ["pattern"] = "saga-choreography",
            ["surface"] = "publication-state",
            ["activation"] = "add-behavior-patterns",
            ["serviceContract"] = typeof(ISagaChoreographyPublicationRuntimeStateCatalog).FullName ?? nameof(ISagaChoreographyPublicationRuntimeStateCatalog),
            ["snapshotField"] = "SagaChoreographyPublicationStates",
            ["aspNetCoreRoute"] = "/engine/saga-choreographies/runtime",
            ["ownership"] = "choreography-strategy",
            ["outcomes"] = "accepted,failed",
            ["bridgeTruth"] = "separate"
        };
    }

    /// <summary>
    /// Registers behaviors from source-generated assembly hints without scanning assembly types.
    /// </summary>
    private static void AutoRegisterBehaviors(
        IServiceCollection services,
        BehaviorExecutionSlotRegistry slotRegistry,
        BehaviorOptions options,
        IReadOnlySet<string>? ownedBehaviorIds)
    {
        var assemblies = options.ResolveAutoRegisterAssemblies();
        if (assemblies.Count == 0) return;

        var requiresGeneratedHints = options.AutoRegisterAssemblies.Any(static name => !string.IsNullOrWhiteSpace(name));
        var missingGeneratedHintAssemblies = new List<string>();
        foreach (var assembly in assemblies)
        {
            if (TrySourceGeneratedRegistration(services, slotRegistry, assembly, ownedBehaviorIds))
            {
                continue;
            }

            if (requiresGeneratedHints)
            {
                missingGeneratedHintAssemblies.Add(assembly.GetName().Name ?? assembly.FullName ?? assembly.ToString());
            }
        }

        if (missingGeneratedHintAssemblies.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Engine:Behaviors:AutoRegisterAssemblies can only auto-register assemblies that expose " +
            $"source-generated behavior module hints through {nameof(BehaviorGeneratedModuleRegistry)}. " +
            $"The following assemblies did not register generated hints: {string.Join(", ", missingGeneratedHintAssemblies.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase))}. " +
            "Reference the current Cephalon.Behaviors.SourceGen package and rebuild the behavior assembly, " +
            "or register behaviors explicitly through module ownership or AddBehaviors(..., behaviors => ...).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Source-generated registration (zero-reflection per-type)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads generated module hints registered by the behavior source generator's module initializer.
    /// Returns <see langword="true"/> if this assembly was handled via source generation.
    /// </summary>
    private static bool TrySourceGeneratedRegistration(
        IServiceCollection services,
        BehaviorExecutionSlotRegistry slotRegistry,
        Assembly assembly,
        IReadOnlySet<string>? ownedBehaviorIds)
    {
        if (!BehaviorGeneratedModuleRegistry.TryGetRegistration(assembly, out var registration))
        {
            return false;
        }

        registration.RegisterBehaviors(services);
        var implementationDescriptors = BehaviorImplementationRegistration
            .GetRegisteredDescriptors(services)
            .ToDictionary(static descriptor => descriptor.Id, static descriptor => descriptor, StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in registration.ExecutionSlots)
        {
            if (ownedBehaviorIds?.Contains(descriptor.Id) == true)
            {
                continue;
            }

            if (!implementationDescriptors.TryGetValue(descriptor.Id, out var implementationDescriptor) ||
                implementationDescriptor.BehaviorType != descriptor.Type)
            {
                continue;
            }

            slotRegistry.Register(descriptor.Id, descriptor.Type, descriptor.Slot);
        }

        foreach (var topologyDescriptor in registration.TopologyDescriptors)
        {
            if (ownedBehaviorIds?.Contains(topologyDescriptor.Id) == true)
            {
                continue;
            }

            if (!implementationDescriptors.TryGetValue(topologyDescriptor.Id, out var implementationDescriptor))
            {
                continue;
            }

            var normalizedDescriptor = BehaviorAttributeTopologyResolver.Resolve(
                topologyDescriptor.Id,
                implementationDescriptor.BehaviorType,
                topologyDescriptor);
            if (normalizedDescriptor is not null)
            {
                services.AddSingleton<IBehaviorContributor>(new FluentBehaviorContributor(normalizedDescriptor));
            }
        }

        foreach (var runtimeBehavior in registration.RuntimeTopologyBehaviors)
        {
            if (ownedBehaviorIds?.Contains(runtimeBehavior.Id) == true)
            {
                continue;
            }

            throw new InvalidOperationException(
                "Source-generated behavior auto-registration can only use topology that Cephalon.Behaviors.SourceGen " +
                "can reduce to generated descriptors. The behavior " +
                $"'{runtimeBehavior.Id}' ({runtimeBehavior.Type.FullName}) still requires runtime ConfigureTopology(...) execution, " +
                "which is no longer performed. Rewrite ConfigureTopology(...) as a source-generator-supported fluent chain, " +
                "move the topology to [BehaviorAllowedPatterns]/[BehaviorAllowedTransports], or register the behavior explicitly with module/fluent topology.");
        }

        return true;
    }

    /// <summary>
    /// Registers the built-in interaction pattern capabilities exposed by this module.
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
            key: "behaviors.saga-choreography",
            displayName: "Saga Choreography Behaviors",
            description: "Choreography-based saga behavior topology for event-reaction coordination."));

        if (publishesSagaChoreographyRuntimeCatalogCapability)
        {
            capabilities.Add(new Capability(
                key: "behaviors.saga-choreography.runtime-catalog",
                displayName: "Saga Choreography Runtime Catalog",
                description: "Publishes the operator-facing saga choreography runtime catalog for active behaviors, transports, feature gates, and result-contract shape.",
                metadata: CreateSagaChoreographyRuntimeCatalogCapabilityMetadata()));
        }

        if (publishesSagaChoreographyPublicationStateCapability)
        {
            capabilities.Add(new Capability(
                key: "behaviors.saga-choreography.publication-state",
                displayName: "Saga Choreography Publication State",
                description: "Publishes the latest accepted or failed saga choreography publication handoff observations for operator introspection.",
                metadata: CreateSagaChoreographyPublicationStateCapabilityMetadata()));
        }

        capabilities.Add(new Capability(
            key: "behaviors.process-manager",
            displayName: "Process Manager Behaviors",
            description: "Process manager behavior topology for long-running orchestrations."));

        capabilities.Add(new Capability(
            key: "behaviors.durable-execution",
            displayName: "Durable Execution Behaviors",
            description: "Replayable durable workflow behavior topology backed by event-store semantics."));

        capabilities.Add(new Capability(
            key: "behaviors.direct",
            displayName: "Direct Behaviors",
            description: "Direct synchronous behavior topology with request-response semantics."));
    }
}
