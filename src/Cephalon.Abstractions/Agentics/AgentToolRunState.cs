namespace Cephalon.Abstractions.Agentics;

/// <summary>
/// Describes the latest operator-facing runtime state reported for one agent-tool run.
/// </summary>
/// <param name="ToolId">The stable tool identifier.</param>
/// <param name="RunId">The stable run identifier.</param>
/// <param name="LastOutcome">The last reported outcome identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last observation was reported.</param>
/// <param name="LastActorId">The actor identifier from the latest observation when one was reported.</param>
/// <param name="LastCorrelationId">The correlation identifier from the latest observation when one was reported.</param>
/// <param name="LastAttempt">The last reported execution attempt number.</param>
/// <param name="StartedCount">The number of <c>started</c> observations reported so far.</param>
/// <param name="SucceededCount">The number of <c>succeeded</c> observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> observations reported so far.</param>
/// <param name="RetryScheduledCount">The number of <c>retry-scheduled</c> observations reported so far.</param>
/// <param name="SkippedCount">The number of <c>skipped</c> observations reported so far.</param>
/// <param name="ApprovalRequiredCount">The number of <c>approval-required</c> observations reported so far.</param>
/// <param name="DeniedCount">The number of <c>denied</c> observations reported so far.</param>
/// <param name="LastOutputSummary">The latest operator-facing output summary when one was reported.</param>
/// <param name="LastError">The latest operator-facing error summary when one was reported.</param>
/// <param name="Metadata">The operator-facing metadata captured by the latest report.</param>
public sealed record AgentToolRunState(
    string ToolId,
    string RunId,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    string? LastActorId,
    string? LastCorrelationId,
    int LastAttempt,
    int StartedCount,
    int SucceededCount,
    int FailedCount,
    int RetryScheduledCount,
    int SkippedCount,
    int ApprovalRequiredCount,
    int DeniedCount,
    string? LastOutputSummary,
    string? LastError,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the total number of observations reported for this run.
    /// </summary>
    public int TotalReports => StartedCount + SucceededCount + FailedCount + RetryScheduledCount + SkippedCount + ApprovalRequiredCount + DeniedCount;

    /// <summary>
    /// Gets a value indicating whether the latest report says explicit approval is required before execution can continue.
    /// </summary>
    public bool RequiresApproval => string.Equals(LastOutcome, AgentToolExecutionOutcomes.ApprovalRequired, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the latest report says another process-local attempt is pending.
    /// </summary>
    public bool RetryPending => string.Equals(LastOutcome, AgentToolExecutionOutcomes.RetryScheduled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the latest report represents a process-local duplicate-completed suppression.
    /// </summary>
    public bool DuplicateCompleted =>
        string.Equals(LastOutcome, AgentToolExecutionOutcomes.Skipped, StringComparison.OrdinalIgnoreCase) &&
        Metadata.TryGetValue("idempotencyOutcome", out var outcome) &&
        string.Equals(outcome, "duplicate-skipped", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the latest report represents a terminal outcome for this run.
    /// </summary>
    public bool IsTerminal =>
        string.Equals(LastOutcome, AgentToolExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(LastOutcome, AgentToolExecutionOutcomes.Failed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(LastOutcome, AgentToolExecutionOutcomes.Skipped, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(LastOutcome, AgentToolExecutionOutcomes.Denied, StringComparison.OrdinalIgnoreCase);
}
