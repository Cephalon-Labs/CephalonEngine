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
    /// <param name="lastCommandId">The most recently observed command identifier.</param>
    /// <param name="lastOperationId">The most recently observed remediation operation identifier.</param>
    /// <param name="lastOutcome">The most recently observed command outcome.</param>
    /// <param name="lastDispatchOutcome">The most recently observed dispatch-store outcome.</param>
    /// <param name="lastObservedAtUtc">The UTC timestamp for the most recent recorded command.</param>
    public EventDispatchRemediationRuntimeSummary(
        int totalCommandCount = 0,
        int acceptedCount = 0,
        int rejectedCount = 0,
        int errorCount = 0,
        int duplicateCommandCount = 0,
        string? lastCommandId = null,
        string? lastOperationId = null,
        string? lastOutcome = null,
        string? lastDispatchOutcome = null,
        DateTimeOffset? lastObservedAtUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCommandCount);
        ArgumentOutOfRangeException.ThrowIfNegative(acceptedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rejectedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(errorCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duplicateCommandCount);

        TotalCommandCount = totalCommandCount;
        AcceptedCount = acceptedCount;
        RejectedCount = rejectedCount;
        ErrorCount = errorCount;
        DuplicateCommandCount = duplicateCommandCount;
        LastCommandId = string.IsNullOrWhiteSpace(lastCommandId) ? null : lastCommandId.Trim();
        LastOperationId = string.IsNullOrWhiteSpace(lastOperationId) ? null : lastOperationId.Trim();
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim();
        LastDispatchOutcome = string.IsNullOrWhiteSpace(lastDispatchOutcome) ? null : lastDispatchOutcome.Trim();
        LastObservedAtUtc = lastObservedAtUtc;
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
    /// Gets a value indicating whether the summary includes any recorded command results.
    /// </summary>
    public bool HasCommands => TotalCommandCount > 0;

    /// <summary>
    /// Gets a value indicating whether any recorded command result was rejected or errored.
    /// </summary>
    public bool HasFailures => RejectedCount > 0 || ErrorCount > 0;
}
