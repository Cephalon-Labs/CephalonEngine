using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Inventory.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Inventory.Behaviors;

/// <summary>
/// Reserves stock for an order using the saga-step pattern.
/// The saga runtime loads and saves <see cref="InventoryReservationSagaState" /> automatically.
/// </summary>
[AppBehavior("inventory.reserve-stock")]
[BehaviorAllowedPatterns("saga-step")]
[BehaviorAllowedTransports("rabbitmq", "in-memory")]
public sealed class ReserveStockBehavior : IAppBehavior<ReserveStockInput, ReserveStockOutput>
{
    /// <inheritdoc />
    public Task<ReserveStockOutput> HandleAsync(
        ReserveStockInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        var reservations = new List<StockReservation>();
        var allReserved = true;

        foreach (var item in input.Items)
        {
            if (ShowcaseDataStore.Inventory.TryGetValue(item.ProductId, out var inventoryItem))
            {
                if (inventoryItem.QuantityAvailable >= item.Quantity)
                {
                    inventoryItem.QuantityReserved += item.Quantity;
                    inventoryItem.LastUpdatedAtUtc = DateTime.UtcNow;
                    reservations.Add(new StockReservation(
                        item.ProductId,
                        item.Quantity,
                        inventoryItem.WarehouseCode));
                }
                else
                {
                    allReserved = false;
                }
            }
            else
            {
                allReserved = false;
            }
        }

        return Task.FromResult(new ReserveStockOutput(input.OrderId, allReserved, reservations));
    }

    /// <summary>
    /// Declares the saga-step pattern with messaging transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsSaga()
            .ViaRabbitMq()
            .ViaInMemory()
            .WithOptions(opts => opts.InboxEnabled = true);
    }
}
