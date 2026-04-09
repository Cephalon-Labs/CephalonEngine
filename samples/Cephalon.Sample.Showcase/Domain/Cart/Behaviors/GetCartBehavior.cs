using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Cart.Behaviors;

/// <summary>
/// Retrieve the current shopping cart state.
/// </summary>
/// <remarks>
/// Uses the CQRS query side.
/// Rebuilds the cart from the event stream on every read.
/// </remarks>
[AppBehavior("cart.get")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.ws", "http.graphql", "http.sse")]
public sealed class GetCartBehavior : IAppBehavior<GetCartInput, GetCartOutput>
{
    /// <inheritdoc />
    public async Task<GetCartOutput> HandleAsync(
        GetCartInput input,
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

        var cart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CartId, ct);

        return new GetCartOutput(cart);
    }
}
