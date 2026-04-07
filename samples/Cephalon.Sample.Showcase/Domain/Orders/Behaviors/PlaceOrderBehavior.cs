using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Orders.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Orders.Behaviors;

/// <summary>
/// Places a new order using the event-driven pattern.
/// The behavior fires and forgets — the order is created asynchronously and downstream
/// processing (inventory reservation, shipping) is triggered via messaging.
/// </summary>
[AppBehavior("orders.place")]
[BehaviorAllowedPatterns("event-driven")]
[BehaviorAllowedTransports("rabbitmq", "kafka", "http.graphql-ws", "http.sse", "http.graphql-sse")]
public sealed class PlaceOrderBehavior : IAppBehavior<PlaceOrderInput, PlaceOrderOutput>
{
    /// <inheritdoc />
    public Task<PlaceOrderOutput> HandleAsync(
        PlaceOrderInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var orderId = $"ord-{Guid.NewGuid():N}"[..16];

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = input.CustomerId,
            ShippingAddress = input.ShippingAddress,
            TenantId = context.Metadata.GetValueOrDefault("tenantId"),
            Status = OrderStatus.Pending,
            Items = input.Items.Select(i => new OrderLineItem(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPriceInCents)).ToList(),
            TotalInCents = input.Items.Sum(i => (long)i.Quantity * i.UnitPriceInCents),
            PlacedAtUtc = DateTime.UtcNow
        };

        ShowcaseDataStore.Orders[orderId] = order;

        return Task.FromResult(new PlaceOrderOutput(orderId, "Pending"));
    }

    /// <summary>
    /// Declares the event-driven pattern with messaging and streaming transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsEventDriven()
            .ViaRabbitMq()
            .ViaKafka()
            .ViaHttpGraphQlWs()
            .ViaHttpSse()
            .ViaHttpGraphQlSse()
            .WithOptions(opts => opts.OutboxEnabled = true);
    }
}
