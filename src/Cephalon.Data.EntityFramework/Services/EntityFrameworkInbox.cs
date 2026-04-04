using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.EntityFramework.Modeling;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkInbox(
    DbContext dbContext,
    IEntityFrameworkInboxContext inboxContext) : IInbox
{
    public async ValueTask<bool> HasProcessedAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await inboxContext.InboxMessages
            .AnyAsync(entry => entry.Id == messageId.Trim(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask MarkProcessedAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (await HasProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        inboxContext.InboxMessages.Add(new EntityFrameworkInboxEntry
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            ReceivedAtUtc = message.ReceivedAtUtc,
            ProcessedAtUtc = DateTimeOffset.UtcNow,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            HeadersJson = JsonSerializer.Serialize(message.Headers),
            MetadataJson = JsonSerializer.Serialize(message.Metadata)
        });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
