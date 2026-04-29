using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal sealed class EventPublicationRuntimeCatalog(
    IEventChannelCatalog channels) : IEventPublicationRuntimeCatalog, IEventPublicationRuntimeReporter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Lock gate = new();
    private readonly Dictionary<string, EventPublicationRuntimeState> states = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<EventPublicationRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return states.Values
                    .OrderBy(static state => state.PublicationId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public EventPublicationRuntimeState? GetByPublicationId(string publicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicationId);

        lock (gate)
        {
            return states.GetValueOrDefault(publicationId.Trim());
        }
    }

    public IReadOnlyList<EventPublicationRuntimeState> GetByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return states.Values
                .Where(state => string.Equals(state.LastChannelId, normalizedChannelId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(static state => state.PublicationId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool TryGet(string publicationId, out EventPublicationRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicationId);

        lock (gate)
        {
            return states.TryGetValue(publicationId.Trim(), out state);
        }
    }

    public ValueTask ReportAsync(
        EventPublicationRuntimeReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

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
            var current = states.TryGetValue(report.PublicationId, out var existing)
                ? existing
                : new EventPublicationRuntimeState(
                    PublicationId: report.PublicationId,
                    LastChannelId: null,
                    LastEventType: null,
                    LastOutcome: null,
                    LastObservedAtUtc: null,
                    AcceptedCount: 0,
                    SucceededCount: 0,
                    FailedCount: 0,
                    SkippedCount: 0,
                    MatchedSubscriptionCount: 0,
                    StartedSubscriptionCount: 0,
                    SucceededSubscriptionCount: 0,
                    FailedSubscriptionCount: 0,
                    RetryScheduledSubscriptionCount: 0,
                    SkippedSubscriptionCount: 0,
                    LastError: null,
                    Metadata: EmptyMetadata);

            current = normalizedOutcome switch
            {
                EventPublicationRuntimeOutcomes.Accepted => current with
                {
                    LastChannelId = report.ChannelId,
                    LastEventType = report.EventType,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    AcceptedCount = current.AcceptedCount + 1,
                    MatchedSubscriptionCount = report.MatchedSubscriptionCount,
                    StartedSubscriptionCount = report.StartedSubscriptionCount,
                    SucceededSubscriptionCount = report.SucceededSubscriptionCount,
                    FailedSubscriptionCount = report.FailedSubscriptionCount,
                    RetryScheduledSubscriptionCount = report.RetryScheduledSubscriptionCount,
                    SkippedSubscriptionCount = report.SkippedSubscriptionCount,
                    LastError = null,
                    Metadata = metadata
                },
                EventPublicationRuntimeOutcomes.Succeeded => current with
                {
                    LastChannelId = report.ChannelId,
                    LastEventType = report.EventType,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    SucceededCount = current.SucceededCount + 1,
                    MatchedSubscriptionCount = report.MatchedSubscriptionCount,
                    StartedSubscriptionCount = report.StartedSubscriptionCount,
                    SucceededSubscriptionCount = report.SucceededSubscriptionCount,
                    FailedSubscriptionCount = report.FailedSubscriptionCount,
                    RetryScheduledSubscriptionCount = report.RetryScheduledSubscriptionCount,
                    SkippedSubscriptionCount = report.SkippedSubscriptionCount,
                    LastError = null,
                    Metadata = metadata
                },
                EventPublicationRuntimeOutcomes.Failed => current with
                {
                    LastChannelId = report.ChannelId,
                    LastEventType = report.EventType,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    FailedCount = current.FailedCount + 1,
                    MatchedSubscriptionCount = report.MatchedSubscriptionCount,
                    StartedSubscriptionCount = report.StartedSubscriptionCount,
                    SucceededSubscriptionCount = report.SucceededSubscriptionCount,
                    FailedSubscriptionCount = report.FailedSubscriptionCount,
                    RetryScheduledSubscriptionCount = report.RetryScheduledSubscriptionCount,
                    SkippedSubscriptionCount = report.SkippedSubscriptionCount,
                    LastError = report.Error,
                    Metadata = metadata
                },
                EventPublicationRuntimeOutcomes.Skipped => current with
                {
                    LastChannelId = report.ChannelId,
                    LastEventType = report.EventType,
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = observedAtUtc,
                    SkippedCount = current.SkippedCount + 1,
                    MatchedSubscriptionCount = report.MatchedSubscriptionCount,
                    StartedSubscriptionCount = report.StartedSubscriptionCount,
                    SucceededSubscriptionCount = report.SucceededSubscriptionCount,
                    FailedSubscriptionCount = report.FailedSubscriptionCount,
                    RetryScheduledSubscriptionCount = report.RetryScheduledSubscriptionCount,
                    SkippedSubscriptionCount = report.SkippedSubscriptionCount,
                    LastError = null,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"Publication outcome '{report.Outcome}' is not supported by the active eventing runtime.")
            };

            states[report.PublicationId] = current;
        }

        return ValueTask.CompletedTask;
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            EventPublicationRuntimeOutcomes.Accepted => EventPublicationRuntimeOutcomes.Accepted,
            EventPublicationRuntimeOutcomes.Succeeded => EventPublicationRuntimeOutcomes.Succeeded,
            EventPublicationRuntimeOutcomes.Failed => EventPublicationRuntimeOutcomes.Failed,
            EventPublicationRuntimeOutcomes.Skipped => EventPublicationRuntimeOutcomes.Skipped,
            _ => throw new InvalidOperationException(
                $"Publication outcome '{outcome}' is not supported by the active eventing runtime.")
        };
    }
}
