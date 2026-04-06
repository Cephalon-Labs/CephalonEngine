using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cephalon.EventSourcing.MongoDB;

/// <summary>
/// Represents one persisted domain event document stored by the MongoDB event-store provider.
/// </summary>
public sealed class MongoDbEventEntry
{
    /// <summary>Gets or sets the database-assigned document identifier.</summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>Gets or sets the stable logical stream identifier.</summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optimistic stream version for the event.</summary>
    public long StreamVersion { get; set; }

    /// <summary>Gets or sets the assembly-qualified CLR event type.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the domain event occurred.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Gets or sets the UTC time at which the event was appended to the store.</summary>
    public DateTime AppendedAtUtc { get; set; }

    /// <summary>Gets or sets the correlation identifier associated with the event when known.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the tenant identifier associated with the event when known.</summary>
    public string? TenantId { get; set; }
}
