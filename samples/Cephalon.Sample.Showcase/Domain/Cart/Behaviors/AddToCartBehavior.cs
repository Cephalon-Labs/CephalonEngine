using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Cart.Behaviors;

/// <summary>
/// Adds an item to the shopping cart using the CQRS pattern with event sourcing.
/// This is the command side — it appends an <see cref="ItemAddedToCart"/> event to the
/// cart's event stream and returns the updated cart summary.
/// </summary>
[AppBehavior("cart.add-item")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.grpc")]
public sealed class AddToCartBehavior : IAppBehavior<AddToCartInput, AddToCartOutput>
{
    /// <inheritdoc />
    public async Task<AddToCartOutput> HandleAsync(
        AddToCartInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var streamId = $"cart-{input.CartId}";
        var eventStore = context.EventStore
            ?? throw new InvalidOperationException("Event store is required for the cart CQRS pattern.");

        var currentVersion = await eventStore.GetVersionAsync(streamId, ct);

        var evt = new ItemAddedToCart(
            StreamId: streamId,
            StreamVersion: currentVersion + 1,
            OccurredAtUtc: DateTime.UtcNow,
            ProductId: input.ProductId,
            ProductName: input.ProductName,
            Quantity: input.Quantity,
            PriceInCents: input.PriceInCents);

        await eventStore.AppendAsync(streamId, [evt], currentVersion, ct);

        // Rebuild cart for response
        var cart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CustomerId, ct);

        return new AddToCartOutput(input.CartId, cart.Items.Count, cart.TotalInCents);
    }
}
