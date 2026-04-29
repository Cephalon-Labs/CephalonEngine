namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one host-agnostic request to publish an integration event through the active eventing runtime.
/// </summary>
public sealed class EventPublicationRequest
{
    /// <summary>
    /// Creates an event-publication request.
    /// </summary>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="payload">The serialized event payload.</param>
    /// <param name="id">The stable publication identifier. A generated identifier is used when omitted.</param>
    /// <param name="occurredAtUtc">The time at which the event occurred. The current UTC time is used when omitted.</param>
    /// <param name="contentType">The payload content type when one is known.</param>
    /// <param name="correlationId">The correlation identifier associated with the event.</param>
    /// <param name="tenantId">The tenant identifier associated with the event.</param>
    /// <param name="headers">Optional event headers.</param>
    /// <param name="metadata">Optional operator-facing event metadata.</param>
    public EventPublicationRequest(
        string channelId,
        string eventType,
        string payload,
        string? id = null,
        DateTimeOffset? occurredAtUtc = null,
        string? contentType = null,
        string? correlationId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
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

        Id = string.IsNullOrWhiteSpace(id)
            ? $"event-publication-{Guid.NewGuid():N}"
            : id.Trim();
        ChannelId = channelId.Trim();
        EventType = eventType.Trim();
        Payload = payload;
        OccurredAtUtc = occurredAtUtc.GetValueOrDefault(DateTimeOffset.UtcNow);
        if (OccurredAtUtc == default)
        {
            throw new ArgumentException("Publication occurrence time is required.", nameof(occurredAtUtc));
        }

        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        Headers = CopyValues(headers);
        Metadata = CopyValues(metadata);
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
    /// Gets event headers associated with the publication.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets optional operator-facing event metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static Dictionary<string, string> CopyValues(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
