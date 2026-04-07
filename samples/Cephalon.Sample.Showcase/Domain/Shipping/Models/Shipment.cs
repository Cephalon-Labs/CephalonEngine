namespace Cephalon.Sample.Showcase.Domain.Shipping.Models;

/// <summary>
/// Represents a shipment managed by the process-manager pattern.
/// </summary>
public sealed class Shipment
{
    /// <summary>Gets the shipment identifier.</summary>
    public required string ShipmentId { get; init; }

    /// <summary>Gets the order identifier this shipment fulfills.</summary>
    public required string OrderId { get; init; }

    /// <summary>Gets the destination shipping address.</summary>
    public required string DestinationAddress { get; init; }

    /// <summary>Gets or sets the carrier name.</summary>
    public string Carrier { get; set; } = "Showcase Express";

    /// <summary>Gets or sets the tracking number assigned by the carrier.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>Gets or sets the current shipment status.</summary>
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

    /// <summary>Gets or sets the estimated delivery date.</summary>
    public DateTime? EstimatedDeliveryUtc { get; set; }

    /// <summary>Gets or sets the actual delivery date.</summary>
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the shipment was created.</summary>
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the lifecycle status of a shipment.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>Shipment created, awaiting carrier pickup.</summary>
    Pending,

    /// <summary>Shipment picked up by carrier, label printed.</summary>
    LabelCreated,

    /// <summary>Shipment in transit to destination.</summary>
    InTransit,

    /// <summary>Shipment out for delivery.</summary>
    OutForDelivery,

    /// <summary>Shipment delivered to recipient.</summary>
    Delivered,

    /// <summary>Shipment returned to sender.</summary>
    Returned
}

// --- Shipping behavior input/output contracts ---

/// <summary>Input for initiating a new shipment.</summary>
public sealed record InitiateShippingInput(
    string OrderId,
    string DestinationAddress,
    List<ShippingLineItem> Items);

/// <summary>A line item included in the shipment.</summary>
public sealed record ShippingLineItem(string ProductId, string ProductName, int Quantity);

/// <summary>Output after a shipment is initiated.</summary>
public sealed record InitiateShippingOutput(
    string ShipmentId,
    string Status,
    DateTime? EstimatedDeliveryUtc);

/// <summary>Input for tracking a shipment.</summary>
public sealed record TrackShipmentInput(string ShipmentId);

/// <summary>Output containing shipment tracking details.</summary>
public sealed record TrackShipmentOutput(
    string ShipmentId,
    string OrderId,
    string Status,
    string Carrier,
    string? TrackingNumber,
    DateTime? EstimatedDeliveryUtc);

/// <summary>Input for confirming shipment delivery.</summary>
public sealed record ConfirmDeliveryInput(string ShipmentId, string RecipientName);

/// <summary>Output after delivery is confirmed.</summary>
public sealed record ConfirmDeliveryOutput(string ShipmentId, string Status, DateTime DeliveredAtUtc);
