using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Data;
using Cephalon.Engine.Runtime;
using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineEventingRuntimeSurfaceContributor(
    WolverineEventingOptions options,
    IServiceProvider serviceProvider,
    IRuntime runtime,
    IEventDispatchRuntimeDescriptorCatalog dispatchRuntimeDescriptorCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        using var scope = serviceProvider.CreateScope();
        var dispatchStores = scope.ServiceProvider.GetServices<IEventDispatchStore>();
        var managedSubscriptions = scope.ServiceProvider.GetService<WolverineManagedEventSubscriptionExecutorCatalog>();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
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
        var dispatchRuntime = dispatchRuntimeDescriptorCatalog.GetById(WolverineEventingRuntimeIds.DispatchRuntimeId);
        var summary = dispatchRuntime?.Summary ?? EventDispatchRuntimeSummary.Empty;
        var managedSubscriptionEntries = managedSubscriptions?.Entries ?? [];
        var managedSubscriptionIds = managedSubscriptionEntries
            .Select(static entry => entry.Subscription.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var managedStates = subscriptionRuntimeCatalog is null
            ? []
            : managedSubscriptionEntries
                .Select(entry => subscriptionRuntimeCatalog.GetById(entry.Subscription.Id))
                .Where(static state => state is not null)
                .Cast<EventSubscriptionRuntimeState>()
                .ToArray();
        var managedReportedCount = managedStates.Sum(static state => state.TotalReports);
        var managedRetryPendingCount = managedStates.Count(static state => state.RetryPending);
        var latestManagedState = managedStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.SubscriptionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        var aggregateMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reportedOutboxCount"] = summary.ReportedOutboxCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedStartedCount"] = summary.StartedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedSucceededCount"] = summary.SucceededCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedFailedCount"] = summary.FailedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedRetryScheduledCount"] = summary.RetryScheduledCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedSkippedCount"] = summary.SkippedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedTotalCount"] = summary.TotalReports.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedRetryPendingCount"] = summary.RetryPendingCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (summary.HasReports)
        {
            if (!string.IsNullOrWhiteSpace(summary.LastOutboxId))
            {
                aggregateMetadata["lastOutboxId"] = summary.LastOutboxId;
            }

            aggregateMetadata["lastOutcome"] = summary.LastOutcome ?? "not-reported";
            aggregateMetadata["lastAttempt"] = summary.LastAttempt.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (summary.LastObservedAtUtc is { } lastObservedAtUtc)
            {
                aggregateMetadata["lastObservedAtUtc"] = lastObservedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(summary.LastMessageId))
            {
                aggregateMetadata["lastMessageId"] = summary.LastMessageId;
            }

            if (!string.IsNullOrWhiteSpace(summary.LastChannelId))
            {
                aggregateMetadata["lastChannelId"] = summary.LastChannelId;
            }

            if (!string.IsNullOrWhiteSpace(summary.LastError))
            {
                aggregateMetadata["lastError"] = summary.LastError;
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
            description: "Optional Wolverine host-wiring surface for Cephalon event-driven workloads.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "wolverine-eventing",
                    displayName: "Wolverine Eventing",
                    description: "Wires the optional Wolverine runtime into the current host while Cephalon keeps dispatch-bridge truth explicit.",
                    metadata: new Dictionary<string, string>
                    {
                        ["adapter"] = "wolverine",
                        ["hostWiring"] = options.EnableHostWiring ? "configured" : "disabled",
                        ["dispatchBridge"] = options.EnableDispatchLoop && dispatchStoreIds.Length > 0 ? "wolverine-managed" : "consumer-managed",
                        ["dispatchRuntime"] = options.EnableDispatchLoop && dispatchStoreIds.Length > 0 ? "configured" : "not-configured",
                        ["dispatchLoop"] = options.EnableDispatchLoop ? "enabled" : "disabled",
                        ["subscriptionExecution"] = options.EnableSubscriptionExecution && managedSubscriptionIds.Length > 0 ? "wolverine-managed" : "not-configured",
                        ["dispatchStore"] = dispatchStoreIds.Length > 0 ? "available" : "not-configured",
                        ["dispatchStoreCount"] = dispatchStoreIds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["dispatchStoreTypes"] = string.Join(",", dispatchStoreIds),
                        ["runtimeSurface"] = options.EnableRuntimeSurface ? "enabled" : "disabled",
                        ["dispatchBatchSize"] = Math.Max(1, options.DispatchBatchSize).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["dispatchPollingIntervalSeconds"] = Math.Max(1, options.DispatchPollingIntervalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["retryDelaySeconds"] = Math.Max(1, options.RetryDelaySeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["subscriptionRetryDelaySeconds"] = Math.Max(1, options.SubscriptionRetryDelaySeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["dispatchMode"] = "publish-event-publication",
                        ["deliveryMode"] = "publish",
                        ["messageType"] = typeof(EventPublication).FullName ?? typeof(EventPublication).Name,
                        ["dispatchRuntimeId"] = options.EnableDispatchLoop ? WolverineEventingRuntimeIds.DispatchRuntimeId : "not-configured",
                        ["hostedExecutionId"] = options.EnableDispatchLoop ? WolverineEventingRuntimeIds.HostedExecutionId : "not-configured",
                        ["executionGraphId"] = options.EnableDispatchLoop ? WolverineEventingRuntimeIds.ExecutionGraphId : "not-configured",
                        ["subscriptionExecutionRuntimeId"] = options.EnableSubscriptionExecution && managedSubscriptionIds.Length > 0 ? WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId : "not-configured",
                        ["managedSubscriptionCount"] = managedSubscriptionIds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["managedSubscriptionIds"] = string.Join(",", managedSubscriptionIds),
                        ["managedSubscriptionReportedCount"] = managedReportedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["managedSubscriptionRetryPendingCount"] = managedRetryPendingCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["hostedExecutionPhase"] = hostedExecution?.LastObservedPhase ?? "not-reported",
                        ["hostedExecutionIsActive"] = hostedExecution?.IsActive == true ? "true" : "false",
                        ["executionGraphPhase"] = executionGraph?.LastObservedPhase ?? "not-reported",
                        ["executionGraphIsActive"] = executionGraph?.IsActive == true ? "true" : "false"
                    }
                    .Concat(latestManagedState is null
                        ? []
                        :
                        [
                            new KeyValuePair<string, string>("managedSubscriptionLastSubscriptionId", latestManagedState.SubscriptionId),
                            new KeyValuePair<string, string>("managedSubscriptionLastOutcome", latestManagedState.LastOutcome ?? "not-reported"),
                            new KeyValuePair<string, string>("managedSubscriptionLastAttempt", latestManagedState.LastAttempt.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        ])
                    .Concat(aggregateMetadata)
                        .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase))
            ]);
    }
}
