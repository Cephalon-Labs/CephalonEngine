using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.ModularMonolith.Modules.Catalog.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.ModularMonolith.Modules.Catalog.Endpoints;

/// <summary>
/// Registers the catalog module for the modular monolith sample.
/// </summary>
public sealed class CatalogModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "catalog",
        displayName: "Catalog",
        description: "Catalog module organized with application, domain, infrastructure, and endpoint boundaries.",
        tags: ["sample", "monolith"],
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
        services.AddSingleton<CatalogOverviewService>();
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
            description: "Summarizes the module-first catalog surface."));
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
            .MapProfile<GetCatalogOverviewBehavior>();
    }
}

[AppBehavior("catalog.overview.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/overview", ApiVersionMajor = 1)]
internal sealed class GetCatalogOverviewBehavior : IAppBehavior<GetCatalogOverviewInput, Result<CatalogOverviewEnvelope>>
{
    private readonly CatalogOverviewService service;

    public GetCatalogOverviewBehavior(CatalogOverviewService service)
    {
        this.service = service;
    }

    public Task<Result<CatalogOverviewEnvelope>> HandleAsync(
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

internal sealed record GetCatalogOverviewInput();
