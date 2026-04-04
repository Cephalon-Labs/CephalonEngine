using Cephalon.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Eventing.Services;

internal sealed class OutboxBackedEventPublisher(
    IOutbox outbox,
    IEventChannelCatalog channels,
    ILoggerFactory? loggerFactory = null) : IEventPublisher
{
    private readonly ILogger logger = (loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<OutboxBackedEventPublisher>();

    public async ValueTask PublishAsync(
        EventPublication publication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);

        if (!channels.TryGet(publication.ChannelId, out _))
        {
            throw new InvalidOperationException(
                $"Event channel '{publication.ChannelId}' is not registered in the active eventing runtime.");
        }

        await outbox.EnqueueAsync(
            new OutboxMessage(
                id: publication.Id,
                channelId: publication.ChannelId,
                messageType: publication.EventType,
                payload: publication.Payload,
                occurredAtUtc: publication.OccurredAtUtc,
                contentType: publication.ContentType,
                correlationId: publication.CorrelationId,
                tenantId: publication.TenantId,
                headers: publication.Headers,
                metadata: publication.Metadata),
            cancellationToken);

        EventingLoggerMessages.LogPublicationStaged(this.logger, publication.Id, publication.ChannelId);
    }
}
