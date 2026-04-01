using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Application;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Microservice.Modules.Customers.Features.Welcome.Api;

/// <summary>
/// Registers the customers module for the microservice sample.
/// </summary>
public sealed class CustomersModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "customers",
        displayName: "Customers",
        description: "Customer-facing service module with explicit contracts.",
        tags: ["sample", "microservice"],
        version: "1.0.0");

    /// <summary>
    /// Gets the descriptor exposed by the customers module.
    /// </summary>
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <summary>
    /// Registers application services required by the customers module.
    /// </summary>
    /// <param name="services">
    /// The host service collection.
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WelcomeApplicationService>();
    }

    /// <summary>
    /// Registers the capabilities exposed by the customers module.
    /// </summary>
    /// <param name="capabilities">
    /// The capability registry used during module discovery.
    /// </param>
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "customers.welcome",
            displayName: "Customer welcome",
            description: "Exposes a service-boundary welcome contract."));
    }

    /// <summary>
    /// Maps the HTTP endpoints exposed by the customers module.
    /// </summary>
    /// <param name="endpoints">
    /// The endpoint route builder used by the ASP.NET Core host adapter.
    /// </param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/customers");
        group.MapGet("/welcome/{name?}", (string? name, string? tenant, WelcomeApplicationService service) =>
            TypedResults.Ok(service.Build(name, tenant)));
    }
}
