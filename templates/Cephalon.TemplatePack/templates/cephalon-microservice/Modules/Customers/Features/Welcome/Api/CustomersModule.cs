using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using CephalonTemplateApp.Modules.Customers.Features.Welcome.Application;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateApp.Modules.Customers.Features.Welcome.Api;

public sealed class CustomersModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "customers",
        displayName: "Customers",
        description: "Customer-facing service module with explicit contracts.",
        tags: ["generated", "microservice"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WelcomeApplicationService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "customers.welcome",
            displayName: "Customer welcome",
            description: "Exposes a service-boundary welcome contract."));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/customers");
        group.MapGet("/welcome/{name?}", (string? name, string? tenant, WelcomeApplicationService service) =>
            TypedResults.Ok(service.Build(name, tenant)));
    }
}
