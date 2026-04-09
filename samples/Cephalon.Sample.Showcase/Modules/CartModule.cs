using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Sample.Showcase.Domain.Cart.Behaviors;
using Microsoft.AspNetCore.Routing;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the shopping cart bounded context module.
/// Implements the CQRS behavior pattern with event sourcing; cart state
/// is rebuilt entirely from the domain event stream.
/// Exposes REST endpoints through behavior-aware Minimal API helpers so the
/// module keeps transport mapping aligned with the shipped CQRS behaviors.
/// </summary>
public sealed class CartModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.cart",
        displayName: "Showcase Cart",
        description: "Shopping cart module using the CQRS behavior pattern with event sourcing.",
        tags: ["showcase", "cart", "cqrs-pattern", "event-sourcing"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.cart.cqrs",
            displayName: "Cart CQRS",
            description: "CQRS command/query handling for the shopping cart aggregate."));
        capabilities.Add(new Capability(
            key: "showcase.cart.event-sourcing",
            displayName: "Cart event sourcing",
            description: "Event-sourced cart state with full event stream replay."));
    }

    /// <inheritdoc />
    public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        behaviors.Add<GetCartBehavior>();
        behaviors.Add<AddToCartBehavior>();
        behaviors.Add<RemoveFromCartBehavior>();
        behaviors.Add<CheckoutCartBehavior>();
    }

    /// <inheritdoc />
    public override void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/cart");
        group.MapBehaviorGet<GetCartBehavior>("/{cartId}");
        group.MapBehaviorPost<AddToCartBehavior>("/{cartId}/items");
        group.MapBehaviorDelete<RemoveFromCartBehavior>("/{cartId}/items/{productId}");
        group.MapBehaviorPost<CheckoutCartBehavior>("/{cartId}/checkout");
    }
}
