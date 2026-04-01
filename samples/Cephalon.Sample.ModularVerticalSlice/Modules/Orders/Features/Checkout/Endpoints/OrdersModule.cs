using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Commands;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Endpoints;

/// <summary>
/// Registers the orders module for the modular vertical-slice sample.
/// </summary>
public sealed class OrdersModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "orders",
        displayName: "Orders",
        description: "Orders module organized by feature slices.",
        tags: ["sample", "vertical-slice"],
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
        services.AddSingleton<CheckoutPreviewService>();
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
            key: "orders.checkout.preview",
            displayName: "Checkout preview",
            description: "Preview a checkout flow grouped by feature slice."));
    }

    /// <summary>
    /// Maps the HTTP endpoints exposed by the orders module.
    /// </summary>
    /// <param name="endpoints">
    /// The endpoint route builder used by the ASP.NET Core host adapter.
    /// </param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders/checkout");
        group.MapGet("/preview/{customerId?}", (string? customerId, CheckoutPreviewService service) =>
            TypedResults.Ok(service.Build(customerId)));
    }
}
