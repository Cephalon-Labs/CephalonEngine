using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Publishers;
using Cephalon.Eventing.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Eventing.Behaviors.Modules;

internal sealed class BehaviorEventingModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "eventing-behaviors",
        displayName: "Eventing Behaviors",
        description: "Explicit saga choreography bridge that stages behavior publications through the eventing publish path.",
        tags: ["technology", "eventing", "behaviors", "bridge"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["technology"] = "event-driven-integration",
            ["pattern"] = "saga-choreography"
        });

    private bool bridgeConfigured;

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
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

        if (HasExplicitSagaPublisherOverride(services))
        {
            return;
        }

        RemoveInMemorySagaPublishers(services);
        services.AddSingleton<ISagaChoreographyPublisher>(static provider =>
        {
            var serviceAvailability = provider.GetService<IServiceProviderIsService>();
            var hasEventPublisher = serviceAvailability?.IsService(typeof(global::Cephalon.Eventing.Services.IEventPublisher)) == true;
            var hasOutbox = serviceAvailability?.IsService(typeof(global::Cephalon.Abstractions.Data.IOutbox)) == true;

            if (!hasEventPublisher || !hasOutbox)
            {
                throw new InvalidOperationException(
                    "Cephalon.Eventing.Behaviors requires the Cephalon.Eventing publishing path to be active " +
                    "(EventDrivenIntegration selected, publishing enabled, and an IOutbox registered) before saga choreography publications can be bridged.");
            }

            return new EventingSagaChoreographyPublisher(
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetService<ILoggerFactory>());
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, BehaviorEventingRuntimeSurfaceContributor>());
        bridgeConfigured = true;
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration") || !bridgeConfigured)
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "eventing.behaviors.saga-choreography",
            displayName: "Saga Choreography Eventing Bridge",
            description: "Stages saga choreography publications through the shared Cephalon.Eventing publish path.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "event-driven-integration",
                ["pattern"] = "saga-choreography",
                ["activation"] = "explicit",
                ["registration"] = "engine-managed",
                ["handoff"] = "eventing.publish",
                ["durabilityPrerequisite"] = "outbox"
            }));
    }

    private static bool HasExplicitSagaPublisherOverride(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType != typeof(ISagaChoreographyPublisher))
            {
                continue;
            }

            if (!IsInMemorySagaPublisherRegistration(descriptor))
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveInMemorySagaPublishers(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(ISagaChoreographyPublisher) &&
                IsInMemorySagaPublisherRegistration(descriptor))
            {
                services.RemoveAt(index);
            }
        }
    }

    private static bool IsInMemorySagaPublisherRegistration(ServiceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.ImplementationType == typeof(InMemorySagaChoreographyPublisher))
        {
            return true;
        }

        return descriptor.ImplementationInstance is InMemorySagaChoreographyPublisher;
    }
}
