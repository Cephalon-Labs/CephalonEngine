namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one inbound message tracked by an inbox implementation for idempotency or replay control.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Creates a new inbox message.
    /// </summary>
    /// <param name="id">The stable inbound message identifier.</param>
    /// <param name="channelId">The logical channel or source identifier.</param>
    /// <param name="messageType">The logical message type identifier.</param>
    /// <param name="payload">The serialized payload that was received.</param>
    /// <param name="receivedAtUtc">The time at which the message was received.</param>
    /// <param name="contentType">The payload content type when one is known.</param>
    /// <param name="correlationId">The correlation identifier associated with the message.</param>
    /// <param name="tenantId">The tenant identifier associated with the message.</param>
    /// <param name="headers">Optional message headers.</param>
    /// <param name="metadata">Optional message metadata.</param>
    public InboxMessage(
        string id,
        string channelId,
        string messageType,
        string payload,
        DateTimeOffset receivedAtUtc,
        string? contentType = null,
        string? correlationId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Inbox message id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Inbox message channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            throw new ArgumentException("Inbox message type is required.", nameof(messageType));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Inbox message payload is required.", nameof(payload));
        }

        if (receivedAtUtc == default)
        {
            throw new ArgumentException("Inbox message receipt time is required.", nameof(receivedAtUtc));
        }

        Id = id.Trim();
        ChannelId = channelId.Trim();
        MessageType = messageType.Trim();
        Payload = payload;
        ReceivedAtUtc = receivedAtUtc;
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
    /// Gets the stable inbound message identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the logical channel or source identifier.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the logical message type identifier.
    /// </summary>
    public string MessageType { get; }

    /// <summary>
    /// Gets the serialized payload that was received.
    /// </summary>
    public string Payload { get; }

    /// <summary>
    /// Gets the time at which the message was received.
    /// </summary>
    public DateTimeOffset ReceivedAtUtc { get; }

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
