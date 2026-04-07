using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Cart.Behaviors;

/// <summary>
/// Removes an item from the shopping cart using the CQRS pattern with event sourcing.
/// Appends an <see cref="ItemRemovedFromCart"/> event to the cart's stream.
/// </summary>
[AppBehavior("cart.remove-item")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.rest", "http.ws", "http.graphql", "http.sse")]
public sealed class RemoveFromCartBehavior : IAppBehavior<RemoveFromCartInput, RemoveFromCartOutput>
{
    /// <inheritdoc />
    public async Task<RemoveFromCartOutput> HandleAsync(
        RemoveFromCartInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var streamId = $"cart-{input.CartId}";
        var eventStore = context.EventStore
            ?? throw new InvalidOperationException("Event store is required for the cart CQRS pattern.");

        var currentVersion = await eventStore.GetVersionAsync(streamId, ct);
        if (currentVersion < 0)
        {
            throw new KeyNotFoundException($"Cart '{input.CartId}' was not found.");
        }

        var evt = new ItemRemovedFromCart(
            StreamId: streamId,
            StreamVersion: currentVersion + 1,
            OccurredAtUtc: DateTime.UtcNow,
            ProductId: input.ProductId);

        await eventStore.AppendAsync(streamId, [evt], currentVersion, ct);

        var cart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CartId, ct);

        return new RemoveFromCartOutput(input.CartId, cart.Items.Count, cart.TotalInCents);
    }

    /// <summary>
    /// Declares the CQRS pattern with event sourcing enabled.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsCqrs()
            .ViaHttpRest()
            .ViaWebSocket()
            .ViaHttpGraphQl()
            .ViaHttpSse()
            .WithOptions(opts => opts.EventSourcingEnabled = true);
    }
}
