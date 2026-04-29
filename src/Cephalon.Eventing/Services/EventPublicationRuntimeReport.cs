using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal sealed class EventPublicationRuntimeReport
{
    public EventPublicationRuntimeReport(
        string publicationId,
        string channelId,
        string eventType,
        string outcome,
        DateTimeOffset observedAtUtc,
        int matchedSubscriptionCount = 0,
        int startedSubscriptionCount = 0,
        int succeededSubscriptionCount = 0,
        int failedSubscriptionCount = 0,
        int retryScheduledSubscriptionCount = 0,
        int skippedSubscriptionCount = 0,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(publicationId))
        {
            throw new ArgumentException("Publication id is required.", nameof(publicationId));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(matchedSubscriptionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(startedSubscriptionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(succeededSubscriptionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(failedSubscriptionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(retryScheduledSubscriptionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(skippedSubscriptionCount);

        PublicationId = publicationId.Trim();
        ChannelId = channelId.Trim();
        EventType = eventType.Trim();
        Outcome = outcome.Trim();
        ObservedAtUtc = observedAtUtc;
        MatchedSubscriptionCount = matchedSubscriptionCount;
        StartedSubscriptionCount = startedSubscriptionCount;
        SucceededSubscriptionCount = succeededSubscriptionCount;
        FailedSubscriptionCount = failedSubscriptionCount;
        RetryScheduledSubscriptionCount = retryScheduledSubscriptionCount;
        SkippedSubscriptionCount = skippedSubscriptionCount;
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string PublicationId { get; }

    public string ChannelId { get; }

    public string EventType { get; }

    public string Outcome { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public int MatchedSubscriptionCount { get; }

    public int StartedSubscriptionCount { get; }

    public int SucceededSubscriptionCount { get; }

    public int FailedSubscriptionCount { get; }

    public int RetryScheduledSubscriptionCount { get; }

    public int SkippedSubscriptionCount { get; }

    public string? Error { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
