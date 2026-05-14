#pragma warning disable MA0048 // Starter keeps small generated helper types beside the module for adoption clarity.

using System.Threading;
using System.Threading.Tasks;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using CephalonTemplateApp.Modules.Orders.Features.Checkout.Commands;
using CephalonTemplateApp.Modules.Orders.Features.Checkout.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateApp.Modules.Orders.Features.Checkout.Endpoints;

public sealed class OrdersModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "orders",
        displayName: "Orders",
        description: "Orders module organized by feature slices.",
        tags: ["generated", "vertical-slice"],
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
    }
}

internal sealed record GetCheckoutPreviewInput(string? CustomerId = null);
