using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.ModularMonolith.Modules.Catalog.Application;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.ModularMonolith.Modules.Catalog.Endpoints;

public sealed class CatalogModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "catalog",
        displayName: "Catalog",
        description: "Catalog module organized with application, domain, infrastructure, and endpoint boundaries.",
        tags: ["sample", "monolith"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CatalogOverviewService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "catalog.overview",
            displayName: "Catalog overview",
            description: "Summarizes the module-first catalog surface."));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/catalog");
        group.MapGet("/overview", (CatalogOverviewService service) => TypedResults.Ok(service.Build()));
    }
}
