using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDispatchRemediationRuntimeSurfaceContributor(
    IEventDispatchRuntimeCatalog runtimeCatalog) : ITechnologyRuntimeContributor
{
    private const string ClaimPolicy = "reported-state-advisory-only";

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-dispatch-remediations",
            displayName: "Event Dispatch Remediations",
            description: "Provider-neutral operator remediation advice derived from reported event-dispatch retry, skipped, failed, and terminal-failure state.",
            entries: runtimeCatalog.States
                .Where(IsActionable)
                .Select(CreateEntry)
                .ToArray());
    }

    private static bool IsActionable(EventDispatchRuntimeState state) =>
        state.RetryPending ||
        state.TerminalFailure ||
        IsOutcome(state, EventDispatchExecutionOutcomes.Failed) ||
        IsOutcome(state, EventDispatchExecutionOutcomes.Skipped);

    private static TechnologyRuntimeEntry CreateEntry(EventDispatchRuntimeState state)
    {
        var remediationState = ResolveRemediationState(state);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["outboxId"] = state.OutboxId,
            ["lastOutcome"] = state.LastOutcome ?? "not-reported",
            ["lastAttempt"] = state.LastAttempt.ToString(CultureInfo.InvariantCulture),
            ["failedCount"] = state.FailedCount.ToString(CultureInfo.InvariantCulture),
            ["retryScheduledCount"] = state.RetryScheduledCount.ToString(CultureInfo.InvariantCulture),
            ["skippedCount"] = state.SkippedCount.ToString(CultureInfo.InvariantCulture),
            ["totalReports"] = state.TotalReports.ToString(CultureInfo.InvariantCulture),
            ["retryPending"] = state.RetryPending ? "true" : "false",
            ["terminalFailure"] = state.TerminalFailure ? "true" : "false",
            ["terminalFailureCount"] = state.TerminalFailureCount.ToString(CultureInfo.InvariantCulture),
            ["remediationState"] = remediationState,
            ["recommendedAction"] = ResolveRecommendedAction(remediationState),
            ["operatorCommandState"] = "advisory-only",
            ["replayCommand"] = "not-claimed",
            ["deadLetterCommand"] = "not-claimed",
            ["quarantineCommand"] = "not-claimed",
            ["skipCommand"] = "not-claimed",
            ["claimPolicy"] = ClaimPolicy,
            ["providerNeutral"] = "true",
            ["wolverineRequired"] = "false"
        };

        if (!string.IsNullOrWhiteSpace(state.LastChannelId))
        {
            metadata["lastChannelId"] = state.LastChannelId;
        }

        if (!string.IsNullOrWhiteSpace(state.LastMessageId))
        {
            metadata["lastMessageId"] = state.LastMessageId;
        }

        if (state.LastObservedAtUtc is { } lastObservedAtUtc)
        {
            metadata["lastObservedAtUtc"] = lastObservedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(state.LastError))
        {
            metadata["lastError"] = state.LastError;
        }

        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.NextRetryAtUtc);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryPolicy);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryMaxAttempts);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryDelaySeconds);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryDurability);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryScope);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryOutcome);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.RetryExhausted);
        CopyReportedValue(state, metadata, EventDispatchRuntimeMetadataKeys.TerminalFailure);

        if (state.Metadata.Count > 0)
        {
            metadata["reportedMetadataKeys"] = string.Join(
                ",",
                state.Metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
            foreach (var pair in state.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                metadata[$"reported.{pair.Key}"] = pair.Value;
            }
        }

        return new TechnologyRuntimeEntry(
            id: BuildEntryId(state),
            displayName: BuildDisplayName(state),
            description: ResolveDescription(remediationState),
            metadata: metadata);
    }

    private static string BuildEntryId(EventDispatchRuntimeState state) =>
        string.IsNullOrWhiteSpace(state.LastMessageId)
            ? state.OutboxId
            : $"{state.OutboxId}:{state.LastMessageId}";

    private static string BuildDisplayName(EventDispatchRuntimeState state) =>
        string.IsNullOrWhiteSpace(state.LastMessageId)
            ? state.OutboxId
            : $"{state.OutboxId} / {state.LastMessageId}";

    private static void CopyReportedValue(
        EventDispatchRuntimeState state,
        Dictionary<string, string> metadata,
        string key)
    {
        if (state.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
    }

    private static string ResolveRemediationState(EventDispatchRuntimeState state)
    {
        if (state.TerminalFailure)
        {
            return "terminal-failure";
        }

        if (state.RetryPending)
        {
            return "retry-pending";
        }

        if (IsOutcome(state, EventDispatchExecutionOutcomes.Skipped))
        {
            return "skipped";
        }

        return "failed";
    }

    private static string ResolveRecommendedAction(string remediationState) =>
        remediationState switch
        {
            "terminal-failure" => "inspect-terminal-failure-before-replay",
            "retry-pending" => "wait-for-scheduled-retry-or-inspect-downstream",
            "skipped" => "inspect-skipped-dispatch-before-requeue",
            _ => "inspect-failed-dispatch-before-retry"
        };

    private static string ResolveDescription(string remediationState) =>
        remediationState switch
        {
            "terminal-failure" => "The dispatch path is terminally failed; inspect the recorded error and retry-exhaustion metadata before any future replay or quarantine command.",
            "retry-pending" => "The dispatch path has a scheduled retry; inspect the downstream dependency and retry eligibility before forcing manual action.",
            "skipped" => "The dispatch path was skipped; inspect why it did not continue before requeueing or discarding the message in a provider-specific tool.",
            _ => "The dispatch path reported a failure; inspect the latest error and policy metadata before retrying manually."
        };

    private static bool IsOutcome(EventDispatchRuntimeState state, string outcome) =>
        string.Equals(state.LastOutcome, outcome, StringComparison.OrdinalIgnoreCase);
}
