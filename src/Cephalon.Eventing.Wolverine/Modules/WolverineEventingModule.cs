using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using Cephalon.Eventing.Wolverine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Cephalon.Eventing.Wolverine.Modules;

internal sealed class WolverineEventingModule(Action<WolverineEventingOptions>? configureOptions)
    : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor, IExecutionGraphContributor, IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "wolverine-eventing",
        displayName: "Wolverine Eventing",
        description: "Official Wolverine host wiring for Cephalon event-driven integration workloads.",
        tags: ["technology", "eventing", "wolverine"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["technology"] = "event-driven-integration",
            ["adapter"] = "wolverine"
        });

    private readonly WolverineEventingOptions options = CreateOptions(configureOptions);
    private bool hasDispatchStore;
    private bool hasDispatchRuntimeReporter;
    private bool hasSubscriptionExecutors;

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);
        if (options.EnableDispatchLoop)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, WolverineEventingDiagnosticsConventionContributor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventDispatchRuntimeContributor, WolverineEventingDispatchRuntimeContributor>());
        }

        if (options.EnableSubscriptionExecution)
        {
            services.TryAddSingleton<WolverineManagedEventSubscriptionExecutorCatalog>();
            services.TryAddSingleton<WolverineManagedEventSubscriptionExecutionProcessor>();
            services.TryAddSingleton<WolverineManagedEventSubscriptionDispatcher>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventSubscriptionExecutionBindingContributor, WolverineManagedEventSubscriptionExecutorCatalog>());
        }
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        ArgumentNullException.ThrowIfNull(graphs);

        if (!options.EnableDispatchLoop)
        {
            return;
        }

        graphs.Add(new ExecutionGraphDescriptor(
            id: WolverineEventingRuntimeIds.ExecutionGraphId,
            displayName: "Wolverine Event Dispatch Flow",
            description: "Reads staged Cephalon integration events, publishes them through Wolverine, and records durable dispatch outcomes.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "read-staged-events",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "read-staged-events",
                    displayName: "Read Staged Events",
                    description: "Reads staged events that are eligible for dispatch from the active durable outbox path.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    tags: ["eventing", "outbox", "wolverine"]),
                new ExecutionGraphNodeDescriptor(
                    id: "publish-through-wolverine",
                    displayName: "Publish Through Wolverine",
                    description: "Publishes the staged event through Wolverine using the shared Cephalon event publication contract.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    tags: ["eventing", "handoff", "wolverine"]),
                new ExecutionGraphNodeDescriptor(
                    id: "record-dispatch-outcome",
                    displayName: "Record Dispatch Outcome",
                    description: "Persists the dispatch outcome back to the durable outbox store and runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    tags: ["eventing", "outbox", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "read-staged-events",
                    toNodeId: "publish-through-wolverine",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "publish-through-wolverine",
                    toNodeId: "record-dispatch-outcome",
                    displayName: "reported")
            ],
            tags: ["eventing", "wolverine", "dispatch"],
            metadata: new Dictionary<string, string>
            {
                ["eventDispatchRuntimeId"] = WolverineEventingRuntimeIds.DispatchRuntimeId
            }));
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        ArgumentNullException.ThrowIfNull(hostedExecutions);

        if (!options.EnableDispatchLoop)
        {
            return;
        }

        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: WolverineEventingRuntimeIds.HostedExecutionId,
            displayName: "Wolverine Event Dispatch Pump",
            description: "Runs the Wolverine-managed durable dispatch loop for staged Cephalon event publications.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: WolverineEventingRuntimeIds.ExecutionGraphId,
            startsWithHost: true,
            tags: ["eventing", "wolverine", "dispatch"],
            metadata: new Dictionary<string, string>
            {
                ["eventDispatchRuntimeId"] = WolverineEventingRuntimeIds.DispatchRuntimeId,
                ["dispatchOwnership"] = "wolverine-managed",
                ["publisherId"] = WolverineEventingRuntimeIds.PublisherId,
                ["technology"] = "event-driven-integration"
            }));
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        hasDispatchStore = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventDispatchStore));
        hasDispatchRuntimeReporter = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventDispatchRuntimeReporter));
        hasSubscriptionExecutors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionExecutor));
        if (options.EnableDispatchLoop && !hasDispatchStore)
        {
            throw new InvalidOperationException(
                "Wolverine-managed dispatch requires an active IEventDispatchStore. Register a durable outbox-backed dispatch store before enabling EnableDispatchLoop.");
        }

        if (options.EnableDispatchLoop && !hasDispatchRuntimeReporter)
        {
            throw new InvalidOperationException(
                "Wolverine-managed dispatch requires the core eventing publishing path to be active. Add Cephalon.Eventing with publishing enabled before enabling EnableDispatchLoop.");
        }

        if (options.EnableSubscriptionExecution && !options.EnableDispatchLoop)
        {
            throw new InvalidOperationException(
                "The current Wolverine-managed subscription execution baseline depends on EnableDispatchLoop. Enable the staged-event dispatch loop before turning on EnableSubscriptionExecution.");
        }

        if (options.EnableSubscriptionExecution && !options.EnableHostWiring)
        {
            throw new InvalidOperationException(
                "Wolverine-managed subscription execution requires EnableHostWiring because the pack has to register the internal retry handler types into Wolverine.");
        }

        if (options.EnableSubscriptionExecution && !hasSubscriptionExecutors)
        {
            throw new InvalidOperationException(
                "Wolverine-managed subscription execution requires at least one IEventSubscriptionExecutor. Register a managed executor before enabling EnableSubscriptionExecution.");
        }

        if (options.EnableHostWiring)
        {
            services.AddWolverine(ExtensionDiscovery.ManualOnly, wolverine =>
            {
                if (options.EnableSubscriptionExecution)
                {
                    wolverine.Discovery.IncludeType<WolverineManagedEventSubscriptionExecutionHandler>();
                }

                options.ConfigureHost?.Invoke(wolverine);
            });
        }

        if (options.EnableDispatchLoop)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, global::Cephalon.Eventing.Wolverine.Services.WolverineEventDispatchHostedService>());
        }

        if (options.EnableRuntimeSurface)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, WolverineEventingRuntimeSurfaceContributor>());
        }
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "eventing.wolverine",
            displayName: "Wolverine Eventing Adapter",
                description: "Registers Wolverine as the official first-class host integration path for Cephalon event-driven workloads.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["adapter"] = "wolverine",
                    ["hostWiring"] = options.EnableHostWiring ? "configured" : "disabled",
                    ["dispatchBridge"] = options.EnableDispatchLoop && hasDispatchStore ? "wolverine-managed" : "consumer-managed",
                    ["dispatchRuntime"] = options.EnableDispatchLoop && hasDispatchStore ? "configured" : "not-configured",
                    ["subscriptionExecution"] = options.EnableSubscriptionExecution && hasSubscriptionExecutors ? "wolverine-managed" : "not-configured",
                    ["dispatchLoop"] = options.EnableDispatchLoop ? "enabled" : "disabled",
                    ["dispatchStore"] = hasDispatchStore ? "available" : "not-configured"
                }));

        if (options.EnableDispatchLoop)
        {
            capabilities.Add(new Capability(
                key: "eventing.wolverine.dispatch",
                displayName: "Wolverine Dispatch Loop",
                description: "Runs a host-managed background loop that publishes staged Cephalon outbox events through Wolverine.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["adapter"] = "wolverine",
                    ["dispatchBridge"] = "wolverine-managed",
                    ["dispatchRuntime"] = "configured",
                    ["dispatchRuntimeId"] = WolverineEventingRuntimeIds.DispatchRuntimeId,
                    ["hostedExecutionId"] = WolverineEventingRuntimeIds.HostedExecutionId,
                    ["executionGraphId"] = WolverineEventingRuntimeIds.ExecutionGraphId,
                    ["publisherId"] = WolverineEventingRuntimeIds.PublisherId
                }));
        }

        if (options.EnableSubscriptionExecution)
        {
            capabilities.Add(new Capability(
                key: "eventing.subscribe",
                displayName: "Managed Event Subscription Execution",
                description: "Executes declared event subscriptions through the Wolverine-managed staged-event dispatch path with runtime-owned retry scheduling.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["adapter"] = "wolverine",
                    ["executionOwnership"] = "wolverine-managed",
                    ["executionMode"] = "message-handler",
                    ["executionRuntimeId"] = WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                    ["triggerRuntimeId"] = WolverineEventingRuntimeIds.DispatchRuntimeId,
                    ["retryPolicy"] = "fixed-delay"
                }));
        }
    }

    private static WolverineEventingOptions CreateOptions(Action<WolverineEventingOptions>? configure)
    {
        var options = new WolverineEventingOptions();
        configure?.Invoke(options);
        return options;
    }
}
