using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Shipping.Behaviors;

/// <summary>
/// Confirms shipment delivery using the process-manager pattern.
/// This is the terminal step and completes the tracked process.
/// </summary>
[AppBehavior("shipping.confirm-delivery")]
[BehaviorAllowedPatterns("process-manager")]
[BehaviorAllowedTransports("kafka", "rabbitmq", "in-memory")]
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
    /// Declares the process-manager pattern with messaging transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsProcessManager()
            .ViaKafka()
            .ViaRabbitMq()
            .ViaInMemory();
    }
}
