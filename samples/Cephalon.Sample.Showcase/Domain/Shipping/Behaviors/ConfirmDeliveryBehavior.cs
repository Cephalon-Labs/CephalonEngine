using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Shipping.Behaviors;

/// <summary>
/// Confirms shipment delivery using the process-manager pattern.
/// This is the terminal step — the process manager deletes the checkpoint on completion.
/// Implements <see cref="IProcessCompletion"/> to signal process termination.
/// </summary>
[AppBehavior("shipping.confirm-delivery")]
[BehaviorAllowedPatterns("process-manager")]
[BehaviorAllowedTransports("kafka", "rabbitmq", "in-memory", "http.rest")]
public sealed class ConfirmDeliveryBehavior : IAppBehavior<ConfirmDeliveryInput, ConfirmDeliveryOutput>,
    IProcessCompletion
{
    /// <inheritdoc />
    public Task<ConfirmDeliveryOutput> HandleAsync(
        ConfirmDeliveryInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        if (!ShowcaseDataStore.Shipments.TryGetValue(input.ShipmentId, out var shipment))
        {
            throw new InvalidOperationException($"Shipment '{input.ShipmentId}' not found.");
        }

        shipment.Status = ShipmentStatus.Delivered;
        shipment.DeliveredAtUtc = DateTime.UtcNow;

        // Update the corresponding order status if it exists
        if (ShowcaseDataStore.Orders.TryGetValue(shipment.OrderId, out var order))
        {
            order.Status = Orders.Models.OrderStatus.Delivered;
            order.UpdatedAtUtc = DateTime.UtcNow;
        }

        return Task.FromResult(new ConfirmDeliveryOutput(
            input.ShipmentId,
            "Delivered",
            shipment.DeliveredAtUtc.Value));
    }

    /// <summary>
    /// Declares the process-manager pattern with multi-transport exposure.
    /// This is the terminal step — <see cref="IProcessCompletion"/> signals checkpoint deletion.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsProcessManager()
            .ViaKafka()
            .ViaRabbitMq()
            .ViaInMemory()
            .ViaHttpRest();
    }
}
