using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class TechnologyCatalogTestModule : ModuleBase, ITechnologyContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "technology-catalog",
        displayName: "Technology Catalog",
        description: "Contributes future-facing technology profiles for testing.",
        tags: ["foundation", "technology"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "foundation",
            ["surface"] = "technology-catalog"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterTechnologies(ITechnologyRegistry technologies)
    {
        technologies.Add(new TechnologyDescriptor(
            id: "digital-twin-orchestration",
            displayName: "Digital Twin Orchestration",
            description: "Prepares the app for digital-twin coordination, telemetry-driven feedback loops, and spatial state synchronization.",
            kind: TechnologyKind.Experience,
            requiresTransports: ["websocket"],
            packageHints: ["Cephalon.DigitalTwin"],
            guidance:
            [
                "Keep device telemetry, simulation state, and orchestration decisions behind module contracts instead of hardwiring transport handlers.",
                "Prefer explicit streaming or push channels when the runtime needs to synchronize twin state continuously."
            ]));
    }
}

internal sealed class TechnologyAwareModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "technology-aware",
        displayName: "Technology Aware",
        description: "Activates services and capabilities when specific technology profiles are selected.",
        tags: ["technology"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "application",
            ["surface"] = "technology-aware"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "technology-aware.base",
            displayName: "Technology-aware baseline",
            description: "Baseline capability registered regardless of technology selection."));
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        if (!technologies.IsSelected("digital-twin-orchestration"))
        {
            return;
        }

        services.AddSingleton(new TechnologyActivationMarker("digital-twin-orchestration"));
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        if (!technologies.IsSelected("digital-twin-orchestration"))
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "technology-aware.digital-twin",
            displayName: "Digital twin activation",
            description: "Activated only when the digital-twin technology profile is selected.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "digital-twin-orchestration"
            }));
    }
}

internal sealed record TechnologyActivationMarker(string TechnologyId);
