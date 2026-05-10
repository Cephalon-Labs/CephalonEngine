namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes aggregate operator-facing state recorded for event-dispatch remediation commands.
/// </summary>
public sealed class EventDispatchRemediationRuntimeSummary
{
    /// <summary>
    /// Gets an empty remediation command summary when no command results have been recorded.
    /// </summary>
    public static EventDispatchRemediationRuntimeSummary Empty { get; } = new();

    /// <summary>
    /// Creates a new remediation command summary.
    /// </summary>
    /// <param name="totalCommandCount">The total number of command-result records in the bounded runtime history.</param>
    /// <param name="acceptedCount">The number of recorded commands that were accepted.</param>
    /// <param name="rejectedCount">The number of recorded commands that were rejected.</param>
    /// <param name="errorCount">The number of recorded commands that carry an operator-facing error.</param>
    /// <param name="duplicateCommandCount">The number of recorded commands marked as duplicate command responses.</param>
    /// <param name="reservedCount">The number of recorded commands still reserved for mutation and not yet finalized.</param>
    /// <param name="lastCommandId">The most recently observed command identifier.</param>
    /// <param name="lastOperationId">The most recently observed remediation operation identifier.</param>
    /// <param name="lastOutcome">The most recently observed command outcome.</param>
    /// <param name="lastDispatchOutcome">The most recently observed dispatch-store outcome.</param>
    /// <param name="lastObservedAtUtc">The UTC timestamp for the most recent recorded command.</param>
    /// <param name="droppedCommandCount">The number of older command results dropped before this summary was calculated.</param>
    /// <param name="retentionTruncated">A value indicating whether the underlying bounded history has dropped older command results.</param>
    /// <param name="summaryMayBeIncomplete">A value indicating whether this summary may omit matching command results because retention truncated older history.</param>
    /// <param name="oldestRetainedCommandId">The oldest retained command identifier visible to this summary when one exists.</param>
    /// <param name="oldestRetainedObservedAtUtc">The UTC timestamp for the oldest retained command result visible to this summary when one exists.</param>
    /// <param name="oldestReservedCommandId">The oldest retained reserved command identifier visible to this summary when one exists.</param>
    /// <param name="oldestReservedObservedAtUtc">The UTC timestamp for the oldest retained reserved command visible to this summary when one exists.</param>
    public EventDispatchRemediationRuntimeSummary(
        int totalCommandCount = 0,
        int acceptedCount = 0,
        int rejectedCount = 0,
        int errorCount = 0,
        int duplicateCommandCount = 0,
        int reservedCount = 0,
        string? lastCommandId = null,
        string? lastOperationId = null,
        string? lastOutcome = null,
        string? lastDispatchOutcome = null,
        DateTimeOffset? lastObservedAtUtc = null,
        long droppedCommandCount = 0,
        bool retentionTruncated = false,
        bool summaryMayBeIncomplete = false,
        string? oldestRetainedCommandId = null,
        DateTimeOffset? oldestRetainedObservedAtUtc = null,
        string? oldestReservedCommandId = null,
        DateTimeOffset? oldestReservedObservedAtUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCommandCount);
        ArgumentOutOfRangeException.ThrowIfNegative(acceptedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rejectedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(errorCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duplicateCommandCount);
        ArgumentOutOfRangeException.ThrowIfNegative(reservedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(droppedCommandCount);

        TotalCommandCount = totalCommandCount;
        AcceptedCount = acceptedCount;
        RejectedCount = rejectedCount;
        ErrorCount = errorCount;
        DuplicateCommandCount = duplicateCommandCount;
        ReservedCount = reservedCount;
        LastCommandId = string.IsNullOrWhiteSpace(lastCommandId) ? null : lastCommandId.Trim();
        LastOperationId = string.IsNullOrWhiteSpace(lastOperationId) ? null : lastOperationId.Trim();
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim();
        LastDispatchOutcome = string.IsNullOrWhiteSpace(lastDispatchOutcome) ? null : lastDispatchOutcome.Trim();
        LastObservedAtUtc = lastObservedAtUtc;
        DroppedCommandCount = droppedCommandCount;
        RetentionTruncated = retentionTruncated || droppedCommandCount > 0;
        SummaryMayBeIncomplete = summaryMayBeIncomplete;
        OldestRetainedCommandId = string.IsNullOrWhiteSpace(oldestRetainedCommandId)
            ? null
            : oldestRetainedCommandId.Trim();
        OldestRetainedObservedAtUtc = oldestRetainedObservedAtUtc;
        OldestReservedCommandId = string.IsNullOrWhiteSpace(oldestReservedCommandId)
            ? null
            : oldestReservedCommandId.Trim();
        OldestReservedObservedAtUtc = oldestReservedObservedAtUtc;
    }

    /// <summary>
    /// Gets the total number of command-result records in the bounded runtime history.
    /// </summary>
    public int TotalCommandCount { get; }

    /// <summary>
    /// Gets the number of recorded commands that were accepted.
    /// </summary>
    public int AcceptedCount { get; }

    /// <summary>
    /// Gets the number of recorded commands that were rejected.
    /// </summary>
    public int RejectedCount { get; }

    /// <summary>
    /// Gets the number of recorded commands that carry an operator-facing error.
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// Gets the number of recorded commands marked as duplicate command responses.
    /// </summary>
    public int DuplicateCommandCount { get; }

    /// <summary>
    /// Gets the number of recorded commands still reserved for mutation and not yet finalized.
    /// </summary>
    public int ReservedCount { get; }

    /// <summary>
    /// Gets the most recently observed command identifier when one exists.
    /// </summary>
    public string? LastCommandId { get; }

    /// <summary>
    /// Gets the most recently observed remediation operation identifier when one exists.
    /// </summary>
    public string? LastOperationId { get; }

    /// <summary>
    /// Gets the most recently observed command outcome when one exists.
    /// </summary>
    public string? LastOutcome { get; }

    /// <summary>
    /// Gets the most recently observed dispatch-store outcome when one exists.
    /// </summary>
    public string? LastDispatchOutcome { get; }

    /// <summary>
    /// Gets the UTC timestamp for the most recent recorded command when one exists.
    /// </summary>
    public DateTimeOffset? LastObservedAtUtc { get; }

    /// <summary>
    /// Gets the number of older command results dropped before this summary was calculated.
    /// </summary>
    public long DroppedCommandCount { get; }

    /// <summary>
    /// Gets a value indicating whether the underlying bounded history has dropped older command results.
    /// </summary>
    public bool RetentionTruncated { get; }

    /// <summary>
    /// Gets a value indicating whether this summary may omit matching command results because retention truncated older history.
    /// </summary>
    public bool SummaryMayBeIncomplete { get; }

    /// <summary>
    /// Gets the oldest retained command identifier visible to this summary when one exists.
    /// </summary>
    public string? OldestRetainedCommandId { get; }

    /// <summary>
    /// Gets the UTC timestamp for the oldest retained command result visible to this summary when one exists.
    /// </summary>
    public DateTimeOffset? OldestRetainedObservedAtUtc { get; }

    /// <summary>
    /// Gets the oldest retained reserved command identifier visible to this summary when one exists.
    /// </summary>
    public string? OldestReservedCommandId { get; }

    /// <summary>
    /// Gets the UTC timestamp for the oldest retained reserved command visible to this summary when one exists.
    /// </summary>
    public DateTimeOffset? OldestReservedObservedAtUtc { get; }

    /// <summary>
    /// Gets a value indicating whether the summary includes any recorded command results.
    /// </summary>
    public bool HasCommands => TotalCommandCount > 0;

    /// <summary>
    /// Gets a value indicating whether any recorded command result was rejected or errored.
    /// </summary>
    public bool HasFailures => RejectedCount > 0 || ErrorCount > 0;

    /// <summary>
    /// Gets a value indicating whether any recorded command remains reserved and therefore in doubt.
    /// </summary>
    public bool HasInDoubtCommands => ReservedCount > 0;
}
