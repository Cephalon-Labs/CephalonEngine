#pragma warning disable MA0048 // Starter keeps small generated helper types beside the module for adoption clarity.

using System.Threading;
using System.Threading.Tasks;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using CephalonTemplateApp.Modules.Catalog.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateApp.Modules.Catalog.Endpoints;

public sealed class CatalogModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "catalog",
        displayName: "Catalog",
        description: "Catalog module organized with application, domain, infrastructure, and endpoint boundaries.",
        tags: ["generated", "monolith"],
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
            description: "Summarizes the generated module-first catalog surface."));
    }

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
