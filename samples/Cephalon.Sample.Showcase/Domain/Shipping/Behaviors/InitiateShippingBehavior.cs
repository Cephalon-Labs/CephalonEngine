using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Shipping.Behaviors;

/// <summary>
/// Initiates a new shipment using the process-manager pattern.
/// The process manager runtime creates a checkpoint tracking the shipment through its
/// lifecycle steps: initiated -> label created -> in transit -> delivered.
/// </summary>
[AppBehavior("shipping.initiate")]
[BehaviorAllowedPatterns("process-manager")]
[BehaviorAllowedTransports("kafka", "rabbitmq", "in-memory", "http.rest", "grpc")]
public sealed class InitiateShippingBehavior : IAppBehavior<InitiateShippingInput, InitiateShippingOutput>
{
    /// <inheritdoc />
    public Task<InitiateShippingOutput> HandleAsync(
        InitiateShippingInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var shipmentId = $"shp-{Guid.NewGuid():N}"[..16];
        var estimatedDelivery = DateTime.UtcNow.AddDays(3);

        var shipment = new Shipment
        {
            ShipmentId = shipmentId,
            OrderId = input.OrderId,
            DestinationAddress = input.DestinationAddress,
            Carrier = "Showcase Express",
            TrackingNumber = $"TRK-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Status = ShipmentStatus.LabelCreated,
            EstimatedDeliveryUtc = estimatedDelivery,
            CreatedAtUtc = DateTime.UtcNow
        };

        ShowcaseDataStore.Shipments[shipmentId] = shipment;

        return Task.FromResult(new InitiateShippingOutput(
            shipmentId, shipment.Status.ToString(), estimatedDelivery));
    }

    /// <summary>
    /// Declares the process-manager pattern with multi-transport exposure.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsProcessManager()
            .ViaKafka()
            .ViaRabbitMq()
            .ViaInMemory()
            .ViaHttpRest()
            .ViaGrpc();
    }
}
