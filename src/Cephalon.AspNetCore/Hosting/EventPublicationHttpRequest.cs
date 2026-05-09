using System.Text.Json;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Represents the operator HTTP request body used to publish an event through the active eventing runtime.
/// </summary>
public sealed class EventPublicationHttpRequest
{
    /// <summary>
    /// Gets or initializes the caller-supplied publication identifier.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets or initializes the target event channel identifier.
    /// </summary>
    public string? ChannelId { get; init; }

    /// <summary>
    /// Gets or initializes the logical event type.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// Gets or initializes the event payload as JSON.
    /// </summary>
    public JsonElement? Payload { get; init; }

    /// <summary>
    /// Gets or initializes the event occurrence timestamp.
    /// </summary>
    public DateTimeOffset? OccurredAtUtc { get; init; }

    /// <summary>
    /// Gets or initializes the payload content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or initializes the correlation identifier for the publication.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or initializes the tenant identifier associated with the event.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or initializes provider-specific event headers.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets or initializes metadata to attach to the publication.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets or initializes the actor identifier responsible for the publication.
    /// </summary>
    public string? ActorId { get; init; }
}
