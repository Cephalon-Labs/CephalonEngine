using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Commands;
using Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Endpoints;

/// <summary>
/// Registers the orders module for the modular vertical-slice sample.
/// </summary>
public sealed class OrdersModule : RestBehaviorModuleBase
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
    /// Configures the public REST behaviors exposed by the orders module.
    /// </summary>
    /// <param name="behaviors">
    /// The REST behavior builder used by the ASP.NET Core host adapter.
    /// </param>
    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/orders/checkout")
            .WithTagName("Orders API")
            .MapProfile<GetCheckoutPreviewBehavior>();
    }
}

[AppBehavior("orders.checkout.preview.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/preview/{customerId?}", ApiVersionMajor = 1)]
internal sealed class GetCheckoutPreviewBehavior : IAppBehavior<GetCheckoutPreviewInput, Result<CheckoutPreviewEnvelope>>
{
    private readonly CheckoutPreviewService service;

    public GetCheckoutPreviewBehavior(CheckoutPreviewService service)
    {
        this.service = service;
    }

    public Task<Result<CheckoutPreviewEnvelope>> HandleAsync(
        GetCheckoutPreviewInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(
            service.Build(input.CustomerId),
            message: "Checkout preview resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
        builder.ViaGrpc();
    }
}

internal sealed record GetCheckoutPreviewInput(string? CustomerId = null);
