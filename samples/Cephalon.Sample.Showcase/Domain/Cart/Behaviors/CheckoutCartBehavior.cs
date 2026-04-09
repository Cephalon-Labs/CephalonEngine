using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Cart.Behaviors;

/// <summary>
/// Checks out a shopping cart using the CQRS command side.
/// Appends a <see cref="CartCheckedOut"/> event and locks the cart from further modifications.
/// </summary>
[AppBehavior("cart.checkout")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.graphql")]
public sealed class CheckoutCartBehavior : IAppBehavior<CheckoutCartInput, CheckoutCartOutput>
{
    /// <inheritdoc />
    public async Task<CheckoutCartOutput> HandleAsync(
        CheckoutCartInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var streamId = $"cart-{input.CartId}";
        var eventStore = context.EventStore
            ?? throw new InvalidOperationException("Event store is required for the cart CQRS pattern.");

        if (await eventStore.GetVersionAsync(streamId, ct).ConfigureAwait(false) < 0)
        {
            throw new KeyNotFoundException($"Cart '{input.CartId}' was not found.");
        }

        // Rebuild to get current state and validate
        var cart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CartId, ct);

        if (cart.IsCheckedOut)
        {
            throw new InvalidOperationException($"Cart '{input.CartId}' has already been checked out.");
        }

        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException($"Cart '{input.CartId}' is empty and cannot be checked out.");
        }

        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        var evt = new CartCheckedOut(
            StreamId: streamId,
            StreamVersion: cart.Version + 1,
            OccurredAtUtc: DateTime.UtcNow,
            OrderId: orderId);

        await eventStore.AppendAsync(streamId, [evt], cart.Version, ct);

        return new CheckoutCartOutput(orderId, cart.TotalInCents, cart.Items.Count);
    }
}
