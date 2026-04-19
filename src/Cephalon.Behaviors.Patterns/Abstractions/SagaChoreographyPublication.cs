using System.Text.Json;

namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Describes one event publication produced by a choreography-based saga step.
/// </summary>
public sealed class SagaChoreographyPublication
{
    private const string JsonContentType = "application/json";
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of <see cref="SagaChoreographyPublication"/>.
    /// </summary>
    /// <param name="id">The stable publication identifier.</param>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="payload">The serialized event payload.</param>
    /// <param name="occurredAtUtc">The time at which the publication occurred.</param>
    /// <param name="contentType">The payload content type when one is known.</param>
    /// <param name="correlationId">The correlation identifier associated with the publication.</param>
    /// <param name="tenantId">The tenant identifier associated with the publication.</param>
    /// <param name="isCompensation">Indicates whether the publication represents compensation work.</param>
    /// <param name="headers">Optional event headers.</param>
    /// <param name="metadata">Optional event metadata.</param>
    public SagaChoreographyPublication(
        string id,
        string channelId,
        string eventType,
        string payload,
        DateTimeOffset occurredAtUtc,
        string? contentType = null,
        string? correlationId = null,
        string? tenantId = null,
        bool isCompensation = false,
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
        IsCompensation = isCompensation;
        Headers = headers is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates one choreography publication with a JSON-serialized payload.
    /// </summary>
    /// <typeparam name="TPayload">The payload type to serialize.</typeparam>
    /// <param name="id">The stable publication identifier.</param>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="payload">The typed payload to serialize as JSON.</param>
    /// <param name="occurredAtUtc">The time at which the publication occurred.</param>
    /// <param name="correlationId">The correlation identifier associated with the publication.</param>
    /// <param name="tenantId">The tenant identifier associated with the publication.</param>
    /// <param name="isCompensation">Indicates whether the publication represents compensation work.</param>
    /// <param name="headers">Optional event headers.</param>
    /// <param name="metadata">Optional event metadata.</param>
    /// <param name="serializerOptions">Optional JSON serializer options for the payload.</param>
    /// <returns>The choreography publication with a JSON payload and <c>application/json</c> content type.</returns>
    public static SagaChoreographyPublication CreateJson<TPayload>(
        string id,
        string channelId,
        string eventType,
        TPayload payload,
        DateTimeOffset occurredAtUtc,
        string? correlationId = null,
        string? tenantId = null,
        bool isCompensation = false,
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        var serializedPayload = JsonSerializer.Serialize(
            payload,
            serializerOptions ?? WebJsonSerializerOptions);

        return new SagaChoreographyPublication(
            id,
            channelId,
            eventType,
            serializedPayload,
            occurredAtUtc,
            contentType: JsonContentType,
            correlationId: correlationId,
            tenantId: tenantId,
            isCompensation: isCompensation,
            headers: headers,
            metadata: metadata);
    }

    /// <summary>
    /// Creates one choreography publication with a JSON-serialized payload that is explicitly marked as compensation work.
    /// </summary>
    /// <typeparam name="TPayload">The payload type to serialize.</typeparam>
    /// <param name="id">The stable publication identifier.</param>
    /// <param name="channelId">The logical channel or destination identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="payload">The typed payload to serialize as JSON.</param>
    /// <param name="occurredAtUtc">The time at which the publication occurred.</param>
    /// <param name="correlationId">The correlation identifier associated with the publication.</param>
    /// <param name="tenantId">The tenant identifier associated with the publication.</param>
    /// <param name="headers">Optional event headers.</param>
    /// <param name="metadata">Optional event metadata.</param>
    /// <param name="serializerOptions">Optional JSON serializer options for the payload.</param>
    /// <returns>The choreography publication with a JSON payload, compensation flag, and JSON content type.</returns>
    public static SagaChoreographyPublication CreateCompensationJson<TPayload>(
        string id,
        string channelId,
        string eventType,
        TPayload payload,
        DateTimeOffset occurredAtUtc,
        string? correlationId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        JsonSerializerOptions? serializerOptions = null)
        => CreateJson(
            id,
            channelId,
            eventType,
            payload,
            occurredAtUtc,
            correlationId: correlationId,
            tenantId: tenantId,
            isCompensation: true,
            headers: headers,
            metadata: metadata,
            serializerOptions: serializerOptions);

    /// <summary>Gets the stable publication identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the logical channel or destination identifier.</summary>
    public string ChannelId { get; }

    /// <summary>Gets the logical event type identifier.</summary>
    public string EventType { get; }

    /// <summary>Gets the serialized event payload.</summary>
    public string Payload { get; }

    /// <summary>Gets the time at which the publication occurred.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>Gets the payload content type when one is known.</summary>
    public string? ContentType { get; }

    /// <summary>Gets the correlation identifier associated with the publication.</summary>
    public string? CorrelationId { get; }

    /// <summary>Gets the tenant identifier associated with the publication.</summary>
    public string? TenantId { get; }

    /// <summary>Gets a value indicating whether this publication represents compensation work.</summary>
    public bool IsCompensation { get; }

    /// <summary>Gets the event headers associated with the publication.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Gets the event metadata associated with the publication.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
