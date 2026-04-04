using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Identity.Configuration;
using Cephalon.Identity.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Identity.Modules;

internal sealed class IdentityModule(Action<IdentityRuntimeOptions>? configureOptions)
    : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "identity-access",
        displayName: "Identity Access",
        description: "Host-agnostic identity and authorization baseline for Cephalon runtimes.",
        tags: ["identity", "authorization", "security"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["technology"] = "identity-access"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var options = IdentityRuntimeOptions.FromConfiguration(configuration);
            configureOptions?.Invoke(options);
            return options;
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, IdentityDiagnosticsConventionContributor>());
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("identity-access"))
        {
            return;
        }

        services.TryAddSingleton<ILogger<MetadataDrivenAuthorizationEvaluator>>(NullLogger<MetadataDrivenAuthorizationEvaluator>.Instance);

        services.TryAddSingleton<Cephalon.Abstractions.Authorization.IAuthorizationEvaluator, MetadataDrivenAuthorizationEvaluator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, IdentityAuthorizationRuntimeSurfaceContributor>());
    }

    public void RegisterTechnologyCapabilities(ICapabilityRegistry capabilities, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("identity-access"))
        {
            return;
        }

        capabilities.Add(new Capability(
            key: "identity.authorization",
            displayName: "Identity Authorization",
            description: "Evaluates Cephalon authorization policies through the active identity companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "identity-access",
                ["evaluation"] = "configuration-driven",
                ["defaultEvaluator"] = "configuration-driven",
                ["runtimeSurface"] = "identity-authorization",
                ["configurationSection"] = "Engine:Identity"
            }));
    }
}
