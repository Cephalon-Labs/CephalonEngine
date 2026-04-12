using Microsoft.EntityFrameworkCore;

namespace Cephalon.Sample.Showcase.Infrastructure;

[Flags]
internal enum ShowcaseProjectionScope
{
    None = 0,
    Products = 1,
    Inventory = 2,
    Orders = 4,
    Shipments = 8,
    All = Products | Inventory | Orders | Shipments
}

/// <summary>
/// Projects the write-side commerce store into the separate read-side database.
/// </summary>
internal sealed class ShowcaseReadModelProjector(
    ShowcaseWriteDbContext? writeDb,
    ShowcaseReadDbContext? readDb)
{
    public Task ProjectAllAsync(CancellationToken cancellationToken = default)
    {
        return ProjectAsync(ShowcaseProjectionScope.All, cancellationToken);
    }

    public async Task ProjectAsync(
        ShowcaseProjectionScope scope,
        CancellationToken cancellationToken = default)
    {
        if (writeDb is null || readDb is null || scope == ShowcaseProjectionScope.None)
        {
            return;
        }

        if (scope.HasFlag(ShowcaseProjectionScope.All))
        {
            await RebuildReadModelAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (scope.HasFlag(ShowcaseProjectionScope.Products))
        {
            var productIds = await writeDb.Products
                .AsNoTracking()
                .Select(static product => product.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            await ProjectProductsAsync(productIds, cancellationToken).ConfigureAwait(false);
        }

        if (scope.HasFlag(ShowcaseProjectionScope.Inventory))
        {
            var productIds = await writeDb.InventoryItems
                .AsNoTracking()
                .Select(static item => item.ProductId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            await ProjectInventoryAsync(productIds, cancellationToken).ConfigureAwait(false);
        }

        if (scope.HasFlag(ShowcaseProjectionScope.Orders))
        {
            var orderIds = await writeDb.Orders
                .AsNoTracking()
                .Select(static order => order.OrderId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            await ProjectOrdersAsync(orderIds, cancellationToken).ConfigureAwait(false);
        }

        if (scope.HasFlag(ShowcaseProjectionScope.Shipments))
        {
            var shipmentIds = await writeDb.Shipments
                .AsNoTracking()
                .Select(static shipment => shipment.ShipmentId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            await ProjectShipmentsAsync(shipmentIds, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ProjectProductsAsync(
        IEnumerable<string> productIds,
        CancellationToken cancellationToken = default)
    {
        if (writeDb is null || readDb is null)
        {
            return;
        }

        var normalizedIds = NormalizeKeys(productIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        var sourceProducts = await writeDb.Products
            .AsNoTracking()
            .Where(product => normalizedIds.Contains(product.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingProducts = await readDb.Products
            .Where(product => normalizedIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        foreach (var productId in normalizedIds.Except(sourceProducts.Select(static product => product.Id), StringComparer.OrdinalIgnoreCase))
        {
            if (existingProducts.TryGetValue(productId, out var missingProjection))
            {
                readDb.Products.Remove(missingProjection);
            }
        }

        foreach (var source in sourceProducts)
        {
            if (!existingProducts.TryGetValue(source.Id, out var projection))
            {
                projection = new ShowcaseProductEntity
                {
                    Id = source.Id
                };
                readDb.Products.Add(projection);
            }

            projection.Sku = source.Sku;
            projection.Name = source.Name;
            projection.Description = source.Description;
            projection.Category = source.Category;
            projection.PriceInCents = source.PriceInCents;
            projection.Currency = source.Currency;
            projection.IsActive = source.IsActive;
            projection.TagsJson = source.TagsJson;
            projection.CreatedAtUtc = source.CreatedAtUtc;
            projection.UpdatedAtUtc = source.UpdatedAtUtc;
        }

        await readDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ProjectInventoryAsync(
        IEnumerable<string> productIds,
        CancellationToken cancellationToken = default)
    {
        if (writeDb is null || readDb is null)
        {
            return;
        }

        var normalizedIds = NormalizeKeys(productIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        var sourceInventory = await writeDb.InventoryItems
            .AsNoTracking()
            .Where(item => normalizedIds.Contains(item.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingInventory = await readDb.InventoryItems
            .Where(item => normalizedIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        foreach (var productId in normalizedIds.Except(sourceInventory.Select(static item => item.ProductId), StringComparer.OrdinalIgnoreCase))
        {
            if (existingInventory.TryGetValue(productId, out var missingProjection))
            {
                readDb.InventoryItems.Remove(missingProjection);
            }
        }

        foreach (var source in sourceInventory)
        {
            if (!existingInventory.TryGetValue(source.ProductId, out var projection))
            {
                projection = new ShowcaseInventoryEntity
                {
                    ProductId = source.ProductId
                };
                readDb.InventoryItems.Add(projection);
            }

            projection.QuantityOnHand = source.QuantityOnHand;
            projection.QuantityReserved = source.QuantityReserved;
            projection.WarehouseCode = source.WarehouseCode;
            projection.LastUpdatedAtUtc = source.LastUpdatedAtUtc;
        }

        await readDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ProjectOrdersAsync(
        IEnumerable<string> orderIds,
        CancellationToken cancellationToken = default)
    {
        if (writeDb is null || readDb is null)
        {
            return;
        }

        var normalizedIds = NormalizeKeys(orderIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        var sourceOrders = await writeDb.Orders
            .AsNoTracking()
            .Include(static order => order.Items)
            .Where(order => normalizedIds.Contains(order.OrderId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingOrders = await readDb.Orders
            .Include(static order => order.Items)
            .Where(order => normalizedIds.Contains(order.OrderId))
            .ToDictionaryAsync(order => order.OrderId, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);
        var existingLineItems = await readDb.OrderLineItems
            .Where(item => normalizedIds.Contains(item.OrderId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existingLineItems.Count > 0)
        {
            readDb.OrderLineItems.RemoveRange(existingLineItems);
        }

        foreach (var orderId in normalizedIds.Except(sourceOrders.Select(static order => order.OrderId), StringComparer.OrdinalIgnoreCase))
        {
            if (existingOrders.TryGetValue(orderId, out var missingProjection))
            {
                readDb.Orders.Remove(missingProjection);
            }
        }

        foreach (var source in sourceOrders)
        {
            if (!existingOrders.TryGetValue(source.OrderId, out var projection))
            {
                projection = new ShowcaseOrderEntity
                {
                    OrderId = source.OrderId
                };
                readDb.Orders.Add(projection);
            }

            projection.CustomerId = source.CustomerId;
            projection.TenantId = source.TenantId;
            projection.Status = source.Status;
            projection.TotalInCents = source.TotalInCents;
            projection.ShippingAddress = source.ShippingAddress;
            projection.PlacedAtUtc = source.PlacedAtUtc;
            projection.UpdatedAtUtc = source.UpdatedAtUtc;
            projection.CancellationReason = source.CancellationReason;
            projection.Items = source.Items
                .Select(item => new ShowcaseOrderLineItemEntity
                {
                    OrderId = source.OrderId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPriceInCents = item.UnitPriceInCents
                })
                .ToList();
        }

        await readDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ProjectShipmentsAsync(
        IEnumerable<string> shipmentIds,
        CancellationToken cancellationToken = default)
    {
        if (writeDb is null || readDb is null)
        {
            return;
        }

        var normalizedIds = NormalizeKeys(shipmentIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        var sourceShipments = await writeDb.Shipments
            .AsNoTracking()
            .Where(shipment => normalizedIds.Contains(shipment.ShipmentId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingShipments = await readDb.Shipments
            .Where(shipment => normalizedIds.Contains(shipment.ShipmentId))
            .ToDictionaryAsync(shipment => shipment.ShipmentId, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        foreach (var shipmentId in normalizedIds.Except(sourceShipments.Select(static shipment => shipment.ShipmentId), StringComparer.OrdinalIgnoreCase))
        {
            if (existingShipments.TryGetValue(shipmentId, out var missingProjection))
            {
                readDb.Shipments.Remove(missingProjection);
            }
        }

        foreach (var source in sourceShipments)
        {
            if (!existingShipments.TryGetValue(source.ShipmentId, out var projection))
            {
                projection = new ShowcaseShipmentEntity
                {
                    ShipmentId = source.ShipmentId
                };
                readDb.Shipments.Add(projection);
            }

            projection.OrderId = source.OrderId;
            projection.DestinationAddress = source.DestinationAddress;
            projection.Carrier = source.Carrier;
            projection.TrackingNumber = source.TrackingNumber;
            projection.Status = source.Status;
            projection.EstimatedDeliveryUtc = source.EstimatedDeliveryUtc;
            projection.DeliveredAtUtc = source.DeliveredAtUtc;
            projection.CreatedAtUtc = source.CreatedAtUtc;
        }

        await readDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RebuildReadModelAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writeDb);
        ArgumentNullException.ThrowIfNull(readDb);

        var sourceProducts = await writeDb.Products
            .AsNoTracking()
            .OrderBy(static product => product.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var sourceInventory = await writeDb.InventoryItems
            .AsNoTracking()
            .OrderBy(static item => item.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var sourceOrders = await writeDb.Orders
            .AsNoTracking()
            .Include(static order => order.Items)
            .OrderBy(static order => order.OrderId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var sourceShipments = await writeDb.Shipments
            .AsNoTracking()
            .OrderBy(static shipment => shipment.ShipmentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingOrderItems = await readDb.OrderLineItems.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (existingOrderItems.Count > 0)
        {
            readDb.OrderLineItems.RemoveRange(existingOrderItems);
        }

        var existingOrders = await readDb.Orders.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (existingOrders.Count > 0)
        {
            readDb.Orders.RemoveRange(existingOrders);
        }

        var existingShipments = await readDb.Shipments.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (existingShipments.Count > 0)
        {
            readDb.Shipments.RemoveRange(existingShipments);
        }

        var existingInventory = await readDb.InventoryItems.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (existingInventory.Count > 0)
        {
            readDb.InventoryItems.RemoveRange(existingInventory);
        }

        var existingProducts = await readDb.Products.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (existingProducts.Count > 0)
        {
            readDb.Products.RemoveRange(existingProducts);
        }

        await readDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        readDb.Products.AddRange(sourceProducts.Select(static source => new ShowcaseProductEntity
        {
            Id = source.Id,
            Sku = source.Sku,
            Name = source.Name,
            Description = source.Description,
            Category = source.Category,
            PriceInCents = source.PriceInCents,
            Currency = source.Currency,
            IsActive = source.IsActive,
            TagsJson = source.TagsJson,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        }));

        readDb.InventoryItems.AddRange(sourceInventory.Select(static source => new ShowcaseInventoryEntity
        {
            ProductId = source.ProductId,
            QuantityOnHand = source.QuantityOnHand,
            QuantityReserved = source.QuantityReserved,
            WarehouseCode = source.WarehouseCode,
            LastUpdatedAtUtc = source.LastUpdatedAtUtc
        }));

        readDb.Orders.AddRange(sourceOrders.Select(static source => new ShowcaseOrderEntity
        {
            OrderId = source.OrderId,
            CustomerId = source.CustomerId,
            TenantId = source.TenantId,
            Status = source.Status,
            TotalInCents = source.TotalInCents,
            ShippingAddress = source.ShippingAddress,
            PlacedAtUtc = source.PlacedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            CancellationReason = source.CancellationReason,
            Items = source.Items
                .Select(item => new ShowcaseOrderLineItemEntity
                {
                    OrderId = source.OrderId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPriceInCents = item.UnitPriceInCents
                })
                .ToList()
        }));

        readDb.Shipments.AddRange(sourceShipments.Select(static source => new ShowcaseShipmentEntity
        {
            ShipmentId = source.ShipmentId,
            OrderId = source.OrderId,
            DestinationAddress = source.DestinationAddress,
            Carrier = source.Carrier,
            TrackingNumber = source.TrackingNumber,
            Status = source.Status,
            EstimatedDeliveryUtc = source.EstimatedDeliveryUtc,
            DeliveredAtUtc = source.DeliveredAtUtc,
            CreatedAtUtc = source.CreatedAtUtc
        }));

        await readDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static HashSet<string> NormalizeKeys(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
