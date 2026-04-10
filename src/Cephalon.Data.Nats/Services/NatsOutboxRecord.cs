using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Nats.Services;

internal sealed class NatsOutboxRecord
{
    public string MessageId { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int DispatchAttemptCount { get; set; }

    public DateTimeOffset? DispatchedAtUtc { get; set; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public string? ContentType { get; set; }

    public string? CorrelationId { get; set; }

    public string? TenantId { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static NatsOutboxRecord Create(OutboxMessage message) =>
        new()
        {
            MessageId = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            OccurredAtUtc = message.OccurredAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DispatchAttemptCount = 0,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            Headers = new Dictionary<string, string>(message.Headers, StringComparer.OrdinalIgnoreCase),
            Metadata = new Dictionary<string, string>(message.Metadata, StringComparer.OrdinalIgnoreCase)
        };
}
