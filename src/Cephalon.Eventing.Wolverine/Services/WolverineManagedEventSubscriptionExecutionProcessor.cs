using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineManagedEventSubscriptionExecutionProcessor(
    WolverineManagedEventSubscriptionExecutorCatalog executors,
    WolverineEventingOptions options,
    IEventSubscriptionRuntimeReporter runtimeReporter,
    ILogger<WolverineManagedEventSubscriptionExecutionProcessor> logger)
{
    private static readonly Action<ILogger, string, string, Exception?> LogRuntimeObservationProjectionFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(4306, "WolverineSubscriptionObservationProjectionFailed"),
            "Wolverine-managed subscription execution could not project runtime observation '{Outcome}' for subscription '{SubscriptionId}'.");

    public async Task ProcessAsync(
        WolverineManagedEventSubscriptionExecutionRequest request,
        int attempt,
        IMessageBus messageBus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(messageBus);
        cancellationToken.ThrowIfCancellationRequested();

        if (!executors.TryGet(request.SubscriptionId, out var entry))
        {
            throw new InvalidOperationException(
                $"Managed subscription execution request references unknown subscription '{request.SubscriptionId}'.");
        }

        var normalizedAttempt = Math.Max(request.Attempt, Math.Max(1, attempt));
        var maxAttempts = WolverineEventingRetryPolicy.GetSubscriptionMaxAttempts(options);
        var retryDelaySeconds = WolverineEventingRetryPolicy.GetSubscriptionRetryDelaySeconds(options);
        await ReportAsync(
            CreateReport(entry, request.Publication, normalizedAttempt, maxAttempts, retryDelaySeconds, EventSubscriptionExecutionOutcomes.Started),
            cancellationToken).ConfigureAwait(false);

        try
        {
            var executionContext = new EventSubscriptionExecutionContext(
                entry.Subscription,
                request.Publication,
                normalizedAttempt,
                metadata: CreateExecutionMetadata(entry, request.Publication, normalizedAttempt, maxAttempts, retryDelaySeconds));
            await entry.Executor.ExecuteAsync(executionContext, cancellationToken).ConfigureAwait(false);

            await ReportAsync(
                CreateReport(entry, request.Publication, normalizedAttempt, maxAttempts, retryDelaySeconds, EventSubscriptionExecutionOutcomes.Succeeded),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (normalizedAttempt >= maxAttempts)
            {
                await ReportAsync(
                    CreateTerminalFailureReport(entry, request.Publication, normalizedAttempt, maxAttempts, retryDelaySeconds, exception.Message, exception),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            var retryOptions = new DeliveryOptions
            {
                ScheduleDelay = TimeSpan.FromSeconds(retryDelaySeconds),
                TenantId = request.Publication.TenantId
            };

            await ReportAsync(
                CreateRetryReport(entry, request.Publication, normalizedAttempt, maxAttempts, retryDelaySeconds, exception.Message, exception),
                cancellationToken).ConfigureAwait(false);
            await messageBus.SendAsync(
                new WolverineManagedEventSubscriptionExecutionRequest(
                    request.SubscriptionId,
                    request.Publication,
                    attempt: normalizedAttempt + 1),
                retryOptions).ConfigureAwait(false);
        }
    }

    private async Task ReportAsync(
        EventSubscriptionExecutionReport report,
        CancellationToken cancellationToken)
    {
        try
        {
            await runtimeReporter.ReportAsync(report, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogRuntimeObservationProjectionFailed(logger, report.Outcome, report.SubscriptionId, exception);
        }
    }

    private static EventSubscriptionExecutionReport CreateRetryReport(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        int maxAttempts,
        int retryDelaySeconds,
        string error,
        Exception exception)
    {
        var nextRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(retryDelaySeconds);
        var metadata = CreateObservationMetadata(
            entry,
            publication,
            attempt,
            maxAttempts,
            retryDelaySeconds,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["nextRetryAtUtc"] = nextRetryAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["retryOutcome"] = "retry-scheduled",
                ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name
            });

        return new EventSubscriptionExecutionReport(
            subscriptionId: entry.Subscription.Id,
            outcome: EventSubscriptionExecutionOutcomes.RetryScheduled,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: publication.Id,
            attempt: attempt,
            error: error,
            metadata: metadata);
    }

    private static EventSubscriptionExecutionReport CreateTerminalFailureReport(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        int maxAttempts,
        int retryDelaySeconds,
        string error,
        Exception exception)
    {
        var metadata = CreateObservationMetadata(
            entry,
            publication,
            attempt,
            maxAttempts,
            retryDelaySeconds,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["retryOutcome"] = "max-attempts-exhausted",
                ["retryExhausted"] = "true",
                ["terminalFailure"] = "true",
                ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name
            });

        return new EventSubscriptionExecutionReport(
            subscriptionId: entry.Subscription.Id,
            outcome: EventSubscriptionExecutionOutcomes.Failed,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: publication.Id,
            attempt: attempt,
            error: error,
            metadata: metadata);
    }

    private static EventSubscriptionExecutionReport CreateReport(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        int maxAttempts,
        int retryDelaySeconds,
        string outcome)
    {
        return new EventSubscriptionExecutionReport(
            subscriptionId: entry.Subscription.Id,
            outcome: outcome,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: publication.Id,
            attempt: attempt,
            metadata: CreateObservationMetadata(entry, publication, attempt, maxAttempts, retryDelaySeconds));
    }

    private static Dictionary<string, string> CreateExecutionMetadata(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        int maxAttempts,
        int retryDelaySeconds)
    {
        var metadata = CreateObservationMetadata(entry, publication, attempt, maxAttempts, retryDelaySeconds);
        metadata["messageId"] = publication.Id;
        return metadata;
    }

    private static Dictionary<string, string> CreateObservationMetadata(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        int maxAttempts,
        int retryDelaySeconds,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        var metadata = new Dictionary<string, string>(publication.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["adapter"] = "wolverine",
            ["technology"] = "event-driven-integration",
            ["channelId"] = publication.ChannelId,
            ["handlerId"] = entry.Subscription.HandlerId,
            ["subscriptionExecutionRuntimeId"] = WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId,
            ["subscriptionOwnership"] = "wolverine-managed",
            ["subscriptionExecutionMode"] = "message-handler",
            ["deliveryMode"] = entry.Subscription.DeliveryMode,
            ["transport"] = "wolverine",
            ["attempt"] = attempt.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["retryPolicy"] = maxAttempts > 1 ? WolverineEventingRetryPolicy.BoundedFixedDelay : WolverineEventingRetryPolicy.None,
            ["retryMaxAttempts"] = maxAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["retryDelaySeconds"] = retryDelaySeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["retryDurability"] = "wolverine-scheduled-message",
            ["retryScope"] = "provider-managed",
            ["contentType"] = publication.ContentType ?? "not-configured",
            ["headerCount"] = publication.Headers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(publication.CorrelationId))
        {
            metadata["correlationId"] = publication.CorrelationId;
        }

        if (!string.IsNullOrWhiteSpace(publication.TenantId))
        {
            metadata["tenantId"] = publication.TenantId;
        }

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static void LogRuntimeObservationProjectionFailed(
        ILogger logger,
        string outcome,
        string subscriptionId,
        Exception exception)
    {
        LogRuntimeObservationProjectionFailedMessage(logger, outcome, subscriptionId, exception);
    }
}
