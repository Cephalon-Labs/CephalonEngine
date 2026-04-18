using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;
using Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Coordination.Application;
using Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Governance.Application;
using Cephalon.Sample.MicroserviceSuite.Governance.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.MicroserviceSuite.OrdersService.Modules.Orders.Features.Coordination.Api;

/// <summary>
/// Registers the orders module for the microservice-suite orders service.
/// </summary>
public sealed class OrdersModule : RestBehaviorModuleBase
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
        services.AddSingleton<OrdersGovernanceApplicationService>();
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
        capabilities.Add(new Capability(
            key: "orders.governance-guidance",
            displayName: "Orders governance guidance",
            description: "Exposes additive gateway and control-plane guidance for the orders service."));
    }

    /// <summary>
    /// Configures the public REST behaviors exposed by the orders module.
    /// </summary>
    /// <param name="behaviors">
    /// The REST behavior builder used by the ASP.NET Core host adapter.
    /// </param>
    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/orders")
            .WithTagName("Orders API")
            .MapProfile<GetOrderCoordinationBehavior>()
            .MapProfile<GetOrdersGovernanceBehavior>();
    }
}

[AppBehavior("orders.coordination.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/coordination/{orderId?}", ApiVersionMajor = 1)]
internal sealed class GetOrderCoordinationBehavior : IAppBehavior<GetOrderCoordinationInput, Result<SuiteServiceSummaryContract>>
{
    private readonly OrderCoordinationApplicationService service;

    public GetOrderCoordinationBehavior(OrderCoordinationApplicationService service)
    {
        this.service = service;
    }

    public Task<Result<SuiteServiceSummaryContract>> HandleAsync(
        GetOrderCoordinationInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(
            service.Build(input.OrderId, input.FulfillmentRegion),
            message: "Order coordination resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
    }
}

[AppBehavior("orders.governance.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/governance", ApiVersionMajor = 1)]
internal sealed class GetOrdersGovernanceBehavior : IAppBehavior<GetOrdersGovernanceInput, Result<SuiteGovernanceSnapshotContract>>
{
    private readonly OrdersGovernanceApplicationService service;

    public GetOrdersGovernanceBehavior(OrdersGovernanceApplicationService service)
    {
        this.service = service;
    }

    public Task<Result<SuiteGovernanceSnapshotContract>> HandleAsync(
        GetOrdersGovernanceInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(
            service.Build(),
            message: "Orders governance guidance resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
    }
}

internal sealed record GetOrderCoordinationInput(string? OrderId = null, string? FulfillmentRegion = null);

internal sealed record GetOrdersGovernanceInput();
