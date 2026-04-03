using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Coordination.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Coordination.Api;

/// <summary>
/// Registers the orders module for the microservice-suite orders service.
/// </summary>
public sealed class OrdersModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "orders",
        displayName: "Orders",
        description: "Orders service module for the multi-service suite sample.",
        tags: ["sample", "microservice-suite", "orders"],
        version: "1.0.0");

    /// <summary>
    /// Gets the descriptor exposed by the orders module.
    /// </summary>
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <summary>
    /// Registers application services required by the orders module.
    /// </summary>
    /// <param name="services">
    /// The host service collection.
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<OrderCoordinationApplicationService>();
    }

    /// <summary>
    /// Registers the capabilities exposed by the orders module.
    /// </summary>
    /// <param name="capabilities">
    /// The capability registry used during module discovery.
    /// </param>
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "orders.coordination",
            displayName: "Orders coordination",
            description: "Exposes the shared suite-level order-coordination contract."));
    }

    /// <summary>
    /// Maps the HTTP endpoints exposed by the orders module.
    /// </summary>
    /// <param name="endpoints">
    /// The endpoint route builder used by the ASP.NET Core host adapter.
    /// </param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders");
        group.MapGet("/coordination/{orderId?}", (string? orderId, string? fulfillmentRegion, OrderCoordinationApplicationService service) =>
            TypedResults.Ok(service.Build(orderId, fulfillmentRegion)));
    }
}
