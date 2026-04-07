using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Cart.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Cart.Behaviors;

/// <summary>
/// Retrieves the current shopping cart state using the CQRS query side.
/// Rebuilds the cart from its event stream on every read.
/// </summary>
[AppBehavior("cart.get")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.rest", "http.ws", "http.graphql", "http.sse")]
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

        var cart = await ShowcaseEventSourcingHelper.RebuildCartAsync(eventStore, streamId, input.CartId, ct);

        return new GetCartOutput(cart);
    }

    /// <summary>
    /// Declares the CQRS pattern with event sourcing for the read side.
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
