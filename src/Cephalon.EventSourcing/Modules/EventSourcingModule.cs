using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.EventSourcing.Configuration;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.EventSourcing.Modules;

internal sealed class EventSourcingModule(Action<EventSourcingOptions>? configureOptions)
    : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "event-sourcing",
        displayName: "Event Sourcing",
        description: "Runtime-neutral event-sourcing baseline for Cephalon runtimes.",
        tags: ["event-sourcing", "events", "runtime"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "event-sourcing"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EventSourcingOptions();
        configureOptions?.Invoke(options);
        EventSourcingServiceRegistration.Register(services, options);
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}
