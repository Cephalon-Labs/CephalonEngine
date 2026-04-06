using System.Text.Json.Serialization;

namespace Cephalon.EventSourcing.OpenSearch;

/// <summary>OpenSearch document mapping for an event stream entry.</summary>
public sealed class OpenSearchEventEntry
{
    /// <summary>Gets or sets the logical stream identifier.</summary>
    [JsonPropertyName("stream_id")] public string StreamId { get; set; } = string.Empty;
    /// <summary>Gets or sets the per-stream monotonic version number.</summary>
    [JsonPropertyName("stream_version")] public long StreamVersion { get; set; }
    /// <summary>Gets or sets the fully-qualified CLR event type name.</summary>
    [JsonPropertyName("event_type")] public string EventType { get; set; } = string.Empty;
    /// <summary>Gets or sets the JSON-serialized event payload.</summary>
    [JsonPropertyName("payload")] public string Payload { get; set; } = string.Empty;
    /// <summary>Gets or sets the UTC timestamp when the domain event occurred.</summary>
    [JsonPropertyName("occurred_at_utc")] public DateTime OccurredAtUtc { get; set; }
    /// <summary>Gets or sets the UTC timestamp when the event was appended to the store.</summary>
    [JsonPropertyName("appended_at_utc")] public DateTime AppendedAtUtc { get; set; }
    /// <summary>Gets or sets an optional causality tracking identifier.</summary>
    [JsonPropertyName("correlation_id")] public string? CorrelationId { get; set; }
    /// <summary>Gets or sets an optional tenant identifier for multi-tenancy.</summary>
    [JsonPropertyName("tenant_id")] public string? TenantId { get; set; }
}
