using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Orders.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Orders.Behaviors;

/// <summary>
/// Cancels an existing order using the event-driven pattern.
/// Publishes a cancellation event for downstream compensation (inventory release).
/// </summary>
[AppBehavior("orders.cancel")]
[BehaviorAllowedPatterns("event-driven")]
[BehaviorAllowedTransports("rabbitmq", "kafka", "http.sse")]
public sealed class CancelOrderBehavior : IAppBehavior<CancelOrderInput, CancelOrderOutput>
{
    /// <inheritdoc />
    public Task<CancelOrderOutput> HandleAsync(
        CancelOrderInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        if (!ShowcaseDataStore.Orders.TryGetValue(input.OrderId, out var order))
        {
            throw new InvalidOperationException($"Order '{input.OrderId}' not found.");
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                $"Order '{input.OrderId}' cannot be cancelled in status '{order.Status}'.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = input.Reason;
        order.UpdatedAtUtc = DateTime.UtcNow;

        return Task.FromResult(new CancelOrderOutput(input.OrderId, "Cancelled"));
    }

    /// <summary>
    /// Declares the event-driven pattern with messaging and streaming transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsEventDriven()
            .ViaRabbitMq()
            .ViaKafka()
            .ViaHttpSse()
            .WithOptions(opts => opts.OutboxEnabled = true);
    }
}
