using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Orders.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Orders.Behaviors;

/// <summary>
/// Retrieves order status using the event-driven pattern.
/// This read-side view is exposed through streaming-oriented transports while REST stays module-owned.
/// </summary>
[AppBehavior("orders.get-status")]
[BehaviorAllowedPatterns("event-driven")]
[BehaviorAllowedTransports("http.graphql-ws", "http.sse", "http.graphql-sse")]
public sealed class GetOrderStatusBehavior : IAppBehavior<GetOrderStatusInput, GetOrderStatusOutput?>
{
    /// <inheritdoc />
    public Task<GetOrderStatusOutput?> HandleAsync(
        GetOrderStatusInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        if (!ShowcaseDataStore.Orders.TryGetValue(input.OrderId, out var order))
        {
            return Task.FromResult<GetOrderStatusOutput?>(null);
        }

        var output = new GetOrderStatusOutput(
            OrderId: order.OrderId,
            CustomerId: order.CustomerId,
            Status: order.Status.ToString(),
            TotalInCents: order.TotalInCents,
            ItemCount: order.Items.Count,
            PlacedAtUtc: order.PlacedAtUtc);

        return Task.FromResult<GetOrderStatusOutput?>(output);
    }

    /// <summary>
    /// Declares the event-driven pattern with streaming transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsEventDriven()
            .ViaHttpGraphQlWs()
            .ViaHttpSse()
            .ViaHttpGraphQlSse();
    }
}
