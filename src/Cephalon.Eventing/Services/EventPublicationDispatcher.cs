using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal sealed class EventPublicationDispatcher(IEventPublisher publisher) : IEventPublicationDispatcher
{
    public async ValueTask<EventPublicationResult> PublishAsync(
        EventPublicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var publication = new EventPublication(
            id: request.Id,
            channelId: request.ChannelId,
            eventType: request.EventType,
            payload: request.Payload,
            occurredAtUtc: request.OccurredAtUtc,
            contentType: request.ContentType,
            correlationId: request.CorrelationId,
            tenantId: request.TenantId,
            headers: request.Headers,
            metadata: request.Metadata);

        await publisher.PublishAsync(publication, cancellationToken).ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["publicationDispatcher"] = "cephalon-eventing",
            ["publicationRuntimeState"] = "available",
            ["publicationId"] = request.Id,
            ["channelId"] = request.ChannelId,
            ["eventType"] = request.EventType
        };

        return new EventPublicationResult(
            request.Id,
            request.ChannelId,
            request.EventType,
            EventPublicationOutcomes.Accepted,
            DateTimeOffset.UtcNow,
            Error: null,
            metadata);
    }
}
