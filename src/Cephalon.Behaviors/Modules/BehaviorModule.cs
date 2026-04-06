using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Behaviors.Compatibility;
using Cephalon.Behaviors.Configuration;
using Cephalon.Behaviors.Runtime;
using Cephalon.Behaviors.Services;
using Cephalon.Behaviors.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Behaviors.Modules;

/// <summary>
/// The Cephalon behavior module. Registers behavior topology infrastructure and
/// exposes the five standard interaction pattern capabilities to the runtime.
/// </summary>
internal sealed class BehaviorModule(
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

        services.TryAddSingleton(options);

        // Catalog — collected from all IBehaviorContributor enumerations
        services.TryAddSingleton<IBehaviorCatalog>(static serviceProvider =>
            new BehaviorCatalog(serviceProvider.GetServices<IBehaviorContributor>()));

        // Dispatcher — built after catalog and registry are available
        services.TryAddSingleton<BehaviorDispatcher>();

        // Compatibility rules
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt001SagaStepStatefulTransportRule>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBehaviorCompatibilityRule, Abt002EventDrivenWithHttpRestRule>());
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
