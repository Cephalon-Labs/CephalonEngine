using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;
using Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Governance.Application;
using Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Overview.Application;
using Cephalon.Sample.MicroserviceSuite.Governance.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Overview.Api;

/// <summary>
/// Registers the catalog module for the microservice-suite catalog service.
/// </summary>
public sealed class CatalogModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "catalog",
        displayName: "Catalog",
        description: "Catalog service module for the multi-service suite sample.",
        tags: ["sample", "microservice-suite", "catalog"],
        version: "1.0.0");

    /// <summary>
    /// Gets the descriptor exposed by the catalog module.
    /// </summary>
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <summary>
    /// Registers application services required by the catalog module.
    /// </summary>
    /// <param name="services">
    /// The host service collection.
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CatalogOverviewApplicationService>();
        services.AddSingleton<CatalogGovernanceApplicationService>();
    }

    /// <summary>
    /// Registers the capabilities exposed by the catalog module.
    /// </summary>
    /// <param name="capabilities">
    /// The capability registry used during module discovery.
    /// </param>
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "catalog.overview",
            displayName: "Catalog overview",
            description: "Exposes the shared suite-level catalog overview contract."));
        capabilities.Add(new Capability(
            key: "catalog.governance-guidance",
            displayName: "Catalog governance guidance",
            description: "Exposes additive gateway and control-plane guidance for the catalog service."));
    }

    /// <summary>
    /// Configures the public REST behaviors exposed by the catalog module.
    /// </summary>
    /// <param name="behaviors">
    /// The REST behavior builder used by the ASP.NET Core host adapter.
    /// </param>
    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/catalog")
            .WithTagName("Catalog API")
            .MapProfile<GetCatalogOverviewBehavior>()
            .MapProfile<GetCatalogGovernanceBehavior>();
    }
}

[AppBehavior("catalog.overview.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/overview", ApiVersionMajor = 1)]
internal sealed class GetCatalogOverviewBehavior : IAppBehavior<GetCatalogOverviewInput, Result<SuiteServiceSummaryContract>>
{
    private readonly CatalogOverviewApplicationService service;

    public GetCatalogOverviewBehavior(CatalogOverviewApplicationService service)
    {
        this.service = service;
    }

    public Task<Result<SuiteServiceSummaryContract>> HandleAsync(
        GetCatalogOverviewInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(
            service.Build(),
            message: "Catalog overview resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
    }
}

[AppBehavior("catalog.governance.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/governance", ApiVersionMajor = 1)]
internal sealed class GetCatalogGovernanceBehavior : IAppBehavior<GetCatalogGovernanceInput, Result<SuiteGovernanceSnapshotContract>>
{
    private readonly CatalogGovernanceApplicationService service;

    public GetCatalogGovernanceBehavior(CatalogGovernanceApplicationService service)
    {
        this.service = service;
    }

    public Task<Result<SuiteGovernanceSnapshotContract>> HandleAsync(
        GetCatalogGovernanceInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(
            service.Build(),
            message: "Catalog governance guidance resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
    }
}

internal sealed record GetCatalogOverviewInput();

internal sealed record GetCatalogGovernanceInput();
