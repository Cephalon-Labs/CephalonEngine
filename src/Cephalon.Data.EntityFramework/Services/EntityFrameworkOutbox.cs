using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.EntityFramework.Modeling;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkOutbox(
    DbContext dbContext,
    IEntityFrameworkOutboxContext outboxContext) : IOutbox
{
    public string OutboxId => EntityFrameworkDataRuntimeIds.OutboxId;

    public async ValueTask EnqueueAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        outboxContext.OutboxMessages.Add(new EntityFrameworkOutboxEntry
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            OccurredAtUtc = message.OccurredAtUtc,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            HeadersJson = JsonSerializer.Serialize(message.Headers),
            MetadataJson = JsonSerializer.Serialize(message.Metadata),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DispatchAttemptCount = 0
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
