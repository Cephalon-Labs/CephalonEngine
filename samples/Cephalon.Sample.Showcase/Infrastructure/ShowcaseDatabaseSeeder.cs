using System.Text.Json;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Seeds and reseeds deterministic commerce reference data for the showcase sample.
/// </summary>
internal static class ShowcaseDatabaseSeeder
{
    public static void SeedCommerceReferenceData(ShowcaseCommerceDbContextBase db)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (!db.Products.Any())
        {
            foreach (var product in ShowcaseDataStore.Products.Values)
            {
                db.Products.Add(new ShowcaseProductEntity
                {
                    Id = product.Id,
                    Sku = product.Sku,
                    Name = product.Name,
                    Description = product.Description,
                    Category = product.Category,
                    PriceInCents = product.PriceInCents,
                    Currency = product.Currency,
                    IsActive = product.IsActive,
                    TagsJson = JsonSerializer.Serialize(product.Tags),
                    CreatedAtUtc = product.CreatedAtUtc,
                    UpdatedAtUtc = product.UpdatedAtUtc
                });
            }

            db.SaveChanges();
        }

        if (!db.InventoryItems.Any())
        {
            foreach (var item in ShowcaseDataStore.Inventory.Values)
            {
                db.InventoryItems.Add(new ShowcaseInventoryEntity
                {
                    ProductId = item.ProductId,
                    QuantityOnHand = item.QuantityOnHand,
                    QuantityReserved = item.QuantityReserved,
                    WarehouseCode = item.WarehouseCode,
                    LastUpdatedAtUtc = item.LastUpdatedAtUtc
                });
            }

            db.SaveChanges();
        }
    }
}
