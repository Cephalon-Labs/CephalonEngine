using System.Text.Json.Serialization;

namespace Cephalon.Data.Elasticsearch.Services;

internal sealed class ElasticsearchOutboxEntry
{
    [JsonPropertyName("message_id")] public string MessageId { get; set; } = string.Empty;
    [JsonPropertyName("channel_id")] public string ChannelId { get; set; } = string.Empty;
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = string.Empty;
    [JsonPropertyName("payload")] public string Payload { get; set; } = string.Empty;
    [JsonPropertyName("content_type")] public string ContentType { get; set; } = string.Empty;
    [JsonPropertyName("correlation_id")] public string? CorrelationId { get; set; }
    [JsonPropertyName("tenant_id")] public string? TenantId { get; set; }
    [JsonPropertyName("occurred_at_utc")] public DateTime OccurredAtUtc { get; set; }
    [JsonPropertyName("created_at_utc")] public DateTime CreatedAtUtc { get; set; }
    [JsonPropertyName("dispatched_at_utc")] public DateTime? DispatchedAtUtc { get; set; }
    [JsonPropertyName("dispatch_attempt_count")] public int DispatchAttemptCount { get; set; }
    [JsonPropertyName("headers_json")] public string HeadersJson { get; set; } = "{}";
    [JsonPropertyName("metadata_json")] public string MetadataJson { get; set; } = "{}";
}
