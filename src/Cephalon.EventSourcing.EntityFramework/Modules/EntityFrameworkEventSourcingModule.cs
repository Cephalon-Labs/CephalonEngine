using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.EventSourcing.EntityFramework.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.EventSourcing.EntityFramework.Modules;

internal sealed class EntityFrameworkEventSourcingModule<TContext> : ModuleBase
    where TContext : DbContext, IEntityFrameworkEventContext
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "entity-framework-event-sourcing",
        displayName: "Entity Framework Event Sourcing",
        description: "Entity Framework Core provider for the Cephalon event-sourcing baseline.",
        tags: ["event-sourcing", "entity-framework", "provider"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "entity-framework-event-store"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCephalonEntityFrameworkEventSourcing<TContext>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}
