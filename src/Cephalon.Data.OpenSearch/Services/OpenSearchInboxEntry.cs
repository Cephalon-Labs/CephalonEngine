using System.Text.Json.Serialization;

namespace Cephalon.Data.OpenSearch.Services;

internal sealed class OpenSearchInboxEntry
{
    [JsonPropertyName("message_id")] public string MessageId { get; set; } = string.Empty;
    [JsonPropertyName("channel_id")] public string ChannelId { get; set; } = string.Empty;
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = string.Empty;
    [JsonPropertyName("received_at_utc")] public DateTime ReceivedAtUtc { get; set; }
    [JsonPropertyName("processed_at_utc")] public DateTime ProcessedAtUtc { get; set; }
    [JsonPropertyName("correlation_id")] public string? CorrelationId { get; set; }
    [JsonPropertyName("tenant_id")] public string? TenantId { get; set; }
}
