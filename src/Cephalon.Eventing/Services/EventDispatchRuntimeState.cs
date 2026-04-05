namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes the latest operator-facing runtime state reported for one durable event-dispatch path.
/// </summary>
/// <param name="OutboxId">The stable outbox identifier that owns the dispatch path.</param>
/// <param name="LastChannelId">The last stable channel identifier reported for this dispatch path.</param>
/// <param name="LastOutcome">The last reported outcome identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last observation was reported.</param>
/// <param name="LastMessageId">The last stable outbound message identifier when one was reported.</param>
/// <param name="LastAttempt">The last reported dispatch attempt number.</param>
/// <param name="StartedCount">The number of <c>started</c> observations reported so far.</param>
/// <param name="SucceededCount">The number of <c>succeeded</c> observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> observations reported so far.</param>
/// <param name="RetryScheduledCount">The number of <c>retry-scheduled</c> observations reported so far.</param>
/// <param name="SkippedCount">The number of <c>skipped</c> observations reported so far.</param>
/// <param name="LastError">The last operator-facing error summary when a failure was reported.</param>
/// <param name="Metadata">The operator-facing metadata captured by the latest report.</param>
public sealed record EventDispatchRuntimeState(
    string OutboxId,
    string? LastChannelId,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    string? LastMessageId,
    int LastAttempt,
    int StartedCount,
    int SucceededCount,
    int FailedCount,
    int RetryScheduledCount,
    int SkippedCount,
    string? LastError,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the total number of observations reported for this dispatch path.
    /// </summary>
    public int TotalReports => StartedCount + SucceededCount + FailedCount + RetryScheduledCount + SkippedCount;

    /// <summary>
    /// Gets a value indicating whether the latest report says another retry attempt is pending.
    /// </summary>
    public bool RetryPending => string.Equals(LastOutcome, EventDispatchExecutionOutcomes.RetryScheduled, StringComparison.OrdinalIgnoreCase);
}
