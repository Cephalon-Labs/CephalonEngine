using Cephalon.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Eventing.Services;

internal sealed class OutboxBackedEventPublisher(
    IOutbox outbox,
    IEventChannelCatalog channels,
    IEventContextPolicyCatalog contextPolicyCatalog,
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

        var contextPolicyEvaluation = EventContextPolicyEvaluation.Evaluate(contextPolicyCatalog, publication);
        if (!contextPolicyEvaluation.IsValid)
        {
            var validationError = contextPolicyEvaluation.CreateValidationFailureMessage(publication);
            await publicationRuntimeReporter.ReportAsync(
                new EventPublicationRuntimeReport(
                    publicationId: publication.Id,
                    channelId: publication.ChannelId,
                    eventType: publication.EventType,
                    outcome: EventPublicationRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    error: validationError,
                    metadata: CreateRuntimeMetadata(publication, outbox.OutboxId, contextPolicyEvaluation, validationError)),
                cancellationToken).ConfigureAwait(false);

            throw new InvalidOperationException(validationError);
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
                metadata: CreateOutboxMessageMetadata(publication, contextPolicyEvaluation)),
            cancellationToken);

        EventingLoggerMessages.LogPublicationStaged(this.logger, publication.Id, publication.ChannelId);

        await publicationRuntimeReporter.ReportAsync(
            new EventPublicationRuntimeReport(
                publicationId: publication.Id,
                channelId: publication.ChannelId,
                eventType: publication.EventType,
                outcome: EventPublicationRuntimeOutcomes.Accepted,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: CreateRuntimeMetadata(publication, outbox.OutboxId, contextPolicyEvaluation)),
            cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string> CreateRuntimeMetadata(
        EventPublication publication,
        string outboxId,
        EventContextPolicyEvaluation contextPolicyEvaluation,
        string? error = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["publisherId"] = "outbox-backed-publisher",
            ["trigger"] = "outbox-backed-publisher",
            ["publicationRuntimeState"] = "reported",
            ["publicationOutcome"] = string.IsNullOrWhiteSpace(error)
                ? EventPublicationRuntimeOutcomes.Accepted
                : EventPublicationRuntimeOutcomes.Failed,
            ["publicationId"] = publication.Id,
            ["channelId"] = publication.ChannelId,
            ["eventType"] = publication.EventType,
            ["handoff"] = "outbox",
            ["dispatchRuntime"] = "configured",
            ["dispatchStore"] = "available",
            ["outboxId"] = outboxId,
            ["deliveryCompletion"] = string.IsNullOrWhiteSpace(error) ? "pending-dispatch" : "not-enqueued",
            ["matchedSubscriptionCount"] = "0",
            ["startedSubscriptionCount"] = "0",
            ["succeededSubscriptionCount"] = "0",
            ["failedSubscriptionCount"] = "0",
            ["retryScheduledSubscriptionCount"] = "0",
            ["skippedSubscriptionCount"] = "0",
            ["headerCount"] = publication.Headers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["publicationMetadataCount"] = publication.Metadata.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        contextPolicyEvaluation.ApplyMetadata(metadata);
        ApplyOutboxContextHandoffMetadata(metadata, contextPolicyEvaluation, error);

        if (!string.IsNullOrWhiteSpace(error))
        {
            metadata["error"] = error;
        }

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

    private static Dictionary<string, string> CreateOutboxMessageMetadata(
        EventPublication publication,
        EventContextPolicyEvaluation contextPolicyEvaluation)
    {
        var metadata = new Dictionary<string, string>(publication.Metadata, StringComparer.OrdinalIgnoreCase);
        contextPolicyEvaluation.ApplyOutboxHandoffMetadata(metadata);
        return metadata;
    }

    private static void ApplyOutboxContextHandoffMetadata(
        Dictionary<string, string> metadata,
        EventContextPolicyEvaluation contextPolicyEvaluation,
        string? error)
    {
        if (!contextPolicyEvaluation.HasPolicies)
        {
            return;
        }

        metadata["outboxContextHandoff"] = string.IsNullOrWhiteSpace(error)
            ? "staged-headers"
            : "not-enqueued";
        metadata["outboxContextValidation"] = string.IsNullOrWhiteSpace(error)
            ? contextPolicyEvaluation.RequiredHeaderNames.Count == 0 ? "not-required" : "publisher-enforced"
            : "failed";
        metadata["outboxContextMetadata"] = string.IsNullOrWhiteSpace(error) ? "staged" : "not-staged";
        metadata["outboxContextRequiredHeaderCount"] = contextPolicyEvaluation.RequiredHeaderNames.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metadata["outboxContextPresentHeaderCount"] = contextPolicyEvaluation.PresentHeaderNames.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metadata["outboxContextRequiredHeaders"] = string.Join(",", contextPolicyEvaluation.RequiredHeaderNames);
        metadata["outboxContextPresentHeaders"] = string.Join(",", contextPolicyEvaluation.PresentHeaderNames);
        metadata["outboxContextPropagationBoundary"] = "outbox-stage";
        metadata[EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation] = "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders] = "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.ConsumerContextExtraction] = "not-claimed";
        metadata[EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff] = "not-claimed";
    }
}
