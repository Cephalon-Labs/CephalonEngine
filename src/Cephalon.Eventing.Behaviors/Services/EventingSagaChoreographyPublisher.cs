using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Eventing.Behaviors.Services;

internal sealed class EventingSagaChoreographyPublisher(
    IServiceScopeFactory serviceScopeFactory,
    ILoggerFactory? loggerFactory = null) : ISagaChoreographyPublisher
{
    private static readonly Action<ILogger, string, string, Exception?> LogPublicationDelegated =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(EventingSagaChoreographyPublisher)),
            "Saga choreography publication {PublicationId} was delegated to the eventing publish path for channel {ChannelId}.");

    private readonly ILogger logger = (loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<EventingSagaChoreographyPublisher>();

    public async ValueTask PublishAsync(
        SagaChoreographyPublication publication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var eventPublisher = scope.ServiceProvider.GetService<IEventPublisher>();
        if (eventPublisher is null)
        {
            throw new InvalidOperationException(
                "The Cephalon.Eventing publish path is not active for the current runtime. " +
                "Add Cephalon.Eventing with publishing enabled and a durable IOutbox before using the behavior eventing bridge.");
        }

        await eventPublisher.PublishAsync(Map(publication), cancellationToken).ConfigureAwait(false);
        LogPublicationDelegated(this.logger, publication.Id, publication.ChannelId, null);
    }

    private static EventPublication Map(SagaChoreographyPublication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);

        var metadata = publication.Metadata.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(publication.Metadata, StringComparer.OrdinalIgnoreCase);
        metadata["cephalon.pattern"] = "saga-choreography";
        metadata["cephalon.publisherBridge"] = "eventing.behaviors";
        metadata["cephalon.isCompensation"] = publication.IsCompensation ? "true" : "false";

        return new EventPublication(
            id: publication.Id,
            channelId: publication.ChannelId,
            eventType: publication.EventType,
            payload: publication.Payload,
            occurredAtUtc: publication.OccurredAtUtc,
            contentType: publication.ContentType,
            correlationId: publication.CorrelationId,
            tenantId: publication.TenantId,
            headers: publication.Headers,
            metadata: metadata);
    }
}
