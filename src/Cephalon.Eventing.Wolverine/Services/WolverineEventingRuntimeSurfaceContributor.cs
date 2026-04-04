using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Runtime;
using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineEventingRuntimeSurfaceContributor(
    WolverineEventingOptions options,
    IEnumerable<IEventDispatchStore> dispatchStores,
    IRuntime runtime,
    IEventDispatchRuntimeCatalog dispatchRuntimeCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var dispatchStoreIds = dispatchStores
            .Select(static store => store.GetType().FullName ?? store.GetType().Name)
            .OrderBy(static typeName => typeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hostedExecution = runtime.OperationalStory.HostedExecutions
            .SingleOrDefault(static execution =>
                string.Equals(execution.HostedExecutionId, WolverineEventingRuntimeIds.HostedExecutionId, StringComparison.OrdinalIgnoreCase));
        var executionGraph = runtime.OperationalStory.ExecutionGraphs
            .SingleOrDefault(static graph =>
                string.Equals(graph.GraphId, WolverineEventingRuntimeIds.ExecutionGraphId, StringComparison.OrdinalIgnoreCase));
        var dispatchStates = dispatchRuntimeCatalog.States.ToArray();
        var latestDispatchState = dispatchStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        var aggregateMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reportedOutboxCount"] = dispatchStates.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedStartedCount"] = dispatchStates.Sum(static state => state.StartedCount).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedSucceededCount"] = dispatchStates.Sum(static state => state.SucceededCount).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedFailedCount"] = dispatchStates.Sum(static state => state.FailedCount).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedRetryScheduledCount"] = dispatchStates.Sum(static state => state.RetryScheduledCount).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedSkippedCount"] = dispatchStates.Sum(static state => state.SkippedCount).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedTotalCount"] = dispatchStates.Sum(static state => state.TotalReports).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedRetryPendingCount"] = dispatchStates.Count(static state => state.RetryPending).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (latestDispatchState is not null)
        {
            aggregateMetadata["lastOutboxId"] = latestDispatchState.OutboxId;
            aggregateMetadata["lastOutcome"] = latestDispatchState.LastOutcome ?? "not-reported";
            aggregateMetadata["lastAttempt"] = latestDispatchState.LastAttempt.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (latestDispatchState.LastObservedAtUtc is { } lastObservedAtUtc)
            {
                aggregateMetadata["lastObservedAtUtc"] = lastObservedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(latestDispatchState.LastMessageId))
            {
                aggregateMetadata["lastMessageId"] = latestDispatchState.LastMessageId;
            }

            if (!string.IsNullOrWhiteSpace(latestDispatchState.LastChannelId))
            {
                aggregateMetadata["lastChannelId"] = latestDispatchState.LastChannelId;
            }

            if (!string.IsNullOrWhiteSpace(latestDispatchState.LastError))
            {
                aggregateMetadata["lastError"] = latestDispatchState.LastError;
            }
        }
        else
        {
            aggregateMetadata["lastOutcome"] = "not-reported";
            aggregateMetadata["lastAttempt"] = "0";
        }

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "wolverine-adapter",
            displayName: "Wolverine Adapter",
            description: "Official Wolverine host-wiring surface for Cephalon event-driven workloads.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "wolverine-eventing",
                    displayName: "Wolverine Eventing",
                    description: "Wires the official Wolverine runtime into the current host while Cephalon keeps dispatch-bridge truth explicit.",
                    metadata: new Dictionary<string, string>
                    {
                        ["adapter"] = "wolverine",
                        ["hostWiring"] = options.EnableHostWiring ? "configured" : "disabled",
                        ["dispatchBridge"] = options.EnableDispatchLoop && dispatchStoreIds.Length > 0 ? "wolverine-managed" : "consumer-managed",
                        ["dispatchRuntime"] = options.EnableDispatchLoop && dispatchStoreIds.Length > 0 ? "configured" : "not-configured",
                        ["dispatchLoop"] = options.EnableDispatchLoop ? "enabled" : "disabled",
                        ["dispatchStore"] = dispatchStoreIds.Length > 0 ? "available" : "not-configured",
                        ["dispatchStoreCount"] = dispatchStoreIds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["dispatchStoreTypes"] = string.Join(",", dispatchStoreIds),
                        ["runtimeSurface"] = options.EnableRuntimeSurface ? "enabled" : "disabled",
                        ["dispatchBatchSize"] = Math.Max(1, options.DispatchBatchSize).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["dispatchPollingIntervalSeconds"] = Math.Max(1, options.DispatchPollingIntervalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["retryDelaySeconds"] = Math.Max(1, options.RetryDelaySeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["dispatchMode"] = "publish-event-publication",
                        ["deliveryMode"] = "publish",
                        ["messageType"] = typeof(EventPublication).FullName ?? typeof(EventPublication).Name,
                        ["dispatchRuntimeId"] = options.EnableDispatchLoop ? WolverineEventingRuntimeIds.DispatchRuntimeId : "not-configured",
                        ["hostedExecutionId"] = options.EnableDispatchLoop ? WolverineEventingRuntimeIds.HostedExecutionId : "not-configured",
                        ["executionGraphId"] = options.EnableDispatchLoop ? WolverineEventingRuntimeIds.ExecutionGraphId : "not-configured",
                        ["hostedExecutionPhase"] = hostedExecution?.LastObservedPhase ?? "not-reported",
                        ["hostedExecutionIsActive"] = hostedExecution?.IsActive == true ? "true" : "false",
                        ["executionGraphPhase"] = executionGraph?.LastObservedPhase ?? "not-reported",
                        ["executionGraphIsActive"] = executionGraph?.IsActive == true ? "true" : "false"
                    }.Concat(aggregateMetadata)
                        .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase))
            ]);
    }
}
