namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one message staged for later delivery through an outbox implementation.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Creates a new outbox message.
    /// </summary>
    /// <param name="id">The stable outbox message identifier.</param>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="messageType">The logical message type identifier.</param>
    /// <param name="payload">The serialized payload that should be delivered later.</param>
    /// <param name="occurredAtUtc">The time at which the message became visible to the outbox.</param>
    /// <param name="contentType">The payload content type when one is known.</param>
    /// <param name="correlationId">The correlation identifier associated with the message.</param>
    /// <param name="tenantId">The tenant identifier associated with the message.</param>
    /// <param name="headers">Optional message headers.</param>
    /// <param name="metadata">Optional message metadata.</param>
    public OutboxMessage(
        string id,
        string channelId,
        string messageType,
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
            throw new ArgumentException("Outbox message id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Outbox message channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            throw new ArgumentException("Outbox message type is required.", nameof(messageType));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Outbox message payload is required.", nameof(payload));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("Outbox message occurrence time is required.", nameof(occurredAtUtc));
        }

        Id = id.Trim();
        ChannelId = channelId.Trim();
        MessageType = messageType.Trim();
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
    /// Gets the stable outbox message identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the logical channel or destination identifier.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the logical message type identifier.
    /// </summary>
    public string MessageType { get; }

    /// <summary>
    /// Gets the serialized payload that should be delivered later.
    /// </summary>
    public string Payload { get; }

    /// <summary>
    /// Gets the time at which the message became visible to the outbox.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

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
    /// Gets message headers associated with the message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets message metadata associated with the message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
