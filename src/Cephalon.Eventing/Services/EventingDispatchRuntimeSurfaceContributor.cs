using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDispatchRuntimeSurfaceContributor(
    IOutboxCatalog outboxes,
    IEventDispatchRuntimeCatalog runtimeCatalog,
    IEventDispatchRuntimeDescriptorCatalog dispatchRuntimes) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-dispatches",
            displayName: "Event Dispatches",
            description: "Reported runtime state for durable event-dispatch paths backed by the active outbox surfaces.",
            entries: outboxes.Outboxes
                .Select(CreateEntry)
                .ToArray());
    }

    private TechnologyRuntimeEntry CreateEntry(OutboxDescriptor outbox)
    {
        var matchingRuntimes = dispatchRuntimes.Runtimes
            .Where(runtime => runtime.OutboxIds.Contains(outbox.Id, StringComparer.OrdinalIgnoreCase))
            .OrderBy(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var dispatchStore = ResolvePolicyMetadata(
            outbox.DispatchPolicy,
            "dispatchStore",
            string.Equals(outbox.DispatchPolicy.ExecutionMode, "disabled", StringComparison.OrdinalIgnoreCase)
                ? "not-configured"
                : "available");
        var dispatchRuntime = ResolvePolicyMetadata(
            outbox.DispatchPolicy,
            "dispatchRuntime",
            string.IsNullOrWhiteSpace(outbox.DispatchPolicy.RuntimeId)
                ? "not-configured"
                : "configured");
        var dispatchRuntimeId = ResolvePolicyMetadata(
            outbox.DispatchPolicy,
            "dispatchRuntimeId",
            string.IsNullOrWhiteSpace(outbox.DispatchPolicy.RuntimeId)
                ? "not-configured"
                : outbox.DispatchPolicy.RuntimeId);
        var metadata = new Dictionary<string, string>(outbox.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = outbox.SourceModuleId,
            ["dispatchStore"] = dispatchStore,
            ["dispatchRuntime"] = dispatchRuntime,
            ["provider"] = outbox.Provider,
            ["mode"] = outbox.Mode,
            ["dispatchPolicyId"] = outbox.DispatchPolicy.PolicyId,
            ["dispatchExecutionMode"] = outbox.DispatchPolicy.ExecutionMode
        };

        if (outbox.ChannelIds.Count > 0)
        {
            metadata["channelIds"] = string.Join(",", outbox.ChannelIds);
        }

        if (outbox.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", outbox.Tags);
        }

        if (!string.IsNullOrWhiteSpace(dispatchRuntimeId))
        {
            metadata["dispatchRuntimeId"] = dispatchRuntimeId;
        }

        if (matchingRuntimes.Length > 0)
        {
            metadata["dispatchRuntimeCount"] = matchingRuntimes.Length.ToString(CultureInfo.InvariantCulture);
            metadata["dispatchRuntimeIds"] = string.Join(",", matchingRuntimes.Select(static runtime => runtime.Id));

            foreach (var runtime in matchingRuntimes)
            {
                metadata[$"dispatchRuntime.{runtime.Id}.displayName"] = runtime.DisplayName;
                metadata[$"dispatchRuntime.{runtime.Id}.description"] = runtime.Description;
                metadata[$"dispatchRuntime.{runtime.Id}.outboxCount"] = runtime.OutboxIds.Count.ToString(CultureInfo.InvariantCulture);
                metadata[$"dispatchRuntime.{runtime.Id}.outboxIds"] = string.Join(",", runtime.OutboxIds);

                foreach (var pair in runtime.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    metadata[$"dispatchRuntime.{runtime.Id}.{pair.Key}"] = pair.Value;
                }
            }
        }

        var state = runtimeCatalog.GetByOutboxId(outbox.Id);
        if (state is null)
        {
            metadata["runtimeState"] = "not-reported";
        }
        else
        {
            metadata["runtimeState"] = "reported";
            if (!string.IsNullOrWhiteSpace(state.LastChannelId))
            {
                metadata["lastChannelId"] = state.LastChannelId;
            }

            if (!string.IsNullOrWhiteSpace(state.LastOutcome))
            {
                metadata["lastOutcome"] = state.LastOutcome;
            }

            if (state.LastObservedAtUtc is { } lastObservedAtUtc)
            {
                metadata["lastObservedAtUtc"] = lastObservedAtUtc.ToString("O", CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(state.LastMessageId))
            {
                metadata["lastMessageId"] = state.LastMessageId;
            }

            metadata["lastAttempt"] = state.LastAttempt.ToString(CultureInfo.InvariantCulture);
            metadata["startedCount"] = state.StartedCount.ToString(CultureInfo.InvariantCulture);
            metadata["succeededCount"] = state.SucceededCount.ToString(CultureInfo.InvariantCulture);
            metadata["failedCount"] = state.FailedCount.ToString(CultureInfo.InvariantCulture);
            metadata["retryScheduledCount"] = state.RetryScheduledCount.ToString(CultureInfo.InvariantCulture);
            metadata["skippedCount"] = state.SkippedCount.ToString(CultureInfo.InvariantCulture);
            metadata["totalReports"] = state.TotalReports.ToString(CultureInfo.InvariantCulture);
            metadata["retryPending"] = state.RetryPending ? "true" : "false";
            metadata["terminalFailure"] = state.TerminalFailure ? "true" : "false";
            metadata["terminalFailureCount"] = state.TerminalFailureCount.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(state.LastError))
            {
                metadata["lastError"] = state.LastError;
            }

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
        }

        return new TechnologyRuntimeEntry(
            id: outbox.Id,
            displayName: outbox.DisplayName,
            description: outbox.Description,
            metadata: metadata);
    }

    private static string ResolvePolicyMetadata(
        OutboxDispatchPolicyDescriptor policy,
        string key,
        string fallbackValue)
    {
        return policy.Metadata.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallbackValue;
    }
}
