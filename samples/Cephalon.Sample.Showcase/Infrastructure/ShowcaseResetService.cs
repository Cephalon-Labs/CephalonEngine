using Microsoft.EntityFrameworkCore;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Resets the showcase sample back to its baseline seeded state for deterministic reruns.
/// </summary>
internal sealed class ShowcaseResetService(
    ShowcaseActivityFeed activityFeed,
    ShowcaseInMemoryEventStore eventStore,
    ShowcaseReadModelProjector readModelProjector,
    ShowcaseWriteDbContext? writeDb,
    ShowcaseReadDbContext? readDb,
    ShowcaseAuditHistoryDbContext? historyDb)
{
    public async Task<ShowcaseResetResponse> ResetAsync(CancellationToken cancellationToken = default)
    {
        ShowcaseDataStore.Reset();
        activityFeed.Clear();
        eventStore.Reset();

        if (writeDb is not null)
        {
            await ClearWriteDatabaseAsync(writeDb, cancellationToken).ConfigureAwait(false);
            ShowcaseDatabaseSeeder.SeedCommerceReferenceData(writeDb);
        }

        if (readDb is not null)
        {
            await ClearReadDatabaseAsync(readDb, cancellationToken).ConfigureAwait(false);
        }

        await readModelProjector.ProjectAllAsync(cancellationToken).ConfigureAwait(false);

        var auditEntryCount = 0;
        if (historyDb is not null)
        {
            await ClearAuditHistoryAsync(historyDb, cancellationToken).ConfigureAwait(false);
            auditEntryCount = await historyDb.AuditEntries.CountAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ShowcaseResetResponse(
            ResetAtUtc: DateTimeOffset.UtcNow,
            ProductCount: ShowcaseDataStore.Products.Count,
            InventoryCount: ShowcaseDataStore.Inventory.Count,
            OrderCount: ShowcaseDataStore.Orders.Count,
            ShipmentCount: ShowcaseDataStore.Shipments.Count,
            CartStreamCount: eventStore.GetSnapshots().Count,
            AuditEntryCount: auditEntryCount);
    }

    private static async Task ClearWriteDatabaseAsync(
        ShowcaseWriteDbContext db,
        CancellationToken cancellationToken)
    {
        await ClearCommerceDatabaseAsync(db, cancellationToken).ConfigureAwait(false);

        var outboxEntries = await db.OutboxMessages.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (outboxEntries.Count > 0)
        {
            db.OutboxMessages.RemoveRange(outboxEntries);
        }

        var inboxEntries = await db.InboxMessages.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (inboxEntries.Count > 0)
        {
            db.InboxMessages.RemoveRange(inboxEntries);
        }

        var projectionJobs = await db.ReadProjectionJobs.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (projectionJobs.Count > 0)
        {
            db.ReadProjectionJobs.RemoveRange(projectionJobs);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ClearReadDatabaseAsync(
        ShowcaseReadDbContext db,
        CancellationToken cancellationToken)
    {
        await ClearCommerceDatabaseAsync(db, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ClearCommerceDatabaseAsync(
        ShowcaseCommerceDbContextBase db,
        CancellationToken cancellationToken)
    {
        var orderLineItems = await db.OrderLineItems.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (orderLineItems.Count > 0)
        {
            db.OrderLineItems.RemoveRange(orderLineItems);
        }

        var orders = await db.Orders.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (orders.Count > 0)
        {
            db.Orders.RemoveRange(orders);
        }

        var shipments = await db.Shipments.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (shipments.Count > 0)
        {
            db.Shipments.RemoveRange(shipments);
        }

        var inventory = await db.InventoryItems.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (inventory.Count > 0)
        {
            db.InventoryItems.RemoveRange(inventory);
        }

        var products = await db.Products.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (products.Count > 0)
        {
            db.Products.RemoveRange(products);
        }
    }

    private static async Task ClearAuditHistoryAsync(
        ShowcaseAuditHistoryDbContext db,
        CancellationToken cancellationToken)
    {
        var entries = await db.AuditEntries.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            return;
        }

        db.AuditEntries.RemoveRange(entries);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
