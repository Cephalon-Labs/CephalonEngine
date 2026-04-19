using Cephalon.Abstractions.Execution;

namespace Cephalon.Behaviors.Patterns.Runtime;

/// <summary>
/// Describes one reported durable-execution observation for an active stream.
/// </summary>
internal sealed class DurableExecutionExecutionReport
{
    public DurableExecutionExecutionReport(
        string behaviorId,
        string streamId,
        string outcome,
        string stage,
        DateTimeOffset observedAtUtc,
        long? replayedVersion = null,
        long? knownVersion = null,
        int? httpStatusCode = null,
        int appendedEventCount = 0,
        bool producedOutput = false,
        bool isCompleted = false,
        IReadOnlyList<DurableExecutionPendingTimer>? pendingTimers = null,
        IReadOnlyList<DurableExecutionPendingSignal>? pendingSignals = null,
        IReadOnlyList<DurableExecutionCompensationAction>? compensationActions = null,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("Behavior id is required.", nameof(behaviorId));
        }

        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        if (string.IsNullOrWhiteSpace(stage))
        {
            throw new ArgumentException("Stage is required.", nameof(stage));
        }

        if (appendedEventCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(appendedEventCount),
                appendedEventCount,
                "Appended event count must be greater than or equal to 0.");
        }

        if (httpStatusCode is < 100 or > 999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(httpStatusCode),
                httpStatusCode,
                "HTTP status codes must stay in the HTTP status-code range.");
        }

        BehaviorId = behaviorId.Trim();
        StreamId = streamId.Trim();
        Outcome = outcome.Trim();
        Stage = stage.Trim();
        ObservedAtUtc = observedAtUtc;
        ReplayedVersion = replayedVersion;
        KnownVersion = knownVersion;
        HttpStatusCode = httpStatusCode;
        AppendedEventCount = appendedEventCount;
        ProducedOutput = producedOutput;
        IsCompleted = isCompleted;
        PendingTimers = NormalizePendingTimers(pendingTimers);
        PendingSignals = NormalizePendingSignals(pendingSignals);
        CompensationActions = NormalizeCompensationActions(compensationActions);
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string BehaviorId { get; }

    public string StreamId { get; }

    public string Outcome { get; }

    public string Stage { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public long? ReplayedVersion { get; }

    public long? KnownVersion { get; }

    public int? HttpStatusCode { get; }

    public int AppendedEventCount { get; }

    public bool ProducedOutput { get; }

    public bool IsCompleted { get; }

    public IReadOnlyList<DurableExecutionPendingTimer> PendingTimers { get; }

    public IReadOnlyList<DurableExecutionPendingSignal> PendingSignals { get; }

    public IReadOnlyList<DurableExecutionCompensationAction> CompensationActions { get; }

    public string? Error { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static DurableExecutionPendingTimer[] NormalizePendingTimers(
        IReadOnlyList<DurableExecutionPendingTimer>? pendingTimers)
    {
        if (pendingTimers is null || pendingTimers.Count == 0)
        {
            return [];
        }

        if (pendingTimers.Any(static timer => timer is null))
        {
            throw new ArgumentException("Pending timers cannot contain null entries.", nameof(pendingTimers));
        }

        var duplicateTimerId = pendingTimers
            .GroupBy(static timer => timer.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;
        if (!string.IsNullOrWhiteSpace(duplicateTimerId))
        {
            throw new ArgumentException(
                $"Pending timer id '{duplicateTimerId}' was declared more than once.",
                nameof(pendingTimers));
        }

        return pendingTimers
            .OrderBy(static timer => timer.DueAtUtc)
            .ThenBy(static timer => timer.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DurableExecutionPendingSignal[] NormalizePendingSignals(
        IReadOnlyList<DurableExecutionPendingSignal>? pendingSignals)
    {
        if (pendingSignals is null || pendingSignals.Count == 0)
        {
            return [];
        }

        if (pendingSignals.Any(static signal => signal is null))
        {
            throw new ArgumentException("Pending signals cannot contain null entries.", nameof(pendingSignals));
        }

        var duplicateSignalId = pendingSignals
            .GroupBy(static signal => signal.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;
        if (!string.IsNullOrWhiteSpace(duplicateSignalId))
        {
            throw new ArgumentException(
                $"Pending signal id '{duplicateSignalId}' was declared more than once.",
                nameof(pendingSignals));
        }

        return pendingSignals
            .OrderBy(static signal => signal.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DurableExecutionCompensationAction[] NormalizeCompensationActions(
        IReadOnlyList<DurableExecutionCompensationAction>? compensationActions)
    {
        if (compensationActions is null || compensationActions.Count == 0)
        {
            return [];
        }

        if (compensationActions.Any(static action => action is null))
        {
            throw new ArgumentException(
                "Compensation actions cannot contain null entries.",
                nameof(compensationActions));
        }

        var duplicateCompensationActionId = compensationActions
            .GroupBy(static action => action.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;
        if (!string.IsNullOrWhiteSpace(duplicateCompensationActionId))
        {
            throw new ArgumentException(
                $"Compensation action id '{duplicateCompensationActionId}' was declared more than once.",
                nameof(compensationActions));
        }

        return compensationActions
            .OrderBy(static action => action.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
