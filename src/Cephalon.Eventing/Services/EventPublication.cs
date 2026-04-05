namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes one integration event publication request handled by the active eventing runtime.
/// </summary>
public sealed class EventPublication
{
    /// <summary>
    /// Creates a new event publication request.
    /// </summary>
    /// <param name="id">The stable publication identifier.</param>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="payload">The serialized event payload.</param>
    /// <param name="occurredAtUtc">The time at which the event occurred.</param>
    /// <param name="contentType">The payload content type when one is known.</param>
    /// <param name="correlationId">The correlation identifier associated with the event.</param>
    /// <param name="tenantId">The tenant identifier associated with the event.</param>
    /// <param name="headers">Optional event headers.</param>
    /// <param name="metadata">Optional event metadata.</param>
    public EventPublication(
        string id,
        string channelId,
        string eventType,
        string payload,
        DateTimeOffset occurredAtUtc,
        string? contentType = null,
        string? correlationId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Publication id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Publication channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Publication event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Publication payload is required.", nameof(payload));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("Publication occurrence time is required.", nameof(occurredAtUtc));
        }

        Id = id.Trim();
        ChannelId = channelId.Trim();
        EventType = eventType.Trim();
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
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
    /// Gets the stable publication identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the logical channel or destination identifier.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the logical event type identifier.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the serialized event payload.
    /// </summary>
    public string Payload { get; }

    /// <summary>
    /// Gets the time at which the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>
    /// Gets the payload content type when one is known.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the event.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the event.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the event headers associated with the publication.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the event metadata associated with the publication.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
