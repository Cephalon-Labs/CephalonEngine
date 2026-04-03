using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Overview.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.MicroserviceSuite.CatalogService.Modules.Catalog.Features.Overview.Api;

/// <summary>
/// Registers the catalog module for the microservice-suite catalog service.
/// </summary>
public sealed class CatalogModule : ModuleBase, IEndpointModule
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
    }

    /// <summary>
    /// Maps the HTTP endpoints exposed by the catalog module.
    /// </summary>
    /// <param name="endpoints">
    /// The endpoint route builder used by the ASP.NET Core host adapter.
    /// </param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/catalog");
        group.MapGet("/overview", (CatalogOverviewApplicationService service) =>
            TypedResults.Ok(service.Build()));
    }
}
