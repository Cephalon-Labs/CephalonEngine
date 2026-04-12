using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Audit;
using System.Globalization;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Sample.Showcase.Domain.Inventory.Behaviors;
using Cephalon.Sample.Showcase.Domain.Inventory.Models;
using Cephalon.Sample.Showcase.Domain.Orders.Models;
using Cephalon.Sample.Showcase.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Registers the inventory bounded context module.
/// Implements the saga-step behavior pattern with a module-owned REST surface.
/// Uses the EF-backed database role configured for the showcase host.
/// </summary>
public sealed class InventoryModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.inventory",
        displayName: "Showcase Inventory",
        description: "Inventory management module using the saga-step behavior pattern for compensable stock operations.",
        tags: ["showcase", "inventory", "saga-pattern", "compensation"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "showcase.inventory.saga",
            displayName: "Inventory saga",
            description: "Saga-step stock reservation with automatic compensation on failure."));
        capabilities.Add(new Capability(
            key: "showcase.inventory.inbox",
            displayName: "Inventory inbox",
            description: "Inbox deduplication for idempotent stock reservation handling."));
    }

    /// <inheritdoc />
    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Internal<ReserveStockBehavior>();
        behaviors.Internal<ReleaseStockBehavior>();
    }

    /// <inheritdoc />
    protected override void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/inventory");
        var routes = group.Routes;

        routes.MapGet(string.Empty, async (HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entities = await db.InventoryItems
                    .AsNoTracking()
                    .OrderBy(i => i.ProductId)
                    .ToListAsync();
                return Results.Ok(entities.Select(ToInventoryDto).ToList());
            }

            return Results.Ok(ShowcaseDataStore.Inventory.Values
                .OrderBy(i => i.ProductId)
                .ToList());
        });

        routes.MapGet("/{productId}", async (string productId, HttpContext ctx) =>
        {
            var readDb = ctx.RequestServices.GetService<ShowcaseReadDbContext>();
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            ShowcaseCommerceDbContextBase? db = readDb is not null ? readDb : writeDb;
            if (db is not null)
            {
                var entity = await db.InventoryItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ProductId == productId);
                return entity is not null ? Results.Ok(ToInventoryDto(entity)) : Results.NotFound();
            }

            return ShowcaseDataStore.Inventory.TryGetValue(productId, out var item)
                ? Results.Ok(item)
                : Results.NotFound();
        });

        routes.MapPost("/reserve", async (ReserveStockInput input, HttpContext ctx, ShowcaseReadModelSyncService readModelSync) =>
        {
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            if (writeDb is not null)
            {
                var order = await writeDb.Orders.FindAsync([input.OrderId], ctx.RequestAborted);
                if (order is not null &&
                    !string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Conflict($"Order '{input.OrderId}' is already in status '{order.Status}'.");
                }

                var reservations = new List<StockReservation>();
                var allReserved = true;
                var changedProductIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var lineItem in input.Items)
                {
                    var entity = await writeDb.InventoryItems.FindAsync([lineItem.ProductId], ctx.RequestAborted);
                    if (entity is null || entity.QuantityOnHand - entity.QuantityReserved < lineItem.Quantity)
                    {
                        allReserved = false;
                        continue;
                    }

                    entity.QuantityReserved += lineItem.Quantity;
                    entity.LastUpdatedAtUtc = DateTime.UtcNow;
                    changedProductIds.Add(entity.ProductId);
                    reservations.Add(new StockReservation(entity.ProductId, lineItem.Quantity, entity.WarehouseCode));
                }

                if (allReserved && order is not null)
                {
                    order.Status = "Confirmed";
                    order.UpdatedAtUtc = DateTime.UtcNow;
                }

                if (changedProductIds.Count > 0)
                {
                    readModelSync.EnqueueInventory(changedProductIds);
                }

                if (allReserved && order is not null)
                {
                    readModelSync.EnqueueOrders([order.OrderId]);
                }

                await writeDb.SaveChangesAsync(ctx.RequestAborted);
                await readModelSync.FlushAsync(ctx.RequestAborted);

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "inventory",
                        action: "stock-reserved",
                        summary: $"Processed stock reservation for order '{input.OrderId}'.",
                        subjectType: "inventory-reservation",
                        subjectId: input.OrderId,
                        outcome: allReserved ? AuditOutcome.Succeeded : AuditOutcome.Failed,
                        changes: reservations
                            .Select(reservation => new AuditChange(
                                $"reservation.{reservation.ProductId}",
                                null,
                                reservation.Quantity.ToString(CultureInfo.InvariantCulture)))
                            .ToArray(),
                        tags: ["inventory", "reserve", "saga-step"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(ctx, Descriptor.Id)));

                return Results.Ok(new ReserveStockOutput(input.OrderId, allReserved, reservations));
            }

            var memReservations = new List<StockReservation>();
            var memAllReserved = true;
            if (ShowcaseDataStore.Orders.TryGetValue(input.OrderId, out var memoryOrder) &&
                memoryOrder.Status != OrderStatus.Pending)
            {
                return Results.Conflict($"Order '{input.OrderId}' is already in status '{memoryOrder.Status}'.");
            }

            foreach (var lineItem in input.Items)
            {
                if (!ShowcaseDataStore.Inventory.TryGetValue(lineItem.ProductId, out var item) ||
                    item.QuantityAvailable < lineItem.Quantity)
                {
                    memAllReserved = false;
                    continue;
                }

                item.QuantityReserved += lineItem.Quantity;
                item.LastUpdatedAtUtc = DateTime.UtcNow;
                memReservations.Add(new StockReservation(item.ProductId, lineItem.Quantity, item.WarehouseCode));
            }

            if (memAllReserved && memoryOrder is not null)
            {
                memoryOrder.Status = OrderStatus.Confirmed;
                memoryOrder.UpdatedAtUtc = DateTime.UtcNow;
            }

            return Results.Ok(new ReserveStockOutput(input.OrderId, memAllReserved, memReservations));
        });

        routes.MapPost("/release", async (ReleaseStockInput input, HttpContext ctx, ShowcaseReadModelSyncService readModelSync) =>
        {
            var writeDb = ctx.RequestServices.GetService<ShowcaseWriteDbContext>();
            if (writeDb is not null)
            {
                var entities = await writeDb.InventoryItems.ToListAsync(ctx.RequestAborted);
                var changedProductIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entity in entities)
                {
                    if (entity.QuantityReserved > 0)
                    {
                        entity.QuantityReserved = 0;
                        entity.LastUpdatedAtUtc = DateTime.UtcNow;
                        changedProductIds.Add(entity.ProductId);
                    }
                }

                if (changedProductIds.Count > 0)
                {
                    readModelSync.EnqueueInventory(changedProductIds);
                }

                await writeDb.SaveChangesAsync(ctx.RequestAborted);
                await readModelSync.FlushAsync(ctx.RequestAborted);

                await ShowcaseAuditHelper.RecordAsync(
                    ctx,
                    new Cephalon.Audit.Services.AuditRecordRequest(
                        category: "inventory",
                        action: "stock-released",
                        summary: $"Released reserved stock for order '{input.OrderId}'.",
                        subjectType: "inventory-reservation",
                        subjectId: input.OrderId,
                        outcome: AuditOutcome.Succeeded,
                        tags: ["inventory", "release", "saga-step"],
                        metadata: ShowcaseAuditHelper.CreateMetadata(ctx, Descriptor.Id)));

                return Results.Ok(new ReleaseStockOutput(input.OrderId, true));
            }

            foreach (var item in ShowcaseDataStore.Inventory.Values)
            {
                if (item.QuantityReserved > 0)
                {
                    item.QuantityReserved = 0;
                    item.LastUpdatedAtUtc = DateTime.UtcNow;
                }
            }

            return Results.Ok(new ReleaseStockOutput(input.OrderId, true));
        });
    }

    private static object ToInventoryDto(ShowcaseInventoryEntity entity)
    {
        return new
        {
            entity.ProductId,
            entity.QuantityOnHand,
            entity.QuantityReserved,
            QuantityAvailable = entity.QuantityOnHand - entity.QuantityReserved,
            entity.WarehouseCode,
            entity.LastUpdatedAtUtc
        };
    }
}
