using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Runtime;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingSubscriptionRuntimeSurfaceContributor(
    IEventSubscriptionCatalog catalog,
    IInboxCatalog inboxes,
    IEventSubscriptionRuntimeCatalog runtimeStates,
    IRuntime runtime,
    IExecutionRuntimeCatalog executionGraphs,
    IHostedExecutionRuntimeCatalog hostedExecutions) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var hostedExecutionLinks = BuildHostedExecutionLinks(catalog, hostedExecutions);
        var hostedExecutionStateIndex = runtime.OperationalStory.HostedExecutions.ToDictionary(
            static execution => execution.HostedExecutionId,
            StringComparer.OrdinalIgnoreCase);
        var executionGraphStateIndex = runtime.OperationalStory.ExecutionGraphs.ToDictionary(
            static graph => graph.GraphId,
            StringComparer.OrdinalIgnoreCase);

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-subscriptions",
            displayName: "Event Subscriptions",
            description: "Declared event subscription descriptors available to the active eventing runtime.",
            entries: catalog.Subscriptions
                .Select(subscription => CreateEntry(
                    subscription,
                    hostedExecutionLinks,
                    hostedExecutionStateIndex,
                    executionGraphStateIndex,
                    executionGraphs,
                    runtimeStates))
                .ToArray());
    }

    private TechnologyRuntimeEntry CreateEntry(
        EventSubscriptionDescriptor subscription,
        Dictionary<string, IReadOnlyList<HostedExecutionDescriptor>> hostedExecutionLinks,
        Dictionary<string, RuntimeHostedExecutionState> hostedExecutionStateIndex,
        Dictionary<string, RuntimeExecutionGraphState> executionGraphStateIndex,
        IExecutionRuntimeCatalog executionGraphs,
        IEventSubscriptionRuntimeCatalog runtimeStates)
    {
        var linkedInboxes = inboxes.Inboxes
            .Where(inbox => SupportsChannel(inbox, subscription.ChannelId))
            .OrderBy(static inbox => inbox.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var linkedHostedExecutions = hostedExecutionLinks.TryGetValue(subscription.Id, out var matches)
            ? matches
            : [];
        var hasRuntimeState = runtimeStates.TryGet(subscription.Id, out var runtimeState) && runtimeState is not null;
        var metadata = new Dictionary<string, string>(subscription.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["channelId"] = subscription.ChannelId,
            ["handlerId"] = subscription.HandlerId,
            ["deliveryMode"] = subscription.DeliveryMode,
            ["dispatchRuntime"] = linkedHostedExecutions.Count > 0 || hasRuntimeState ? "application-managed" : "not-configured",
            ["inbox"] = linkedInboxes.Length > 0 ? "available" : "not-configured",
            ["inboxLink"] = linkedInboxes.Length > 0 ? "application-managed" : "not-configured",
            ["runtimeState"] = hasRuntimeState ? "reported" : "not-reported",
            ["subscriptionRuntime"] = linkedHostedExecutions.Count > 0
                ? "hosted-execution-linked"
                : hasRuntimeState
                    ? "application-managed-state"
                    : "not-configured",
            ["tags"] = string.Join(",", subscription.Tags)
        };

        if (linkedInboxes.Length > 0)
        {
            metadata["inboxIds"] = string.Join(",", linkedInboxes.Select(static inbox => inbox.Id));
        }

        if (linkedHostedExecutions.Count > 0)
        {
            metadata["hostedExecutionIds"] = string.Join(",", linkedHostedExecutions.Select(static execution => execution.Id));
        }

        if (linkedHostedExecutions.Count == 1)
        {
            var hostedExecution = linkedHostedExecutions[0];
            metadata["hostedExecutionId"] = hostedExecution.Id;
            metadata["hostedExecutionDisplayName"] = hostedExecution.DisplayName;
            metadata["hostedExecutionKind"] = hostedExecution.Kind;
            metadata["hostedExecutionStartsWithHost"] = hostedExecution.StartsWithHost.ToString().ToLowerInvariant();

            if (hostedExecutionStateIndex.TryGetValue(hostedExecution.Id, out var hostedExecutionState))
            {
                metadata["hostedExecutionPhase"] = hostedExecutionState.LastObservedPhase ?? "unknown";
                metadata["hostedExecutionIsActive"] = hostedExecutionState.IsActive.ToString().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(hostedExecution.ExecutionGraphId))
            {
                var executionGraph = executionGraphs.GetById(hostedExecution.ExecutionGraphId!);
                if (executionGraph is not null)
                {
                    metadata["executionGraphId"] = executionGraph.Id;
                    metadata["executionGraphDisplayName"] = executionGraph.DisplayName;

                    if (executionGraphStateIndex.TryGetValue(executionGraph.Id, out var executionGraphState))
                    {
                        metadata["executionGraphPhase"] = executionGraphState.LastObservedPhase ?? "unknown";
                        metadata["executionGraphIsActive"] = executionGraphState.IsActive.ToString().ToLowerInvariant();
                    }
                }
            }
        }

        if (runtimeState is not null)
        {
            metadata["lastOutcome"] = runtimeState.LastOutcome ?? "unknown";
            metadata["lastObservedAtUtc"] = runtimeState.LastObservedAtUtc?.ToString("O") ?? string.Empty;
            metadata["lastAttempt"] = runtimeState.LastAttempt.ToString(CultureInfo.InvariantCulture);
            metadata["startedCount"] = runtimeState.StartedCount.ToString(CultureInfo.InvariantCulture);
            metadata["succeededCount"] = runtimeState.SucceededCount.ToString(CultureInfo.InvariantCulture);
            metadata["failedCount"] = runtimeState.FailedCount.ToString(CultureInfo.InvariantCulture);
            metadata["retryScheduledCount"] = runtimeState.RetryScheduledCount.ToString(CultureInfo.InvariantCulture);
            metadata["skippedCount"] = runtimeState.SkippedCount.ToString(CultureInfo.InvariantCulture);
            metadata["totalReports"] = runtimeState.TotalReports.ToString(CultureInfo.InvariantCulture);
            metadata["retryPending"] = runtimeState.RetryPending.ToString().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(runtimeState.LastMessageId))
            {
                metadata["lastMessageId"] = runtimeState.LastMessageId;
            }

            if (!string.IsNullOrWhiteSpace(runtimeState.LastError))
            {
                metadata["lastError"] = runtimeState.LastError;
            }

            if (runtimeState.Metadata.Count > 0)
            {
                metadata["reportedMetadataKeys"] = string.Join(
                    ",",
                    runtimeState.Metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));

                foreach (var pair in runtimeState.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        metadata[$"reported.{pair.Key.Trim()}"] = pair.Value;
                    }
                }
            }
        }

        return new TechnologyRuntimeEntry(
            id: subscription.Id,
            displayName: subscription.DisplayName,
            description: subscription.Description,
            metadata: metadata);
    }

    private static Dictionary<string, IReadOnlyList<HostedExecutionDescriptor>> BuildHostedExecutionLinks(
        IEventSubscriptionCatalog catalog,
        IHostedExecutionRuntimeCatalog hostedExecutions)
    {
        var links = new Dictionary<string, List<HostedExecutionDescriptor>>(StringComparer.OrdinalIgnoreCase);
        foreach (var hostedExecution in hostedExecutions.HostedExecutions)
        {
            foreach (var subscriptionId in GetLinkedSubscriptionIds(hostedExecution))
            {
                if (!catalog.TryGet(subscriptionId, out _))
                {
                    throw new InvalidOperationException(
                        $"Hosted execution '{hostedExecution.Id}' references unknown event subscription '{subscriptionId}'. Register the subscription before linking hosted execution metadata.");
                }

                if (!links.TryGetValue(subscriptionId, out var executions))
                {
                    executions = [];
                    links[subscriptionId] = executions;
                }

                executions.Add(hostedExecution);
            }
        }

        return links.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<HostedExecutionDescriptor>)pair.Value
                .OrderBy(static execution => execution.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string[] GetLinkedSubscriptionIds(HostedExecutionDescriptor hostedExecution)
    {
        var ids = new List<string>();
        if (hostedExecution.Metadata.TryGetValue("eventSubscriptionId", out var singleId) &&
            !string.IsNullOrWhiteSpace(singleId))
        {
            ids.Add(singleId.Trim());
        }

        if (hostedExecution.Metadata.TryGetValue("eventSubscriptionIds", out var manyIds) &&
            !string.IsNullOrWhiteSpace(manyIds))
        {
            ids.AddRange(manyIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool SupportsChannel(
        InboxDescriptor inbox,
        string channelId)
    {
        return inbox.ChannelIds.Count == 0 ||
            inbox.ChannelIds.Any(candidate => string.Equals(candidate, channelId, StringComparison.OrdinalIgnoreCase));
    }
}
