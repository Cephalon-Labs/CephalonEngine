using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Modules;

internal sealed class MultiTenancyGovernanceModule(MultiTenancyGovernanceOptions options)
    : ModuleBase, ITechnologyServiceContributor, ITechnologyCapabilityContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "multi-tenancy-governance",
        displayName: "Multi-Tenancy Governance",
        description: "Tenant membership catalog and evaluation runtime for Cephalon multi-tenancy workloads.",
        tags: ["tenant", "membership", "governance"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["technology"] = "multi-tenancy",
            ["basePackage"] = "Cephalon.MultiTenancy"
        });

    private bool hasMembershipContributors;

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MultiTenancyGovernanceDiagnosticsConventionContributor>());
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

        hasMembershipContributors = services.Any(static descriptor => descriptor.ServiceType == typeof(ITenantMembershipContributor));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILogger<TenantMembershipEvaluator>>(NullLogger<TenantMembershipEvaluator>.Instance);
        services.TryAddSingleton<ITenantMembershipCatalog, TenantMembershipCatalog>();

        if (options.EnableMembershipEvaluation)
        {
            services.TryAddSingleton<ITenantMembershipEvaluator, TenantMembershipEvaluator>();
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MultiTenancyGovernanceRuntimeSurfaceContributor>());
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
            key: "tenancy.membership.catalog",
            displayName: "Tenant Membership Catalog",
            description: "Exposes tenant membership descriptors through the multi-tenancy governance companion pack.",
            metadata: new Dictionary<string, string>
            {
                ["technology"] = "multi-tenancy",
                ["package"] = "Cephalon.MultiTenancy.Governance",
                ["ownership"] = "cephalon-managed",
                ["runtimeSurface"] = "tenant-memberships",
                ["configuredMembershipCount"] = options.Memberships.Count.ToString(CultureInfo.InvariantCulture),
                ["hasMembershipContributors"] = hasMembershipContributors.ToString().ToLowerInvariant()
            }));

        if (options.EnableMembershipEvaluation)
        {
            capabilities.Add(new Capability(
                key: "tenancy.membership.evaluation",
                displayName: "Tenant Membership Evaluation",
                description: "Evaluates whether a principal has active tenant membership and required tenant roles.",
                metadata: new Dictionary<string, string>
                {
                    ["technology"] = "multi-tenancy",
                    ["package"] = "Cephalon.MultiTenancy.Governance",
                    ["executionOwnership"] = "cephalon-managed",
                    ["runtimeSurface"] = "tenant-memberships"
                }));
        }
    }
}
