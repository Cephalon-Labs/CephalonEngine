using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cephalon.Data.MongoDB.Services;

/// <summary>
/// Represents one persisted outbox message document stored by the MongoDB outbox provider.
/// </summary>
internal sealed class MongoDbOutboxEntry
{
    /// <summary>Gets or sets the database-assigned document identifier.</summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>Gets or sets the stable logical message identifier (unique).</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical channel or destination identifier.</summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical message type identifier.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized payload that should be delivered later.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the payload content type when one is known.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets or sets the correlation identifier associated with the message.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the tenant identifier associated with the message.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the UTC time at which the message was staged.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC time at which the message was delivered.</summary>
    public DateTime? DispatchedAtUtc { get; set; }

    /// <summary>Gets or sets the number of dispatch attempts made for this message.</summary>
    public int DispatchAttemptCount { get; set; }

    /// <summary>Gets or sets the UTC time of the next permitted delivery attempt.</summary>
    public DateTime? NextAttemptAtUtc { get; set; }

    /// <summary>Gets or sets the serialized message headers.</summary>
    public string HeadersJson { get; set; } = "{}";

    /// <summary>Gets or sets the serialized message metadata.</summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>Gets or sets the UTC time at which the domain event occurred.</summary>
    public DateTime OccurredAtUtc { get; set; }
}
