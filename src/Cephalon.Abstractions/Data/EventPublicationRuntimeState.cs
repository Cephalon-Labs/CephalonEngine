namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the latest operator-facing runtime state reported for one event publication.
/// </summary>
/// <param name="PublicationId">The stable publication identifier.</param>
/// <param name="LastChannelId">The last stable channel identifier reported for this publication.</param>
/// <param name="LastEventType">The last stable event-type identifier reported for this publication.</param>
/// <param name="LastOutcome">The last reported publication outcome identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last publication observation was reported.</param>
/// <param name="AcceptedCount">The number of <c>accepted</c> publication observations reported so far.</param>
/// <param name="SucceededCount">The number of <c>succeeded</c> publication observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> publication observations reported so far.</param>
/// <param name="SkippedCount">The number of <c>skipped</c> publication observations reported so far.</param>
/// <param name="MatchedSubscriptionCount">The number of subscriptions matched by the latest publication observation.</param>
/// <param name="StartedSubscriptionCount">The number of subscription-start observations produced by the latest publication observation.</param>
/// <param name="SucceededSubscriptionCount">The number of subscription-success observations produced by the latest publication observation.</param>
/// <param name="FailedSubscriptionCount">The number of subscription-failure observations produced by the latest publication observation.</param>
/// <param name="RetryScheduledSubscriptionCount">The number of subscription-retry observations produced by the latest publication observation.</param>
/// <param name="SkippedSubscriptionCount">The number of subscription-skip observations produced by the latest publication observation.</param>
/// <param name="LastError">The last operator-facing error summary when a publication failure was reported.</param>
/// <param name="Metadata">The operator-facing metadata captured by the latest publication observation.</param>
public sealed record EventPublicationRuntimeState(
    string PublicationId,
    string? LastChannelId,
    string? LastEventType,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    int AcceptedCount,
    int SucceededCount,
    int FailedCount,
    int SkippedCount,
    int MatchedSubscriptionCount,
    int StartedSubscriptionCount,
    int SucceededSubscriptionCount,
    int FailedSubscriptionCount,
    int RetryScheduledSubscriptionCount,
    int SkippedSubscriptionCount,
    string? LastError,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the total number of publication observations reported for this publication id.
    /// </summary>
    public int TotalReports => AcceptedCount + SucceededCount + FailedCount + SkippedCount;

    /// <summary>
    /// Gets a value indicating whether the latest observation reported any subscription failures.
    /// </summary>
    public bool HasSubscriptionFailures => FailedSubscriptionCount > 0;

    /// <summary>
    /// Gets a value indicating whether the latest observation reported any skipped subscriptions.
    /// </summary>
    public bool HasSubscriptionSkips => SkippedSubscriptionCount > 0;
}
