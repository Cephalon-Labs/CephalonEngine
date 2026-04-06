using Cephalon.Abstractions.Data;
using Elastic.Clients.Elasticsearch;

namespace Cephalon.Data.Elasticsearch.Services;

/// <summary>Elasticsearch-backed inbox implementation for idempotent inbound message tracking.</summary>
internal sealed class ElasticsearchInbox(ElasticsearchClient client, string indexName) : IInbox
{
    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var response = await client.GetAsync<ElasticsearchInboxEntry>(messageId, g => g.Index(indexName), cancellationToken).ConfigureAwait(false);
        return response.IsSuccess() && response.Found;
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var entry = new ElasticsearchInboxEntry
        {
            MessageId = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            ReceivedAtUtc = message.ReceivedAtUtc.UtcDateTime,
            ProcessedAtUtc = DateTime.UtcNow,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId
        };
        var response = await client.IndexAsync(entry, idx => idx
                .Index(indexName).Id(message.Id).OpType(OpType.Create),
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess() && response.ApiCallDetails.HttpStatusCode != 409)
        {
            throw new InvalidOperationException(
                $"Elasticsearch inbox mark-processed failed for message '{message.Id}': {response.DebugInformation}");
        }
    }
}
