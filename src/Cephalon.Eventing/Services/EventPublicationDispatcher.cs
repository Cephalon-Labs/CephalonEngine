using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventPublicationDispatcher(
    IEventPublisher publisher,
    EventingOptions options,
    EventPublicationScheduleQueue scheduleQueue) : IEventPublicationDispatcher
{
    public async ValueTask<EventPublicationResult> PublishAsync(
        EventPublicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var publication = CreatePublication(request, request.Metadata);

        var metadata = new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["publicationDispatcher"] = "cephalon-eventing",
            ["publicationRuntimeState"] = "available",
            ["publicationId"] = request.Id,
            ["channelId"] = request.ChannelId,
            ["eventType"] = request.EventType
        };

        var nowUtc = DateTimeOffset.UtcNow;
        if (EventPublicationSchedulingPolicy.TryCreateSchedule(request, nowUtc, out var schedule))
        {
            if (!options.EnablePublicationScheduling)
            {
                throw new ArgumentException(
                    "Publication scheduling is not enabled for the active eventing runtime.",
                    nameof(request));
            }

            if (schedule.DelayMilliseconds > 0)
            {
                var scheduledMetadata = await scheduleQueue.ScheduleAsync(
                        CreatePublication(request, metadata),
                        schedule,
                        cancellationToken)
                    .ConfigureAwait(false);
                MergeMetadata(metadata, scheduledMetadata);

                return new EventPublicationResult(
                    request.Id,
                    request.ChannelId,
                    request.EventType,
                    EventPublicationOutcomes.Accepted,
                    DateTimeOffset.UtcNow,
                    Error: null,
                    metadata);
            }

            var immediateMetadata = EventPublicationSchedulingPolicy.CreateMetadata(
                metadata,
                schedule,
                "past-due-immediate",
                scheduleQueue.PendingCount);
            immediateMetadata["scheduleDispatch"] = "immediate";
            MergeMetadata(metadata, immediateMetadata);
            publication = CreatePublication(request, metadata);
        }

        await publisher.PublishAsync(publication, cancellationToken).ConfigureAwait(false);

        return new EventPublicationResult(
            request.Id,
            request.ChannelId,
            request.EventType,
            EventPublicationOutcomes.Accepted,
            DateTimeOffset.UtcNow,
            Error: null,
            metadata);
    }

    private static EventPublication CreatePublication(
        EventPublicationRequest request,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new EventPublication(
            id: request.Id,
            channelId: request.ChannelId,
            eventType: request.EventType,
            payload: request.Payload,
            occurredAtUtc: request.OccurredAtUtc,
            contentType: request.ContentType,
            correlationId: request.CorrelationId,
            tenantId: request.TenantId,
            headers: request.Headers,
            metadata: metadata);
    }

    private static void MergeMetadata(
        Dictionary<string, string> target,
        IReadOnlyDictionary<string, string> source)
    {
        foreach (var pair in source)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                target[pair.Key.Trim()] = pair.Value;
            }
        }
    }
}
