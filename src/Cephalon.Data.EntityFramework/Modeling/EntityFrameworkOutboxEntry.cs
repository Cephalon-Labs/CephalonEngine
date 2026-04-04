namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Represents one durable outbox row stored through the Entity Framework data companion pack.
/// </summary>
public sealed class EntityFrameworkOutboxEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkOutboxEntry" /> class.
    /// </summary>
    public EntityFrameworkOutboxEntry()
    {
    }

    /// <summary>
    /// Gets or sets the stable outbox message identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical channel or destination identifier.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical message type identifier.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized payload that should be delivered later.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time at which the message became visible to the outbox.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

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

    /// <summary>
    /// Gets or sets the time at which the outbox row was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the time at which the outbox row was dispatched, when known.
    /// </summary>
    public DateTimeOffset? DispatchedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the time at which the outbox row becomes eligible for the next dispatch attempt, when delayed retry is in effect.
    /// </summary>
    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the current dispatch-attempt count.
    /// </summary>
    public int DispatchAttemptCount { get; set; }
}
