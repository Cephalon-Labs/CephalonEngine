using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the shopping cart bounded context module.
/// Implements the CQRS behavior pattern with event sourcing — cart state
/// is rebuilt entirely from the domain event stream.
/// Exposes REST endpoints for direct cart manipulation; the CQRS behaviors
/// handle the event-sourced command/query flow separately.
/// </summary>
public sealed class CartModule : ModuleBase, IEndpointModule
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
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/showcase/cart");

        group.MapGet("/{cartId}", (string cartId) =>
        {
            return ShowcaseDataStore.Carts.TryGetValue(cartId, out var cart)
                ? Results.Ok(new GetCartOutput(cart))
                : Results.NotFound();
        });

        group.MapPost("/{cartId}/items", (string cartId, AddToCartInput input) =>
        {
            var cart = ShowcaseDataStore.Carts.GetOrAdd(cartId, _ => new ShoppingCart
            {
                CartId = cartId,
                CustomerId = input.CustomerId
            });

            if (cart.IsCheckedOut)
                return Results.BadRequest($"Cart '{cartId}' has already been checked out.");

            cart.Items[input.ProductId] = new CartItem(
                input.ProductId, input.ProductName, input.Quantity, input.PriceInCents);

            return Results.Ok(new AddToCartOutput(cartId, cart.Items.Count, cart.TotalInCents));
        });

        group.MapDelete("/{cartId}/items/{productId}", (string cartId, string productId) =>
        {
            if (!ShowcaseDataStore.Carts.TryGetValue(cartId, out var cart))
                return Results.NotFound();

            if (cart.IsCheckedOut)
                return Results.BadRequest($"Cart '{cartId}' has already been checked out.");

            cart.Items.Remove(productId);
            return Results.Ok(new RemoveFromCartOutput(cartId, cart.Items.Count, cart.TotalInCents));
        });

        group.MapPost("/{cartId}/checkout", (string cartId, CheckoutCartInput input) =>
        {
            if (!ShowcaseDataStore.Carts.TryGetValue(cartId, out var cart))
                return Results.NotFound();

            if (cart.IsCheckedOut)
                return Results.BadRequest($"Cart '{cartId}' has already been checked out.");

            if (cart.Items.Count == 0)
                return Results.BadRequest("Cannot checkout an empty cart.");

            cart.IsCheckedOut = true;
            var orderId = $"ord-{Guid.NewGuid():N}"[..16];

            return Results.Ok(new CheckoutCartOutput(orderId, cart.TotalInCents, cart.Items.Count));
        });
    }
}
