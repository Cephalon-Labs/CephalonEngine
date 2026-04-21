using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;
using Cephalon.Edge.KubernetesGateway.Services;
using Cephalon.Engine.Technologies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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

        services.TryAddSingleton(_ => new KubernetesGatewayTrafficProjectionCatalog(options));
        if (string.Equals(
                KubernetesGatewayTrafficObservationModes.Normalize(options.Observation.Mode),
                KubernetesGatewayTrafficObservationModes.ObserveOnly,
                StringComparison.OrdinalIgnoreCase))
        {
            services.TryAddSingleton<IKubernetesGatewayTrafficObservationSource>(serviceProvider =>
                new KubernetesGatewayTrafficObservationSource(
                    options,
                    serviceProvider.GetRequiredService<TimeProvider>(),
                    serviceProvider.GetService<k8s.IKubernetes>()));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KubernetesGatewayTrafficObservationHostedService>());
        }

        services.AddSingleton(serviceProvider =>
            new KubernetesGatewayTrafficAutomationMaterializer(
                serviceProvider.GetRequiredService<KubernetesGatewayTrafficProjectionCatalog>(),
                options,
                serviceProvider.GetService<IKubernetesGatewayTrafficObservationSource>()));
        services.AddSingleton<ICellTrafficAutomationProviderMaterializer>(serviceProvider =>
            serviceProvider.GetRequiredService<KubernetesGatewayTrafficAutomationMaterializer>());
        services.AddSingleton<ITechnologyRuntimeContributor>(
            serviceProvider => new KubernetesGatewayTrafficMaterializationRuntimeContributor(
                serviceProvider.GetRequiredService<KubernetesGatewayTrafficProjectionCatalog>(),
                options,
                serviceProvider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>()));
    }
}
