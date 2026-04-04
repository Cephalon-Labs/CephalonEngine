using Cephalon.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRuntimeCatalog(
    IOutboxCatalog outboxes,
    IEventChannelCatalog channels,
    ILoggerFactory? loggerFactory = null) : IEventDispatchRuntimeCatalog, IEventDispatchRuntimeReporter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Lock gate = new();
    private readonly Dictionary<string, EventDispatchRuntimeState> states = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger logger = (loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<EventDispatchRuntimeCatalog>();

    public IReadOnlyList<EventDispatchRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return states.Values
                    .OrderBy(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public EventDispatchRuntimeState? GetByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);

        lock (gate)
        {
            return states.GetValueOrDefault(outboxId.Trim());
        }
    }

    public bool TryGet(string outboxId, out EventDispatchRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);

        lock (gate)
        {
            return states.TryGetValue(outboxId.Trim(), out state);
        }
    }

    public ValueTask ReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        if (outboxes.GetById(report.OutboxId) is null)
        {
            throw new InvalidOperationException(
                $"Outbox '{report.OutboxId}' is not registered in the active eventing runtime.");
        }

        if (!channels.TryGet(report.ChannelId, out _))
        {
            throw new InvalidOperationException(
                $"Event channel '{report.ChannelId}' is not registered in the active eventing runtime.");
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
            var current = states.TryGetValue(report.OutboxId, out var existing)
                ? existing
                : new EventDispatchRuntimeState(
                    OutboxId: report.OutboxId,
                    LastChannelId: null,
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
                EventDispatchExecutionOutcomes.Started => current with
                {
                    LastChannelId = report.ChannelId,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    StartedCount = current.StartedCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                EventDispatchExecutionOutcomes.Succeeded => current with
                {
                    LastChannelId = report.ChannelId,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    SucceededCount = current.SucceededCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                EventDispatchExecutionOutcomes.Failed => current with
                {
                    LastChannelId = report.ChannelId,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    FailedCount = current.FailedCount + 1,
                    LastError = report.Error,
                    Metadata = metadata
                },
                EventDispatchExecutionOutcomes.RetryScheduled => current with
                {
                    LastChannelId = report.ChannelId,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    RetryScheduledCount = current.RetryScheduledCount + 1,
                    LastError = report.Error,
                    Metadata = metadata
                },
                EventDispatchExecutionOutcomes.Skipped => current with
                {
                    LastChannelId = report.ChannelId,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    LastMessageId = report.MessageId,
                    LastAttempt = report.Attempt,
                    SkippedCount = current.SkippedCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"Dispatch outcome '{report.Outcome}' is not supported by the active eventing runtime.")
            };

            states[report.OutboxId] = current;
        }

        LogObservation(this.logger, report.OutboxId, report.ChannelId, report.MessageId, report.Attempt, normalizedOutcome, report.Error);

        return ValueTask.CompletedTask;
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            EventDispatchExecutionOutcomes.Started => EventDispatchExecutionOutcomes.Started,
            EventDispatchExecutionOutcomes.Succeeded => EventDispatchExecutionOutcomes.Succeeded,
            EventDispatchExecutionOutcomes.Failed => EventDispatchExecutionOutcomes.Failed,
            EventDispatchExecutionOutcomes.RetryScheduled => EventDispatchExecutionOutcomes.RetryScheduled,
            EventDispatchExecutionOutcomes.Skipped => EventDispatchExecutionOutcomes.Skipped,
            _ => throw new InvalidOperationException(
                $"Dispatch outcome '{outcome}' is not supported by the active eventing runtime.")
        };
    }

    private static void LogObservation(
        ILogger logger,
        string outboxId,
        string channelId,
        string? messageId,
        int attempt,
        string outcome,
        string? error)
    {
        var safeMessageId = string.IsNullOrWhiteSpace(messageId) ? "<not-reported>" : messageId;
        switch (outcome)
        {
            case EventDispatchExecutionOutcomes.Started:
                EventingLoggerMessages.LogDispatchStarted(logger, outboxId, channelId, safeMessageId, attempt);
                break;

            case EventDispatchExecutionOutcomes.Succeeded:
                EventingLoggerMessages.LogDispatchSucceeded(logger, outboxId, channelId, safeMessageId, attempt);
                break;

            case EventDispatchExecutionOutcomes.Failed:
                EventingLoggerMessages.LogDispatchFailed(
                    logger,
                    outboxId,
                    channelId,
                    safeMessageId,
                    attempt,
                    error ?? "<not-reported>");
                break;

            case EventDispatchExecutionOutcomes.RetryScheduled:
                EventingLoggerMessages.LogDispatchRetryScheduled(
                    logger,
                    outboxId,
                    channelId,
                    safeMessageId,
                    attempt,
                    error ?? "<not-reported>");
                break;

            case EventDispatchExecutionOutcomes.Skipped:
                EventingLoggerMessages.LogDispatchSkipped(logger, outboxId, channelId, safeMessageId, attempt);
                break;
        }
    }
}
