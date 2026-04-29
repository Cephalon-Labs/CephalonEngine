using Cephalon.Eventing.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventPublisher(
    EventingOptions options,
    IEventChannelCatalog channels,
    InProcessEventSubscriptionExecutorCatalog executors,
    IEventSubscriptionRuntimeReporter runtimeReporter,
    ILoggerFactory? loggerFactory = null) : IEventPublisher
{
    private readonly ILogger logger = (loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<InProcessEventPublisher>();

    public async ValueTask PublishAsync(
        EventPublication publication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);
        cancellationToken.ThrowIfCancellationRequested();

        if (!channels.TryGet(publication.ChannelId, out _))
        {
            throw new InvalidOperationException(
                $"Event channel '{publication.ChannelId}' is not registered in the active eventing runtime.");
        }

        var entries = executors.GetByChannelId(publication.ChannelId);
        if (entries.Count == 0)
        {
            EventingLoggerMessages.LogPublicationDispatchSkipped(
                logger,
                InProcessEventingRuntimeIds.PublisherId,
                publication.Id,
                attempt: 1);
            return;
        }

        var failures = new List<Exception>();
        EventingLoggerMessages.LogPublicationDispatchStarted(
            logger,
            InProcessEventingRuntimeIds.PublisherId,
            publication.Id,
            attempt: 1);

        foreach (var entry in entries)
        {
            var metadata = CreateExecutionMetadata(publication, entry.Subscription);
            await runtimeReporter.ReportAsync(
                new EventSubscriptionExecutionReport(
                    subscriptionId: entry.Subscription.Id,
                    outcome: EventSubscriptionExecutionOutcomes.Started,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    messageId: publication.Id,
                    attempt: 1,
                    metadata: metadata),
                cancellationToken).ConfigureAwait(false);

            try
            {
                await entry.Executor.ExecuteAsync(
                    new EventSubscriptionExecutionContext(
                        entry.Subscription,
                        publication,
                        attempt: 1,
                        metadata: metadata),
                    cancellationToken).ConfigureAwait(false);

                await runtimeReporter.ReportAsync(
                    new EventSubscriptionExecutionReport(
                        subscriptionId: entry.Subscription.Id,
                        outcome: EventSubscriptionExecutionOutcomes.Succeeded,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        messageId: publication.Id,
                        attempt: 1,
                        metadata: metadata),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await runtimeReporter.ReportAsync(
                    new EventSubscriptionExecutionReport(
                        subscriptionId: entry.Subscription.Id,
                        outcome: EventSubscriptionExecutionOutcomes.Failed,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        messageId: publication.Id,
                        attempt: 1,
                        error: exception.Message,
                        metadata: metadata),
                    cancellationToken).ConfigureAwait(false);

                failures.Add(exception);
                if (!options.ContinueInProcessSubscriptionExecutionAfterFailure)
                {
                    throw;
                }
            }
        }

        if (failures.Count > 0)
        {
            var message = failures.Count == 1
                ? $"In-process event publication '{publication.Id}' failed for one subscription on channel '{publication.ChannelId}'."
                : $"In-process event publication '{publication.Id}' failed for {failures.Count.ToString(CultureInfo.InvariantCulture)} subscriptions on channel '{publication.ChannelId}'.";

            EventingLoggerMessages.LogPublicationDispatchFailed(
                logger,
                InProcessEventingRuntimeIds.PublisherId,
                publication.Id,
                attempt: 1,
                message);

            throw new InvalidOperationException(message, failures[0]);
        }

        EventingLoggerMessages.LogPublicationDispatchSucceeded(
            logger,
            InProcessEventingRuntimeIds.PublisherId,
            publication.Id,
            attempt: 1);
    }

    private static Dictionary<string, string> CreateExecutionMetadata(
        EventPublication publication,
        EventSubscriptionDescriptor subscription)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["publisherId"] = InProcessEventingRuntimeIds.PublisherId,
            ["trigger"] = InProcessEventingRuntimeIds.PublisherId,
            ["executionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
            ["executionOwnership"] = "cephalon-managed",
            ["executionMode"] = "in-process-direct",
            ["deliveryMode"] = "direct",
            ["retryPolicy"] = "none",
            ["channelId"] = publication.ChannelId,
            ["eventType"] = publication.EventType,
            ["subscriptionId"] = subscription.Id,
            ["handlerId"] = subscription.HandlerId,
            ["attempt"] = "1",
            ["headerCount"] = publication.Headers.Count.ToString(CultureInfo.InvariantCulture),
            ["publicationMetadataCount"] = publication.Metadata.Count.ToString(CultureInfo.InvariantCulture)
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
