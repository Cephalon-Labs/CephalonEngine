using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRuntimeDescriptorCatalog : IEventDispatchRuntimeDescriptorCatalog
{
    private readonly Dictionary<string, EventDispatchRuntimeDescriptor> index;
    private readonly IEventDispatchRuntimeCatalog? runtimeCatalog;

    public EventDispatchRuntimeDescriptorCatalog(
        IEnumerable<IEventDispatchRuntimeContributor> contributors,
        IEventDispatchRuntimeCatalog? runtimeCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventDispatchRuntimeRegistry();
        foreach (var contributor in contributors)
        {
            contributor.RegisterDispatchRuntimes(registry);
        }

        this.runtimeCatalog = runtimeCatalog;
        var runtimes = registry.Build();
        index = runtimes.ToDictionary(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventDispatchRuntimeDescriptor> Runtimes => index.Values
        .Select(Enrich)
        .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public EventDispatchRuntimeDescriptor? GetById(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
        {
            return null;
        }

        return index.TryGetValue(runtimeId.Trim(), out var runtime)
            ? Enrich(runtime)
            : null;
    }

    private EventDispatchRuntimeDescriptor Enrich(EventDispatchRuntimeDescriptor runtime)
    {
        if (runtimeCatalog is null)
        {
            return runtime;
        }

        var summary = CreateSummary(runtime);
        return new EventDispatchRuntimeDescriptor(
            id: runtime.Id,
            displayName: runtime.DisplayName,
            description: runtime.Description,
            metadata: runtime.Metadata,
            outboxIds: runtime.OutboxIds,
            summary: summary);
    }

    private EventDispatchRuntimeSummary CreateSummary(EventDispatchRuntimeDescriptor runtime)
    {
        var matchingStates = runtimeCatalog!.States
            .Where(state => runtime.OutboxIds.Contains(state.OutboxId, StringComparer.OrdinalIgnoreCase) ||
                RuntimeStateBelongsToRuntime(state, runtime.Id))
            .OrderBy(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (matchingStates.Length == 0)
        {
            return EventDispatchRuntimeSummary.Empty;
        }

        var latestState = matchingStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase)
            .First();

        return new EventDispatchRuntimeSummary(
            reportedOutboxIds: matchingStates.Select(static state => state.OutboxId).ToArray(),
            lastOutboxId: latestState.OutboxId,
            lastChannelId: latestState.LastChannelId,
            lastOutcome: latestState.LastOutcome,
            lastObservedAtUtc: latestState.LastObservedAtUtc,
            lastMessageId: latestState.LastMessageId,
            lastAttempt: latestState.LastAttempt,
            startedCount: matchingStates.Sum(static state => state.StartedCount),
            succeededCount: matchingStates.Sum(static state => state.SucceededCount),
            failedCount: matchingStates.Sum(static state => state.FailedCount),
            retryScheduledCount: matchingStates.Sum(static state => state.RetryScheduledCount),
            skippedCount: matchingStates.Sum(static state => state.SkippedCount),
            retryPendingCount: matchingStates.Count(static state => state.RetryPending),
            lastError: latestState.LastError);
    }

    private static bool RuntimeStateBelongsToRuntime(EventDispatchRuntimeState state, string runtimeId)
    {
        if (state.Metadata.TryGetValue("dispatchRuntimeId", out var reportedRuntimeId) &&
            string.Equals(reportedRuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return state.Metadata.TryGetValue("eventDispatchRuntimeId", out reportedRuntimeId) &&
            string.Equals(reportedRuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase);
    }
}
