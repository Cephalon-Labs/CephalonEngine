using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Configuration;
using Cephalon.Edge.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;

namespace Cephalon.Edge.Modules;

internal sealed class EdgeRuntimeModule : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "edge-runtime",
        displayName: "Edge Runtime",
        description: "Companion runtime services for edge-native delivery workloads.",
        tags: ["technology", "edge"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "technology-pack",
            ["technology"] = "edge-native-delivery"
        });

    private readonly EdgeRuntimeOptions options;
    private bool hasNodeContributors;

    public EdgeRuntimeModule(EdgeRuntimeOptions options)
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

        if (!technologies.IsSelected("edge-native-delivery"))
        {
            return;
        }

        hasNodeContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(IEdgeNodeContributor));
        services.TryAddSingleton(options);
        services.TryAddSingleton<IEdgeNodeCatalog, EdgeNodeCatalog>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EdgeRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("edge-native-delivery"))
        {
            return;
        }

        if (options.EnableOfflineMode)
        {
            capabilities.Add(new Capability(
                key: "edge.offline",
                displayName: "Offline Mode",
                description: "Supports degraded or partially connected execution at the edge.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "edge-native-delivery"
                }));
        }

        if (options.EnableSynchronization)
        {
            capabilities.Add(new Capability(
                key: "edge.sync",
                displayName: "Edge Synchronization",
                description: "Supports delayed or eventual synchronization between edge and central runtime components.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "edge-native-delivery"
                }));
        }

        if (options.Nodes.Count > 0 || hasNodeContributors)
        {
            capabilities.Add(new Capability(
                key: "edge.nodes",
                displayName: "Edge Nodes",
                description: "Exposes configured edge node descriptors to the runtime.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "edge-native-delivery",
                    ["nodeCount"] = options.Nodes.Count.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }
}
