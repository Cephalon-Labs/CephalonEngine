namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Represents one processed inbound message row stored through the Entity Framework data companion pack.
/// </summary>
public sealed class EntityFrameworkInboxEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkInboxEntry" /> class.
    /// </summary>
    public EntityFrameworkInboxEntry()
    {
    }

    /// <summary>
    /// Gets or sets the stable inbound message identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical channel or source identifier.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical message type identifier.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized payload that was received.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time at which the message was received.
    /// </summary>
    public DateTimeOffset ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the time at which the inbox row was marked as processed.
    /// </summary>
    public DateTimeOffset ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the payload content type when one is known.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier associated with the message.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier associated with the message.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the serialized message headers payload.
    /// </summary>
    public string HeadersJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the serialized message metadata payload.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
}
