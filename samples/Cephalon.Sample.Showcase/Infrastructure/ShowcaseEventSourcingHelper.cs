using Cephalon.Abstractions.EventSourcing;
using Cephalon.Sample.Showcase.Domain.Cart.Models;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Helper that rebuilds cart aggregate state from the event store.
/// </summary>
public static class ShowcaseEventSourcingHelper
{
    private static readonly ShoppingCartAggregate Aggregate = new();

    /// <summary>
    /// Rebuilds a <see cref="ShoppingCart"/> from its event stream.
    /// </summary>
    /// <param name="eventStore">The event store to read from.</param>
    /// <param name="streamId">The event stream identifier.</param>
    /// <param name="customerId">The customer identifier for the initial cart state.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The rebuilt shopping cart state.</returns>
    public static async Task<ShoppingCart> RebuildCartAsync(
        IEventStore eventStore,
        string streamId,
        string customerId,
        CancellationToken ct = default)
    {
        var cart = new ShoppingCart
        {
            CartId = streamId.StartsWith("cart-", StringComparison.Ordinal)
                ? streamId["cart-".Length..]
                : streamId,
            CustomerId = customerId
        };

        await foreach (var evt in eventStore.ReadStreamAsync(streamId, fromVersion: 0, ct))
        {
            cart = Aggregate.Apply(cart, evt);
        }

        return cart;
    }
}
