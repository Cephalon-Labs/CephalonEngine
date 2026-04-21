using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;
using Cephalon.Edge.KubernetesGateway.Services;
using Cephalon.Engine.Technologies;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Edge.KubernetesGateway.Modules;

internal sealed class KubernetesGatewayTrafficMaterializerModule(KubernetesGatewayTrafficMaterializerOptions options)
    : ModuleBase, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "edge-kubernetes-gateway-traffic-materializer",
        displayName: "Kubernetes Gateway Traffic Materializer",
        description: "Projects provider-managed cell traffic automation into Kubernetes Gateway API intent.",
        tags: ["technology", "edge", "kubernetes", "gateway-api"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["layer"] = "technology-pack",
            ["technology"] = "cell-based-architecture",
            ["provider"] = KubernetesGatewayTrafficMaterializerOptions.DefaultProviderId
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected(BuiltInTechnologies.CellBasedArchitecture.Id))
        {
            return;
        }

        services.AddSingleton<ICellTrafficAutomationProviderMaterializer>(
            _ => new KubernetesGatewayTrafficAutomationMaterializer(options));
        services.AddSingleton<ITechnologyRuntimeContributor>(
            serviceProvider => new KubernetesGatewayTrafficMaterializationRuntimeContributor(
                options,
                serviceProvider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>()));
    }
}
