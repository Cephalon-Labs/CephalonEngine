namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes one staged outbound event that is waiting for dispatch through the active eventing runtime.
/// </summary>
public sealed class EventDispatchItem
{
    /// <summary>
    /// Creates a new dispatch item.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier that owns the staged message.</param>
    /// <param name="messageId">The stable outbound message identifier.</param>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="payload">The serialized payload that should be dispatched.</param>
    /// <param name="occurredAtUtc">The time at which the event became visible to the outbox.</param>
    /// <param name="createdAtUtc">The time at which the durable outbox row was created.</param>
    /// <param name="dispatchAttemptCount">The number of dispatch attempts already recorded for this message.</param>
    /// <param name="contentType">The payload content type when one is known.</param>
    /// <param name="correlationId">The correlation identifier associated with the message.</param>
    /// <param name="tenantId">The tenant identifier associated with the message.</param>
    /// <param name="headers">Optional message headers associated with the dispatch item.</param>
    /// <param name="metadata">Optional message metadata associated with the dispatch item.</param>
    public EventDispatchItem(
        string outboxId,
        string messageId,
        string channelId,
        string eventType,
        string payload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc,
        int dispatchAttemptCount,
        string? contentType = null,
        string? correlationId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            throw new ArgumentException("Outbox id is required.", nameof(outboxId));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload is required.", nameof(payload));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("Occurrence time is required.", nameof(occurredAtUtc));
        }

        if (createdAtUtc == default)
        {
            throw new ArgumentException("Created time is required.", nameof(createdAtUtc));
        }

        if (dispatchAttemptCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dispatchAttemptCount), dispatchAttemptCount, "Dispatch attempt count cannot be negative.");
        }

        OutboxId = outboxId.Trim();
        MessageId = messageId.Trim();
        ChannelId = channelId.Trim();
        EventType = eventType.Trim();
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
        DispatchAttemptCount = dispatchAttemptCount;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        Headers = headers is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable outbox identifier that owns the staged message.
    /// </summary>
    public string OutboxId { get; }

    /// <summary>
    /// Gets the stable outbound message identifier.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the logical channel or destination identifier.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the logical event type identifier.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the serialized payload that should be dispatched.
    /// </summary>
    public string Payload { get; }

    /// <summary>
    /// Gets the time at which the event became visible to the outbox.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>
    /// Gets the time at which the durable outbox row was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Gets the number of dispatch attempts already recorded for this message.
    /// </summary>
    public int DispatchAttemptCount { get; }

    /// <summary>
    /// Gets the payload content type when one is known.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the message.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the message.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets optional message headers associated with the dispatch item.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets optional message metadata associated with the dispatch item.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
