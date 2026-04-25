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

        var normalizedAttempt = Math.Max(1, attempt);
        await ReportAsync(
            CreateReport(entry, request.Publication, normalizedAttempt, EventSubscriptionExecutionOutcomes.Started),
            cancellationToken).ConfigureAwait(false);

        try
        {
            var executionContext = new EventSubscriptionExecutionContext(
                entry.Subscription,
                request.Publication,
                normalizedAttempt,
                metadata: CreateExecutionMetadata(entry, request.Publication, normalizedAttempt));
            await entry.Executor.ExecuteAsync(executionContext, cancellationToken).ConfigureAwait(false);

            await ReportAsync(
                CreateReport(entry, request.Publication, normalizedAttempt, EventSubscriptionExecutionOutcomes.Succeeded),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var retryOptions = new DeliveryOptions
            {
                ScheduleDelay = TimeSpan.FromSeconds(Math.Max(1, options.SubscriptionRetryDelaySeconds)),
                TenantId = request.Publication.TenantId
            };

            await ReportAsync(
                CreateRetryReport(entry, request.Publication, normalizedAttempt, exception.Message, exception),
                cancellationToken).ConfigureAwait(false);
            await messageBus.SendAsync(request, retryOptions).ConfigureAwait(false);
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

    private EventSubscriptionExecutionReport CreateRetryReport(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        string error,
        Exception exception)
    {
        var nextRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, options.SubscriptionRetryDelaySeconds));
        var metadata = CreateObservationMetadata(
            entry,
            publication,
            attempt,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["nextRetryAtUtc"] = nextRetryAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["retryPolicy"] = "fixed-delay",
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

    private static EventSubscriptionExecutionReport CreateReport(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
        string outcome)
    {
        return new EventSubscriptionExecutionReport(
            subscriptionId: entry.Subscription.Id,
            outcome: outcome,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: publication.Id,
            attempt: attempt,
            metadata: CreateObservationMetadata(entry, publication, attempt));
    }

    private static Dictionary<string, string> CreateExecutionMetadata(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt)
    {
        var metadata = CreateObservationMetadata(entry, publication, attempt);
        metadata["messageId"] = publication.Id;
        return metadata;
    }

    private static Dictionary<string, string> CreateObservationMetadata(
        WolverineManagedEventSubscriptionExecutorCatalog.ManagedSubscriptionEntry entry,
        EventPublication publication,
        int attempt,
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
