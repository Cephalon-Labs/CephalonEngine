using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Tenancy;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Configuration;
using Cephalon.MultiTenancy.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.MultiTenancy.Modules;

internal sealed class MultiTenancyModule(Action<MultiTenancyRuntimeOptions>? configureOptions)
    : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "multi-tenancy",
        displayName: "Multi-Tenancy",
        description: "Host-agnostic tenant resolution and ambient tenant-context baseline for Cephalon runtimes.",
        tags: ["tenant", "tenancy", "platform"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["technology"] = "multi-tenancy"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var options = MultiTenancyRuntimeOptions.FromConfiguration(configuration);
            configureOptions?.Invoke(options);
            return options;
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MultiTenancyDiagnosticsConventionContributor>());
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("multi-tenancy"))
        {
            return;
        }

        services.TryAddSingleton<ILogger<ConfiguredTenantResolver>>(NullLogger<ConfiguredTenantResolver>.Instance);
        services.TryAddSingleton<ILogger<DisabledTenantResolver>>(NullLogger<DisabledTenantResolver>.Instance);
        services.TryAddSingleton<AmbientTenantContextAccessor>();

        services.TryAddSingleton<ITenantContextAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<AmbientTenantContextAccessor>());
        services.TryAddSingleton<ITenantResolver>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<MultiTenancyRuntimeOptions>();
            return options.EnableDefaultResolver
                ? ActivatorUtilities.CreateInstance<ConfiguredTenantResolver>(serviceProvider)
                : ActivatorUtilities.CreateInstance<DisabledTenantResolver>(serviceProvider);
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("multi-tenancy"))
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "tenancy.resolution",
            displayName: "Tenant Resolution",
            description: "Resolves tenant context through the active Cephalon multi-tenancy companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["resolver"] = "configuration-driven",
                ["runtimeSurface"] = "tenant-resolution",
                ["configurationSection"] = "Engine:Tenancy"
            }));
    }
}
