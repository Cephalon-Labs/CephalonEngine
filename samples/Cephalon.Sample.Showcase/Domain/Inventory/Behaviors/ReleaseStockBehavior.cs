using Cephalon.Abstractions.Behaviors;
using Cephalon.Sample.Showcase.Domain.Inventory.Models;
using Cephalon.Sample.Showcase.Infrastructure;

namespace Cephalon.Sample.Showcase.Domain.Inventory.Behaviors;

/// <summary>
/// Releases previously reserved stock as a saga compensation step.
/// This is triggered when the order saga fails and reservations need to be rolled back.
/// </summary>
[AppBehavior("inventory.release-stock")]
[BehaviorAllowedPatterns("saga-step")]
[BehaviorAllowedTransports("rabbitmq", "in-memory")]
public sealed class ReleaseStockBehavior : IAppBehavior<ReleaseStockInput, ReleaseStockOutput>
{
    /// <inheritdoc />
    public Task<ReleaseStockOutput> HandleAsync(
        ReleaseStockInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        foreach (var inventoryItem in ShowcaseDataStore.Inventory.Values)
        {
            if (inventoryItem.QuantityReserved > 0)
            {
                inventoryItem.QuantityReserved = Math.Max(0, inventoryItem.QuantityReserved - 1);
                inventoryItem.LastUpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return Task.FromResult(new ReleaseStockOutput(input.OrderId, Released: true));
    }

    /// <summary>
    /// Declares the saga-step pattern (compensation) with messaging transports.
    /// </summary>
    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsSaga()
            .ViaRabbitMq()
            .ViaInMemory()
            .WithOptions(opts => opts.InboxEnabled = true);
    }
}
