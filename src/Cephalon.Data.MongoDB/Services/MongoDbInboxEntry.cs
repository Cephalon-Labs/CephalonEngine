using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cephalon.Data.MongoDB.Services;

/// <summary>
/// Represents one processed inbound message document stored by the MongoDB inbox provider.
/// </summary>
internal sealed class MongoDbInboxEntry
{
    /// <summary>Gets or sets the database-assigned document identifier.</summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>Gets or sets the stable inbound message identifier (unique).</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical channel or source identifier.</summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical message type identifier.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the correlation identifier associated with the message.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the tenant identifier associated with the message.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the UTC time at which the message was received.</summary>
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC time at which the message was marked as processed.</summary>
    public DateTime ProcessedAtUtc { get; set; }
}
