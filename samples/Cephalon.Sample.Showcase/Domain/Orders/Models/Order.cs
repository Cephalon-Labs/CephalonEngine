namespace Cephalon.Sample.Showcase.Domain.Orders.Models;

/// <summary>
/// Represents an order in the event-driven orders domain.
/// </summary>
public sealed class Order
{
    /// <summary>Gets or sets the unique order identifier.</summary>
    public required string OrderId { get; init; }

    /// <summary>Gets or sets the customer identifier who placed the order.</summary>
    public required string CustomerId { get; init; }

    /// <summary>Gets or sets the tenant identifier.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets or sets the current order status.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Gets or sets the order line items.</summary>
    public List<OrderLineItem> Items { get; init; } = [];

    /// <summary>Gets or sets the total amount in cents.</summary>
    public long TotalInCents { get; set; }

    /// <summary>Gets or sets the shipping address.</summary>
    public required string ShippingAddress { get; init; }

    /// <summary>Gets or sets the UTC timestamp when the order was placed.</summary>
    public DateTime PlacedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when the order was last updated.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Gets or sets the cancellation reason if the order was cancelled.</summary>
    public string? CancellationReason { get; set; }
}

/// <summary>
/// Represents the lifecycle status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created, awaiting inventory reservation.</summary>
    Pending,

    /// <summary>Inventory reserved, awaiting payment confirmation.</summary>
    Confirmed,

    /// <summary>Payment confirmed, awaiting shipping.</summary>
    Processing,

    /// <summary>Order shipped, awaiting delivery.</summary>
    Shipped,

    /// <summary>Order delivered to the customer.</summary>
    Delivered,

    /// <summary>Order cancelled before fulfillment.</summary>
    Cancelled
}

/// <summary>
/// Represents a single line item within an order.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="ProductName">The product name at the time of ordering.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPriceInCents">The unit price at the time of ordering.</param>
public sealed record OrderLineItem(
    string ProductId,
    string ProductName,
    int Quantity,
    long UnitPriceInCents)
{
    /// <summary>Gets the line total in cents.</summary>
    public long LineTotalInCents => Quantity * UnitPriceInCents;
}

// --- Orders behavior input/output contracts ---

/// <summary>Input for placing a new order.</summary>
public sealed record PlaceOrderInput(
    string CustomerId,
    string ShippingAddress,
    List<PlaceOrderLineItem> Items);

/// <summary>A line item in a place-order request.</summary>
public sealed record PlaceOrderLineItem(
    string ProductId,
    string ProductName,
    int Quantity,
    long UnitPriceInCents);

/// <summary>Output after an order is placed (fire-and-forget acknowledgement).</summary>
public sealed record PlaceOrderOutput(string OrderId, string Status);

/// <summary>Input for cancelling an order.</summary>
public sealed record CancelOrderInput(string OrderId, string Reason);

/// <summary>Output after an order cancellation is accepted.</summary>
public sealed record CancelOrderOutput(string OrderId, string Status);

/// <summary>Input for retrieving order status.</summary>
public sealed record GetOrderStatusInput(string OrderId);

/// <summary>Output containing order status details.</summary>
public sealed record GetOrderStatusOutput(
    string OrderId,
    string CustomerId,
    string Status,
    long TotalInCents,
    int ItemCount,
    DateTime PlacedAtUtc);
