using System.Text.Json;
using Cassandra;
using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Services;

namespace Cephalon.Data.Cassandra.Services;

/// <summary>
/// Represents one durable Cassandra outbox row together with the metadata needed by the dispatch-store path.
/// </summary>
internal sealed class CassandraOutboxRecord
{
    public string MessageId { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public string? CorrelationId { get; set; }

    public string? TenantId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? DispatchedAtUtc { get; set; }

    public int DispatchAttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset EligibleAtUtc => NextAttemptAtUtc ?? CreatedAtUtc;

    public static CassandraOutboxRecord Create(OutboxMessage message, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new CassandraOutboxRecord
        {
            MessageId = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            CreatedAtUtc = createdAtUtc,
            OccurredAtUtc = message.OccurredAtUtc,
            DispatchAttemptCount = 0,
            NextAttemptAtUtc = createdAtUtc,
            Headers = new Dictionary<string, string>(message.Headers, StringComparer.OrdinalIgnoreCase),
            Metadata = new Dictionary<string, string>(message.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }

    public static CassandraOutboxRecord FromRow(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new CassandraOutboxRecord
        {
            MessageId = row.GetValue<string>("message_id"),
            ChannelId = row.GetValue<string>("channel_id"),
            MessageType = row.GetValue<string>("message_type"),
            Payload = row.GetValue<string>("payload"),
            ContentType = NormalizeOptional(row.GetValue<string>("content_type")),
            CorrelationId = NormalizeOptional(row.GetValue<string>("correlation_id")),
            TenantId = NormalizeOptional(row.GetValue<string>("tenant_id")),
            CreatedAtUtc = ToUtc(row.GetValue<DateTime>("created_at_utc")),
            OccurredAtUtc = ToUtc(row.GetValue<DateTime>("occurred_at_utc")),
            DispatchedAtUtc = row.IsNull("dispatched_at_utc")
                ? null
                : ToUtc(row.GetValue<DateTime>("dispatched_at_utc")),
            DispatchAttemptCount = row.GetValue<int>("dispatch_attempt_count"),
            NextAttemptAtUtc = row.IsNull("next_attempt_at_utc")
                ? null
                : ToUtc(row.GetValue<DateTime>("next_attempt_at_utc")),
            Headers = DeserializeDictionary(row.GetValue<string>("headers_json")),
            Metadata = DeserializeDictionary(row.GetValue<string>("metadata_json"))
        };
    }

    public EventDispatchItem ToDispatchItem(string outboxId) =>
        new(
            outboxId: outboxId,
            messageId: MessageId,
            channelId: ChannelId,
            eventType: MessageType,
            payload: Payload,
            occurredAtUtc: OccurredAtUtc,
            createdAtUtc: CreatedAtUtc,
            dispatchAttemptCount: DispatchAttemptCount,
            contentType: ContentType,
            correlationId: CorrelationId,
            tenantId: TenantId,
            headers: Headers,
            metadata: Metadata);

    public int ComputePendingDispatchShard(int shardCount) =>
        ComputePendingDispatchShard(MessageId, shardCount);

    public static int ComputePendingDispatchShard(string messageId, int shardCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (shardCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(shardCount), shardCount, "Shard count must be greater than or equal to 1.");
        }

        const uint fnvPrime = 16777619;
        uint hash = 2166136261;

        foreach (var ch in messageId)
        {
            hash ^= ch;
            hash *= fnvPrime;
        }

        return (int)(hash % shardCount);
    }

    private static string? NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value;

    private static DateTimeOffset ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(value)
            : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static Dictionary<string, string> DeserializeDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return result is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase);
    }
}
