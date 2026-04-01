using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Commands;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Endpoints;

public sealed class OrdersModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "orders",
        displayName: "Orders",
        description: "Orders module organized by feature slices.",
        tags: ["sample", "vertical-slice"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CheckoutPreviewService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "orders.checkout.preview",
            displayName: "Checkout preview",
            description: "Preview a checkout flow grouped by feature slice."));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders/checkout");
        group.MapGet("/preview/{customerId?}", (string? customerId, CheckoutPreviewService service) =>
            TypedResults.Ok(service.Build(customerId)));
    }
}
