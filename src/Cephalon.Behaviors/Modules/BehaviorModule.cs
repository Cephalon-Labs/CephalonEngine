using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Rules;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Modules;

/// <summary>Registers the behavior topology capabilities for Cephalon runtimes.</summary>
internal sealed class BehaviorModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "behaviors",
        displayName: "Adaptive Behavior Topology",
        description: "Config-driven behavior topology (ABT) baseline for Cephalon runtimes.",
        tags: ["behaviors", "topology", "abt", "runtime"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "behaviors"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IBehaviorCatalog, BehaviorCatalog>();
        services.AddSingleton<BehaviorDispatcher>();
        services.AddSingleton<CompatibilityMatrix>();
        services.AddSingleton<IBehaviorCompatibilityRule, Abt001SagaRequiresStatefulTransportRule>();
        services.AddSingleton<IBehaviorCompatibilityRule, Abt002EventDrivenWithHttpRestRule>();
        services.AddSingleton<IBehaviorCompatibilityRule, Abt003ProcessManagerRequiresInboxRule>();
        services.AddSingleton<IBehaviorCompatibilityRule, Abt004CqrsMultipleTransportsRule>();
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}
