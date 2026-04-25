using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Eventing.Configuration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;

namespace Cephalon.Eventing.Modules;

internal sealed class EventingModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "eventing-runtime",
        displayName: "Eventing Runtime",
        description: "Companion runtime services for event-driven integration workloads.",
        tags: ["technology", "eventing"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "technology-pack",
            ["technology"] = "event-driven-integration"
        });

    private readonly EventingOptions options;
    private bool hasChannelContributors;
    private bool hasDispatchStore;
    private bool hasDispatchRuntimeContributors;
    private bool hasInboxPath;
    private bool hasManagedSubscriptionExecutionBindings;
    private bool hasSubscriptionContributors;
    private bool hasPublishingPath;

    public EventingModule(EventingOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        hasChannelContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventChannelContributor));
        hasDispatchStore = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventDispatchStore));
        hasDispatchRuntimeContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventDispatchRuntimeContributor));
        hasInboxPath = services.Any(static descriptor => descriptor.ServiceType == typeof(IInbox));
        hasManagedSubscriptionExecutionBindings = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionExecutionBindingContributor));
        hasSubscriptionContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEventSubscriptionContributor));
        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, EventingDiagnosticsConventionContributor>());
        services.TryAddSingleton<IEventChannelCatalog, EventChannelCatalog>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingRuntimeSurfaceContributor>());
        if (options.EnableSubscriptions)
        {
            services.TryAddSingleton<IEventSubscriptionCatalog, EventSubscriptionCatalog>();
            services.TryAddSingleton<EventSubscriptionExecutionBindingCatalog>();
            services.TryAddSingleton<EventSubscriptionRuntimeCatalog>();
            services.TryAddSingleton<IEventSubscriptionRuntimeCatalog>(static provider => provider.GetRequiredService<EventSubscriptionRuntimeCatalog>());
            services.TryAddSingleton<IEventSubscriptionRuntimeReporter>(static provider => provider.GetRequiredService<EventSubscriptionRuntimeCatalog>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingSubscriptionRuntimeSurfaceContributor>());
        }

        hasPublishingPath = options.EnablePublishing &&
            services.Any(static descriptor => descriptor.ServiceType == typeof(IOutbox));
        if (hasPublishingPath)
        {
            services.TryAddSingleton<EventDispatchRuntimeDescriptorCatalog>();
            services.TryAddSingleton<EventDispatchRuntimeCatalog>();
            services.TryAddSingleton<IEventDispatchRuntimeDescriptorCatalog>(static provider => provider.GetRequiredService<EventDispatchRuntimeDescriptorCatalog>());
            services.TryAddSingleton<IEventDispatchRuntimeCatalog>(static provider => provider.GetRequiredService<EventDispatchRuntimeCatalog>());
            services.TryAddSingleton<IEventDispatchRuntimeReporter>(static provider => provider.GetRequiredService<EventDispatchRuntimeCatalog>());
            services.TryAddSingleton<IOutboxDispatchPolicyCatalog, OutboxDispatchPolicyCatalog>();
            services.TryAddScoped<IEventPublisher, OutboxBackedEventPublisher>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingPublishingRuntimeSurfaceContributor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingDispatchRuntimeSurfaceContributor>());
            if (hasDispatchRuntimeContributors)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingDispatchRuntimeCatalogSurfaceContributor>());
            }
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

        if (options.EnablePublishing && hasPublishingPath)
        {
            capabilities.Add(new Capability(
                key: "eventing.publish",
                displayName: "Event Publishing",
                description: "Accepts integration events for configured event channels and stages them through the active outbox path.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["handoff"] = "outbox",
                    ["dispatchRuntime"] = hasDispatchRuntimeContributors ? "configured" : "not-configured",
                    ["dispatchStore"] = hasDispatchStore ? "available" : "not-configured",
                    ["runtimeState"] = "available"
                }));
        }

        if (options.EnableSubscriptions && (options.Subscriptions.Count > 0 || hasSubscriptionContributors))
        {
            capabilities.Add(new Capability(
                key: "eventing.subscriptions",
                displayName: "Event Subscription Descriptors",
                description: "Exposes declared event subscription descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["dispatchRuntime"] = hasManagedSubscriptionExecutionBindings ? "configured" : "not-configured",
                    ["inbox"] = hasInboxPath ? "available" : "not-configured",
                    ["runtimeState"] = "available"
                }));
        }

        if (options.Channels.Count > 0 || hasChannelContributors)
        {
            capabilities.Add(new Capability(
                key: "eventing.channels",
                displayName: "Event Channels",
                description: "Exposes configured event channel descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration",
                    ["channelCount"] = options.Channels.Count.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }
}
