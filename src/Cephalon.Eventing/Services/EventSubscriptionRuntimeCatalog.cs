using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Eventing.Services;

internal sealed class EventSubscriptionRuntimeCatalog(
    IEventSubscriptionCatalog subscriptions,
    ILoggerFactory? loggerFactory = null) : IEventSubscriptionRuntimeCatalog, IEventSubscriptionRuntimeReporter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Lock gate = new();
    private readonly Dictionary<string, EventSubscriptionRuntimeState> states = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger logger = (loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<EventSubscriptionRuntimeCatalog>();

    public IReadOnlyList<EventSubscriptionRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return states.Values
                    .OrderBy(static state => state.SubscriptionId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public EventSubscriptionRuntimeState? GetById(string subscriptionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        lock (gate)
        {
            return states.GetValueOrDefault(subscriptionId.Trim());
        }
    }

    public bool TryGet(string subscriptionId, out EventSubscriptionRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        lock (gate)
        {
            return states.TryGetValue(subscriptionId.Trim(), out state);
        }
    }

    public ValueTask ReportAsync(
        EventSubscriptionExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        if (!subscriptions.TryGet(report.SubscriptionId, out _))
        {
            throw new InvalidOperationException(
                $"Event subscription '{report.SubscriptionId}' is not registered in the active eventing runtime.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;
        var metadata = report.Metadata.Count == 0
            ? EmptyMetadata
            : new Dictionary<string, string>(report.Metadata, StringComparer.OrdinalIgnoreCase);

        lock (gate)
        {
            var current = states.TryGetValue(report.SubscriptionId, out var existing)
                ? existing
                : new EventSubscriptionRuntimeState(
                    SubscriptionId: report.SubscriptionId,
                    LastOutcome: null,
                    LastObservedAtUtc: null,
                    LastMessageId: null,
                    LastAttempt: 0,
                    StartedCount: 0,
                    SucceededCount: 0,
                    FailedCount: 0,
                    RetryScheduledCount: 0,
                    SkippedCount: 0,
                    LastError: null,
                    Metadata: EmptyMetadata);

            current = normalizedOutcome switch
            {
                EventSubscriptionExecutionOutcomes.Started => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    StartedCount = current.StartedCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                EventSubscriptionExecutionOutcomes.Succeeded => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    SucceededCount = current.SucceededCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                EventSubscriptionExecutionOutcomes.Failed => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    FailedCount = current.FailedCount + 1,
                    LastError = report.Error,
                    Metadata = metadata
                },
                EventSubscriptionExecutionOutcomes.RetryScheduled => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    RetryScheduledCount = current.RetryScheduledCount + 1,
                    LastError = report.Error,
                    Metadata = metadata
                },
                EventSubscriptionExecutionOutcomes.Skipped => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    SkippedCount = current.SkippedCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"Event subscription outcome '{report.Outcome}' is not supported by the active eventing runtime.")
            };

            states[report.SubscriptionId] = current;
        }

        LogObservation(this.logger, report.SubscriptionId, report.MessageId, report.Attempt, normalizedOutcome, report.Error);

        return ValueTask.CompletedTask;
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            EventSubscriptionExecutionOutcomes.Started => EventSubscriptionExecutionOutcomes.Started,
            EventSubscriptionExecutionOutcomes.Succeeded => EventSubscriptionExecutionOutcomes.Succeeded,
            EventSubscriptionExecutionOutcomes.Failed => EventSubscriptionExecutionOutcomes.Failed,
            EventSubscriptionExecutionOutcomes.RetryScheduled => EventSubscriptionExecutionOutcomes.RetryScheduled,
            EventSubscriptionExecutionOutcomes.Skipped => EventSubscriptionExecutionOutcomes.Skipped,
            _ => throw new InvalidOperationException(
                $"Event subscription outcome '{outcome}' is not supported by the active eventing runtime.")
        };
    }

    private static void LogObservation(
        ILogger logger,
        string subscriptionId,
        string? messageId,
        int attempt,
        string outcome,
        string? error)
    {
        var safeMessageId = string.IsNullOrWhiteSpace(messageId) ? "<not-reported>" : messageId;
        switch (outcome)
        {
            case EventSubscriptionExecutionOutcomes.Started:
                EventingLoggerMessages.LogSubscriptionStarted(logger, subscriptionId, safeMessageId, attempt);
                break;

            case EventSubscriptionExecutionOutcomes.Succeeded:
                EventingLoggerMessages.LogSubscriptionSucceeded(logger, subscriptionId, safeMessageId, attempt);
                break;

            case EventSubscriptionExecutionOutcomes.Failed:
                EventingLoggerMessages.LogSubscriptionFailed(
                    logger,
                    subscriptionId,
                    safeMessageId,
                    attempt,
                    error ?? "<not-reported>");
                break;

            case EventSubscriptionExecutionOutcomes.RetryScheduled:
                EventingLoggerMessages.LogSubscriptionRetryScheduled(
                    logger,
                    subscriptionId,
                    safeMessageId,
                    attempt,
                    error ?? "<not-reported>");
                break;

            case EventSubscriptionExecutionOutcomes.Skipped:
                EventingLoggerMessages.LogSubscriptionSkipped(logger, subscriptionId, safeMessageId, attempt);
                break;
        }
    }
}
