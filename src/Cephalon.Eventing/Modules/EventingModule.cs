using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
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
        services.TryAddSingleton(options);
        services.TryAddSingleton<IEventChannelCatalog, EventChannelCatalog>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventingRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        if (options.EnablePublishing)
        {
            capabilities.Add(new Capability(
                key: "eventing.publish",
                displayName: "Event Publishing",
                description: "Publishes integration events to configured event channels.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration"
                }));
        }

        if (options.EnableSubscriptions)
        {
            capabilities.Add(new Capability(
                key: "eventing.subscribe",
                displayName: "Event Subscription",
                description: "Subscribes to integration events from configured event channels.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "event-driven-integration"
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
