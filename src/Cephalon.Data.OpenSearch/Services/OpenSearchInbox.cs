using Cephalon.Abstractions.Data;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Cephalon.Data.OpenSearch.Services;

/// <summary>OpenSearch-backed inbox implementation for idempotent inbound message tracking.</summary>
internal sealed class OpenSearchInbox(OpenSearchClient client, string indexName) : IInbox
{
    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var response = await client.GetAsync<OpenSearchInboxEntry>(
            new DocumentPath<OpenSearchInboxEntry>(messageId),
            g => g.Index(indexName),
            cancellationToken).ConfigureAwait(false);
        return response.IsValid && response.Found;
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var entry = new OpenSearchInboxEntry
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
        if (!response.IsValid && response.ServerError?.Status != 409)
        {
            throw new InvalidOperationException(
                $"OpenSearch inbox mark-processed failed for message '{message.Id}': {response.DebugInformation}");
        }
    }
}
