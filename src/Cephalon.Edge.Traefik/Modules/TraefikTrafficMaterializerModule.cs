using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;
using Cephalon.Edge.Traefik.Services;
using Cephalon.Engine.Technologies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Edge.Traefik.Modules;

internal sealed class TraefikTrafficMaterializerModule(TraefikTrafficMaterializerOptions options)
    : ModuleBase, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "edge-traefik-traffic-materializer",
        displayName: "Traefik Traffic Materializer",
        description: "Projects provider-managed cell traffic automation into Traefik IngressRoute intent.",
        tags: ["technology", "edge", "traefik", "ingressroute"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["layer"] = "technology-pack",
            ["technology"] = "cell-based-architecture",
            ["provider"] = TraefikTrafficMaterializerOptions.DefaultProviderId
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

        services.TryAddSingleton(_ => new TraefikTrafficProjectionCatalog(options));
        var controlPlaneMode = TraefikTrafficObservationModes.Normalize(options.Observation.Mode);
        if (string.Equals(
                controlPlaneMode,
                TraefikTrafficObservationModes.ObserveOnly,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                controlPlaneMode,
                TraefikTrafficObservationModes.ApplyAndReconcile,
                StringComparison.OrdinalIgnoreCase))
        {
            services.TryAddSingleton<TraefikTrafficObservationSource>(serviceProvider =>
                new TraefikTrafficObservationSource(
                    options,
                    serviceProvider.GetRequiredService<TimeProvider>(),
                    serviceProvider.GetService<k8s.IKubernetes>()));
            services.TryAddSingleton<ITraefikTrafficObservationSource>(serviceProvider =>
                    serviceProvider.GetRequiredService<TraefikTrafficObservationSource>());
            services.TryAddSingleton<ITraefikTrafficApplyService>(serviceProvider =>
                serviceProvider.GetRequiredService<TraefikTrafficObservationSource>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, TraefikTrafficObservationHostedService>());
        }

        services.AddSingleton<TraefikTrafficAutomationMaterializer>(serviceProvider =>
            new TraefikTrafficAutomationMaterializer(
                serviceProvider.GetRequiredService<TraefikTrafficProjectionCatalog>(),
                options,
                serviceProvider.GetService<ITraefikTrafficObservationSource>(),
                serviceProvider.GetService<ITraefikTrafficApplyService>()));
        services.AddSingleton<ICellTrafficAutomationProviderMaterializer>(serviceProvider =>
            serviceProvider.GetRequiredService<TraefikTrafficAutomationMaterializer>());
        services.AddSingleton<ITechnologyRuntimeContributor>(
            serviceProvider => new TraefikTrafficMaterializationRuntimeContributor(
                serviceProvider.GetRequiredService<TraefikTrafficProjectionCatalog>(),
                options,
                serviceProvider.GetRequiredService<ICellTrafficAutomationRuntimeCatalog>()));
    }
}
