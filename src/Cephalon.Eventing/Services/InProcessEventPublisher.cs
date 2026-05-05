using Cephalon.Abstractions.Data;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Eventing.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventPublisher(
    EventingOptions options,
    IEventChannelCatalog channels,
    InProcessEventSubscriptionExecutorCatalog executors,
    InProcessEventSubscriptionIdempotencyTracker idempotencyTracker,
    IEventSubscriptionRuntimeReporter runtimeReporter,
    IEventPublicationRuntimeReporter publicationRuntimeReporter,
    ILoggerFactory? loggerFactory = null,
    RedactionPipeline? redactionPipeline = null) : IEventPublisher
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

        using var dispatchActivity = EventingDiagnostics.ActivitySource.StartActivity(
            EventingDiagnostics.PublicationDispatchActivityName,
            ActivityKind.Producer);
        dispatchActivity?.SetTag(
            EventingDiagnostics.PublisherIdTag,
            Redact(dispatchActivity, EventingDiagnostics.PublisherIdTag, InProcessEventingRuntimeIds.PublisherId));
        dispatchActivity?.SetTag(
            EventingDiagnostics.PublicationIdTag,
            Redact(dispatchActivity, EventingDiagnostics.PublicationIdTag, publication.Id));
        dispatchActivity?.SetTag(
            EventingDiagnostics.ChannelIdTag,
            Redact(dispatchActivity, EventingDiagnostics.ChannelIdTag, publication.ChannelId));
        dispatchActivity?.SetTag(
            EventingDiagnostics.EventTypeTag,
            Redact(dispatchActivity, EventingDiagnostics.EventTypeTag, publication.EventType));

        var entries = executors.GetByChannelId(publication.ChannelId);
        var maxAttempts = InProcessEventingRetryPolicy.GetMaxAttempts(options);
        var retryDelayMilliseconds = InProcessEventingRetryPolicy.GetRetryDelayMilliseconds(options);
        var retryDelay = TimeSpan.FromMilliseconds(retryDelayMilliseconds);
        var idempotencyPolicy = InProcessEventingIdempotencyPolicy.GetPolicyId(options);
        var idempotencyRetentionMinutes = InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options);
        var matchedSubscriptionCount = entries.Count;
        var startedSubscriptionCount = 0;
        var succeededSubscriptionCount = 0;
        var failedSubscriptionCount = 0;
        var retryScheduledSubscriptionCount = 0;
        var skippedSubscriptionCount = 0;
        var subscriptionIds = entries
            .Select(static entry => entry.Subscription.Id)
            .OrderBy(static subscriptionId => subscriptionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (entries.Count == 0)
        {
            EventingLoggerMessages.LogPublicationDispatchSkipped(
                logger,
                InProcessEventingRuntimeIds.PublisherId,
                publication.Id,
                attempt: 1);
            await publicationRuntimeReporter.ReportAsync(
                new EventPublicationRuntimeReport(
                    publicationId: publication.Id,
                    channelId: publication.ChannelId,
                    eventType: publication.EventType,
                    outcome: EventPublicationRuntimeOutcomes.Skipped,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    matchedSubscriptionCount: 0,
                    metadata: CreatePublicationRuntimeMetadata(
                        publication,
                        EventPublicationRuntimeOutcomes.Skipped,
                        matchedSubscriptionCount: 0,
                        startedSubscriptionCount: 0,
                        succeededSubscriptionCount: 0,
                        failedSubscriptionCount: 0,
                        retryScheduledSubscriptionCount: 0,
                        skippedSubscriptionCount: 0,
                        maxAttempts,
                        retryDelayMilliseconds,
                        idempotencyPolicy,
                        idempotencyRetentionMinutes,
                        subscriptionIds,
                        skipReason: "no-matching-subscriptions")),
                cancellationToken).ConfigureAwait(false);
            CompleteDispatchActivity(
                dispatchActivity,
                publication,
                EventPublicationRuntimeOutcomes.Skipped,
                matchedSubscriptionCount: 0);
            return;
        }

        var failures = new List<Exception>();
        Exception? failureToRethrow = null;
        EventingLoggerMessages.LogPublicationDispatchStarted(
            logger,
            InProcessEventingRuntimeIds.PublisherId,
            publication.Id,
            attempt: 1);

        foreach (var entry in entries)
        {
            if (idempotencyTracker.TryGetCompleted(
                entry.Subscription.Id,
                publication.Id,
                DateTimeOffset.UtcNow,
                out var completedAtUtc))
            {
                skippedSubscriptionCount++;
                await runtimeReporter.ReportAsync(
                    new EventSubscriptionExecutionReport(
                        subscriptionId: entry.Subscription.Id,
                        outcome: EventSubscriptionExecutionOutcomes.Skipped,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        messageId: publication.Id,
                        attempt: 1,
                        metadata: CreateDuplicateSkippedMetadata(
                            CreateExecutionMetadata(
                                publication,
                                entry.Subscription,
                                attempt: 1,
                                maxAttempts,
                                retryDelayMilliseconds,
                                idempotencyPolicy,
                                idempotencyRetentionMinutes),
                            completedAtUtc)),
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            Exception? finalFailure = null;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var metadata = CreateExecutionMetadata(
                    publication,
                    entry.Subscription,
                    attempt,
                    maxAttempts,
                    retryDelayMilliseconds,
                    idempotencyPolicy,
                    idempotencyRetentionMinutes);
                startedSubscriptionCount++;
                await runtimeReporter.ReportAsync(
                    new EventSubscriptionExecutionReport(
                        subscriptionId: entry.Subscription.Id,
                        outcome: EventSubscriptionExecutionOutcomes.Started,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        messageId: publication.Id,
                        attempt: attempt,
                        metadata: metadata),
                    cancellationToken).ConfigureAwait(false);

                try
                {
                    await entry.Executor.ExecuteAsync(
                        new EventSubscriptionExecutionContext(
                            entry.Subscription,
                            publication,
                            attempt: attempt,
                            metadata: metadata),
                        cancellationToken).ConfigureAwait(false);

                    await runtimeReporter.ReportAsync(
                        new EventSubscriptionExecutionReport(
                            subscriptionId: entry.Subscription.Id,
                            outcome: EventSubscriptionExecutionOutcomes.Succeeded,
                            observedAtUtc: DateTimeOffset.UtcNow,
                            messageId: publication.Id,
                            attempt: attempt,
                            metadata: metadata),
                        cancellationToken).ConfigureAwait(false);

                    succeededSubscriptionCount++;
                    idempotencyTracker.MarkCompleted(entry.Subscription.Id, publication.Id, DateTimeOffset.UtcNow);
                    finalFailure = null;
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    finalFailure = exception;
                    if (attempt < maxAttempts)
                    {
                        retryScheduledSubscriptionCount++;
                        await runtimeReporter.ReportAsync(
                            new EventSubscriptionExecutionReport(
                                subscriptionId: entry.Subscription.Id,
                                outcome: EventSubscriptionExecutionOutcomes.RetryScheduled,
                                observedAtUtc: DateTimeOffset.UtcNow,
                                messageId: publication.Id,
                                attempt: attempt,
                                error: exception.Message,
                                metadata: CreateRetryScheduledMetadata(metadata, retryDelay)),
                            cancellationToken).ConfigureAwait(false);

                        if (retryDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                        }

                        continue;
                    }

                    failedSubscriptionCount++;
                    await runtimeReporter.ReportAsync(
                        new EventSubscriptionExecutionReport(
                            subscriptionId: entry.Subscription.Id,
                            outcome: EventSubscriptionExecutionOutcomes.Failed,
                            observedAtUtc: DateTimeOffset.UtcNow,
                            messageId: publication.Id,
                            attempt: attempt,
                            error: exception.Message,
                            metadata: metadata),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            if (finalFailure is not null)
            {
                failures.Add(finalFailure);
                if (!options.ContinueInProcessSubscriptionExecutionAfterFailure)
                {
                    failureToRethrow = finalFailure;
                    break;
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

            await publicationRuntimeReporter.ReportAsync(
                new EventPublicationRuntimeReport(
                    publicationId: publication.Id,
                    channelId: publication.ChannelId,
                    eventType: publication.EventType,
                    outcome: EventPublicationRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    matchedSubscriptionCount: matchedSubscriptionCount,
                    startedSubscriptionCount: startedSubscriptionCount,
                    succeededSubscriptionCount: succeededSubscriptionCount,
                    failedSubscriptionCount: failedSubscriptionCount,
                    retryScheduledSubscriptionCount: retryScheduledSubscriptionCount,
                    skippedSubscriptionCount: skippedSubscriptionCount,
                    error: message,
                    metadata: CreatePublicationRuntimeMetadata(
                        publication,
                        EventPublicationRuntimeOutcomes.Failed,
                        matchedSubscriptionCount,
                        startedSubscriptionCount,
                        succeededSubscriptionCount,
                        failedSubscriptionCount,
                        retryScheduledSubscriptionCount,
                        skippedSubscriptionCount,
                        maxAttempts,
                        retryDelayMilliseconds,
                        idempotencyPolicy,
                        idempotencyRetentionMinutes,
                        subscriptionIds,
                        error: message)),
                cancellationToken).ConfigureAwait(false);

            CompleteDispatchActivity(
                dispatchActivity,
                publication,
                EventPublicationRuntimeOutcomes.Failed,
                matchedSubscriptionCount,
                error: message);

            if (failureToRethrow is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failureToRethrow).Throw();
            }

            throw new InvalidOperationException(message, failures[0]);
        }

        EventingLoggerMessages.LogPublicationDispatchSucceeded(
            logger,
            InProcessEventingRuntimeIds.PublisherId,
            publication.Id,
            attempt: 1);

        var publicationOutcome = startedSubscriptionCount == 0 && skippedSubscriptionCount > 0
            ? EventPublicationRuntimeOutcomes.Skipped
            : EventPublicationRuntimeOutcomes.Succeeded;

        await publicationRuntimeReporter.ReportAsync(
            new EventPublicationRuntimeReport(
                publicationId: publication.Id,
                channelId: publication.ChannelId,
                eventType: publication.EventType,
                outcome: publicationOutcome,
                observedAtUtc: DateTimeOffset.UtcNow,
                matchedSubscriptionCount: matchedSubscriptionCount,
                startedSubscriptionCount: startedSubscriptionCount,
                succeededSubscriptionCount: succeededSubscriptionCount,
                failedSubscriptionCount: failedSubscriptionCount,
                retryScheduledSubscriptionCount: retryScheduledSubscriptionCount,
                skippedSubscriptionCount: skippedSubscriptionCount,
                metadata: CreatePublicationRuntimeMetadata(
                    publication,
                    publicationOutcome,
                    matchedSubscriptionCount,
                    startedSubscriptionCount,
                    succeededSubscriptionCount,
                    failedSubscriptionCount,
                    retryScheduledSubscriptionCount,
                    skippedSubscriptionCount,
                    maxAttempts,
                    retryDelayMilliseconds,
                    idempotencyPolicy,
                    idempotencyRetentionMinutes,
                    subscriptionIds,
                    skipReason: publicationOutcome == EventPublicationRuntimeOutcomes.Skipped
                        ? "duplicate-completed-subscriptions"
                        : null)),
            cancellationToken).ConfigureAwait(false);

        CompleteDispatchActivity(
            dispatchActivity,
            publication,
            publicationOutcome,
            matchedSubscriptionCount);
    }

    /// <summary>
    /// Sets the publication outcome tag, matched-subscription-count tag, optional error status,
    /// and increments the publication-dispatch counter for the given dispatch activity. Tag values
    /// are routed through the redaction pipeline so consumer-registered redaction filters apply
    /// uniformly across the dispatch span.
    /// </summary>
    private void CompleteDispatchActivity(
        Activity? dispatchActivity,
        EventPublication publication,
        string outcome,
        int matchedSubscriptionCount,
        string? error = null)
    {
        dispatchActivity?.SetTag(
            EventingDiagnostics.PublicationOutcomeTag,
            Redact(dispatchActivity, EventingDiagnostics.PublicationOutcomeTag, outcome));
        dispatchActivity?.SetTag(
            EventingDiagnostics.MatchedSubscriptionCountTag,
            Redact(dispatchActivity, EventingDiagnostics.MatchedSubscriptionCountTag, matchedSubscriptionCount));

        if (string.Equals(outcome, EventPublicationRuntimeOutcomes.Failed, StringComparison.OrdinalIgnoreCase))
        {
            dispatchActivity?.SetStatus(ActivityStatusCode.Error, error);
        }

        EventingDiagnostics.PublicationDispatchCounter.Add(
            1,
            new TagList
            {
                { EventingDiagnostics.ChannelIdTag, publication.ChannelId },
                { EventingDiagnostics.EventTypeTag, publication.EventType },
                { EventingDiagnostics.PublicationOutcomeTag, outcome }
            });
    }

    /// <summary>
    /// Routes <paramref name="value"/> through the consumer-registered <see cref="RedactionPipeline"/>
    /// (resolved through the publisher's optional ctor parameter) before the publisher emits it as
    /// an activity tag. The pipeline is empty by default when no consumer registered any
    /// <see cref="IRedactionFilter"/>; in that case (and when DI did not supply a pipeline at all)
    /// this method short-circuits to passthrough so dispatch emission stays cheap.
    /// </summary>
    private object? Redact(Activity? activity, string attributeKey, object? value)
    {
        if (redactionPipeline is null)
        {
            return value;
        }

        var context = new RedactionContext(
            ActivitySourceName: activity?.Source.Name,
            MeterName: null,
            AttributeKey: attributeKey,
            LoggerCategory: null);
        return redactionPipeline.Filter(context, value);
    }

    private static Dictionary<string, string> CreateExecutionMetadata(
        EventPublication publication,
        EventSubscriptionDescriptor subscription,
        int attempt,
        int maxAttempts,
        int retryDelayMilliseconds,
        string idempotencyPolicy,
        int idempotencyRetentionMinutes)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["publisherId"] = InProcessEventingRuntimeIds.PublisherId,
            ["trigger"] = InProcessEventingRuntimeIds.PublisherId,
            ["executionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
            ["executionOwnership"] = "cephalon-managed",
            ["executionMode"] = "in-process-direct",
            ["deliveryMode"] = "direct",
            ["retryPolicy"] = maxAttempts > 1 ? InProcessEventingRetryPolicy.BoundedInProcess : InProcessEventingRetryPolicy.None,
            ["retryMaxAttempts"] = maxAttempts.ToString(CultureInfo.InvariantCulture),
            ["retryDelayMilliseconds"] = retryDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
            ["retryDurability"] = "none",
            ["retryScope"] = "process-local",
            ["idempotencyPolicy"] = idempotencyPolicy,
            ["idempotencyKey"] = idempotencyPolicy == InProcessEventingIdempotencyPolicy.None
                ? InProcessEventingIdempotencyPolicy.None
                : InProcessEventingIdempotencyPolicy.KeyShape,
            ["idempotencyRetentionMinutes"] = idempotencyRetentionMinutes.ToString(CultureInfo.InvariantCulture),
            ["idempotencyDurability"] = InProcessEventingIdempotencyPolicy.Durability,
            ["idempotencyScope"] = idempotencyPolicy == InProcessEventingIdempotencyPolicy.None
                ? InProcessEventingIdempotencyPolicy.None
                : InProcessEventingIdempotencyPolicy.Scope,
            ["channelId"] = publication.ChannelId,
            ["eventType"] = publication.EventType,
            ["subscriptionId"] = subscription.Id,
            ["handlerId"] = subscription.HandlerId,
            ["attempt"] = attempt.ToString(CultureInfo.InvariantCulture),
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

    private static Dictionary<string, string> CreateRetryScheduledMetadata(
        IReadOnlyDictionary<string, string> metadata,
        TimeSpan retryDelay)
    {
        var retryMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["nextRetryAtUtc"] = DateTimeOffset.UtcNow.Add(retryDelay).ToString("O", CultureInfo.InvariantCulture)
        };

        return retryMetadata;
    }

    private static Dictionary<string, string> CreateDuplicateSkippedMetadata(
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset completedAtUtc)
    {
        var skippedMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["idempotencyOutcome"] = "duplicate-skipped",
            ["idempotencyCompletedAtUtc"] = completedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        };

        return skippedMetadata;
    }

    private static Dictionary<string, string> CreatePublicationRuntimeMetadata(
        EventPublication publication,
        string outcome,
        int matchedSubscriptionCount,
        int startedSubscriptionCount,
        int succeededSubscriptionCount,
        int failedSubscriptionCount,
        int retryScheduledSubscriptionCount,
        int skippedSubscriptionCount,
        int maxAttempts,
        int retryDelayMilliseconds,
        string idempotencyPolicy,
        int idempotencyRetentionMinutes,
        IReadOnlyList<string> subscriptionIds,
        string? skipReason = null,
        string? error = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["publisherId"] = InProcessEventingRuntimeIds.PublisherId,
            ["trigger"] = InProcessEventingRuntimeIds.PublisherId,
            ["publicationRuntimeState"] = "reported",
            ["publicationOutcome"] = outcome,
            ["publicationId"] = publication.Id,
            ["channelId"] = publication.ChannelId,
            ["eventType"] = publication.EventType,
            ["handoff"] = "in-process",
            ["dispatchRuntime"] = "cephalon-managed",
            ["dispatchStore"] = "not-configured",
            ["subscriptionExecution"] = "cephalon-managed",
            ["subscriptionExecutionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
            ["executionMode"] = "in-process-direct",
            ["deliveryMode"] = "direct",
            ["retryPolicy"] = maxAttempts > 1 ? InProcessEventingRetryPolicy.BoundedInProcess : InProcessEventingRetryPolicy.None,
            ["retryMaxAttempts"] = maxAttempts.ToString(CultureInfo.InvariantCulture),
            ["retryDelayMilliseconds"] = retryDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
            ["retryDurability"] = "none",
            ["retryScope"] = "process-local",
            ["idempotencyPolicy"] = idempotencyPolicy,
            ["idempotencyKey"] = idempotencyPolicy == InProcessEventingIdempotencyPolicy.None
                ? InProcessEventingIdempotencyPolicy.None
                : InProcessEventingIdempotencyPolicy.KeyShape,
            ["idempotencyRetentionMinutes"] = idempotencyRetentionMinutes.ToString(CultureInfo.InvariantCulture),
            ["idempotencyDurability"] = InProcessEventingIdempotencyPolicy.Durability,
            ["idempotencyScope"] = idempotencyPolicy == InProcessEventingIdempotencyPolicy.None
                ? InProcessEventingIdempotencyPolicy.None
                : InProcessEventingIdempotencyPolicy.Scope,
            ["matchedSubscriptionCount"] = matchedSubscriptionCount.ToString(CultureInfo.InvariantCulture),
            ["startedSubscriptionCount"] = startedSubscriptionCount.ToString(CultureInfo.InvariantCulture),
            ["succeededSubscriptionCount"] = succeededSubscriptionCount.ToString(CultureInfo.InvariantCulture),
            ["failedSubscriptionCount"] = failedSubscriptionCount.ToString(CultureInfo.InvariantCulture),
            ["retryScheduledSubscriptionCount"] = retryScheduledSubscriptionCount.ToString(CultureInfo.InvariantCulture),
            ["skippedSubscriptionCount"] = skippedSubscriptionCount.ToString(CultureInfo.InvariantCulture),
            ["subscriptionIds"] = string.Join(",", subscriptionIds),
            ["headerCount"] = publication.Headers.Count.ToString(CultureInfo.InvariantCulture),
            ["publicationMetadataCount"] = publication.Metadata.Count.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(skipReason))
        {
            metadata["skipReason"] = skipReason;
        }

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
}
