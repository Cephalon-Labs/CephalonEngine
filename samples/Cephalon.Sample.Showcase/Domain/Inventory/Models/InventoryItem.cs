namespace Cephalon.Sample.Showcase.Domain.Inventory.Models;

/// <summary>
/// Represents an inventory item tracking stock levels for a product.
/// </summary>
public sealed class InventoryItem
{
    /// <summary>Gets the product identifier this inventory tracks.</summary>
    public required string ProductId { get; init; }

    /// <summary>Gets or sets the total stock quantity on hand.</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Gets or sets the quantity currently reserved by pending orders.</summary>
    public int QuantityReserved { get; set; }

    /// <summary>Gets the available quantity (on hand minus reserved).</summary>
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    /// <summary>Gets or sets the warehouse location code.</summary>
    public string WarehouseCode { get; set; } = "WH-01";

    /// <summary>Gets or sets the UTC timestamp of the last stock update.</summary>
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the saga state for an inventory reservation workflow.
/// </summary>
public sealed class InventoryReservationSagaState
{
    /// <summary>Gets or sets the order identifier this reservation belongs to.</summary>
    public required string OrderId { get; init; }

    /// <summary>Gets or sets the reservations made for this order.</summary>
    public List<StockReservation> Reservations { get; init; } = [];

    /// <summary>Gets or sets whether all reservations succeeded.</summary>
    public bool AllReserved { get; set; }

    /// <summary>Gets or sets whether this saga has been compensated (rolled back).</summary>
    public bool Compensated { get; set; }
}

/// <summary>
/// Represents a single stock reservation within a saga.
/// </summary>
/// <param name="ProductId">The product identifier reserved.</param>
/// <param name="Quantity">The quantity reserved.</param>
/// <param name="WarehouseCode">The warehouse holding the reservation.</param>
public sealed record StockReservation(string ProductId, int Quantity, string WarehouseCode);

// --- Inventory behavior input/output contracts ---

/// <summary>Input for reserving stock for an order.</summary>
public sealed record ReserveStockInput(
    string OrderId,
    List<ReserveStockLineItem> Items);

/// <summary>A line item in a stock reservation request.</summary>
public sealed record ReserveStockLineItem(string ProductId, int Quantity);

/// <summary>Output after a stock reservation attempt.</summary>
public sealed record ReserveStockOutput(
    string OrderId,
    bool AllReserved,
    List<StockReservation> Reservations);

/// <summary>Input for releasing previously reserved stock (saga compensation).</summary>
public sealed record ReleaseStockInput(string OrderId);

/// <summary>Output after stock is released.</summary>
public sealed record ReleaseStockOutput(string OrderId, bool Released);
