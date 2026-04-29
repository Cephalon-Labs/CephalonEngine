using Cephalon.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Eventing.Services;

internal sealed class OutboxBackedEventPublisher(
    IOutbox outbox,
    IEventChannelCatalog channels,
    IEventPublicationRuntimeReporter publicationRuntimeReporter,
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

        await publicationRuntimeReporter.ReportAsync(
            new EventPublicationRuntimeReport(
                publicationId: publication.Id,
                channelId: publication.ChannelId,
                eventType: publication.EventType,
                outcome: EventPublicationRuntimeOutcomes.Accepted,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: CreateRuntimeMetadata(publication, outbox.OutboxId)),
            cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string> CreateRuntimeMetadata(
        EventPublication publication,
        string outboxId)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["publisherId"] = "outbox-backed-publisher",
            ["trigger"] = "outbox-backed-publisher",
            ["publicationRuntimeState"] = "reported",
            ["publicationOutcome"] = EventPublicationRuntimeOutcomes.Accepted,
            ["publicationId"] = publication.Id,
            ["channelId"] = publication.ChannelId,
            ["eventType"] = publication.EventType,
            ["handoff"] = "outbox",
            ["dispatchRuntime"] = "configured",
            ["dispatchStore"] = "available",
            ["outboxId"] = outboxId,
            ["deliveryCompletion"] = "pending-dispatch",
            ["matchedSubscriptionCount"] = "0",
            ["startedSubscriptionCount"] = "0",
            ["succeededSubscriptionCount"] = "0",
            ["failedSubscriptionCount"] = "0",
            ["retryScheduledSubscriptionCount"] = "0",
            ["skippedSubscriptionCount"] = "0",
            ["headerCount"] = publication.Headers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["publicationMetadataCount"] = publication.Metadata.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(publication.ContentType))
        {
            metadata["contentType"] = publication.ContentType!;
        }

        if (!string.IsNullOrWhiteSpace(publication.CorrelationId))
        {
            metadata["correlationId"] = publication.CorrelationId!;
        }

        if (!string.IsNullOrWhiteSpace(publication.TenantId))
        {
            metadata["tenantId"] = publication.TenantId!;
        }

        foreach (var pair in publication.Metadata)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                metadata[$"publicationMetadata.{pair.Key.Trim()}"] = pair.Value;
            }
        }

        return metadata;
    }
}
