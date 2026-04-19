using System.Text.Json;
using Cephalon.Abstractions.Data;
using Elastic.Clients.Elasticsearch;

namespace Cephalon.Data.Elasticsearch.Services;

/// <summary>Elasticsearch-backed outbox implementation that stages messages for durable delivery.</summary>
internal sealed class ElasticsearchOutbox(ElasticsearchClient client, string indexName) : IOutbox
{
    /// <inheritdoc />
    public string OutboxId => "elasticsearch-outbox";

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var entry = new ElasticsearchOutboxEntry
        {
            MessageId = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            ContentType = message.ContentType ?? string.Empty,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            OccurredAtUtc = message.OccurredAtUtc.UtcDateTime,
            CreatedAtUtc = DateTime.UtcNow,
            DispatchAttemptCount = 0,
            HeadersJson = JsonSerializer.Serialize(message.Headers),
            MetadataJson = JsonSerializer.Serialize(message.Metadata)
        };

        var response = await client.IndexAsync(entry, idx => idx
                .Index(indexName)
                .Id(message.Id)
                .OpType(OpType.Create),
            cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess() && response.ApiCallDetails.HttpStatusCode != 409)
        {
            throw new InvalidOperationException(
                $"Elasticsearch outbox staging failed for message '{message.Id}': {response.DebugInformation}");
        }
    }
}
