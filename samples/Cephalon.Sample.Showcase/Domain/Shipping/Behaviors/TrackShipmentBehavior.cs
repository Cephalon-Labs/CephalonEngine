using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Shipping.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Shipping.Behaviors;

/// <summary>
/// Tracks a shipment's current status using the process-manager pattern.
/// The checkpoint store provides the current step in the shipping process.
/// </summary>
[AppBehavior("shipping.track")]
[BehaviorAllowedPatterns("process-manager")]
[BehaviorAllowedTransports("grpc", "in-memory")]
public sealed class TrackShipmentBehavior : IAppBehavior<TrackShipmentInput, TrackShipmentOutput?>
{
    /// <inheritdoc />
    public Task<TrackShipmentOutput?> HandleAsync(
        TrackShipmentInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        if (!ShowcaseDataStore.Shipments.TryGetValue(input.ShipmentId, out var shipment))
        {
            return Task.FromResult<TrackShipmentOutput?>(null);
        }

        var output = new TrackShipmentOutput(
            ShipmentId: shipment.ShipmentId,
            OrderId: shipment.OrderId,
            Status: shipment.Status.ToString(),
            Carrier: shipment.Carrier,
            TrackingNumber: shipment.TrackingNumber,
            EstimatedDeliveryUtc: shipment.EstimatedDeliveryUtc);

        return Task.FromResult<TrackShipmentOutput?>(output);
    }

    /// <summary>
    /// Declares the process-manager pattern with read-oriented transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsProcessManager()
            .ViaGrpc()
            .ViaInMemory();
    }
}
