namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes the latest operator-facing runtime state reported for one declared event subscription.
/// </summary>
/// <param name="SubscriptionId">The stable declared subscription identifier.</param>
/// <param name="LastOutcome">The last reported outcome identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last observation was reported.</param>
/// <param name="LastMessageId">The last stable inbound message identifier when one was reported.</param>
/// <param name="LastAttempt">The last reported application-managed attempt number.</param>
/// <param name="StartedCount">The number of <c>started</c> observations reported so far.</param>
/// <param name="SucceededCount">The number of <c>succeeded</c> observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> observations reported so far.</param>
/// <param name="RetryScheduledCount">The number of <c>retry-scheduled</c> observations reported so far.</param>
/// <param name="SkippedCount">The number of <c>skipped</c> observations reported so far.</param>
/// <param name="LastError">The last operator-facing error summary when a failure was reported.</param>
/// <param name="Metadata">The operator-facing metadata captured by the latest report.</param>
public sealed record EventSubscriptionRuntimeState(
    string SubscriptionId,
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
    /// Gets the total number of observations reported for this subscription.
    /// </summary>
    public int TotalReports => StartedCount + SucceededCount + FailedCount + RetryScheduledCount + SkippedCount;

    /// <summary>
    /// Gets a value indicating whether the latest report says another retry attempt is pending.
    /// </summary>
    public bool RetryPending => string.Equals(LastOutcome, EventSubscriptionExecutionOutcomes.RetryScheduled, StringComparison.OrdinalIgnoreCase);
}
