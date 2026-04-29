using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventSubscriptionExecutionReadinessCatalog(
    IEventSubscriptionCatalog subscriptions,
    IEventSubscriptionExecutionBindingCatalog executionBindings,
    IEventSubscriptionRuntimeCatalog runtimeStates,
    IHostedExecutionRuntimeCatalog hostedExecutions) : IEventSubscriptionExecutionReadinessCatalog
{
    public IReadOnlyList<EventSubscriptionExecutionReadinessDescriptor> Readiness
    {
        get
        {
            var hostedExecutionLinks = BuildHostedExecutionLinks();
            return subscriptions.Subscriptions
                .Select(subscription => CreateDescriptor(subscription, hostedExecutionLinks))
                .OrderBy(static descriptor => descriptor.SubscriptionId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public EventSubscriptionExecutionReadinessDescriptor? GetBySubscriptionId(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return null;
        }

        var hostedExecutionLinks = BuildHostedExecutionLinks();
        return subscriptions.TryGet(subscriptionId.Trim(), out var subscription)
            ? CreateDescriptor(subscription, hostedExecutionLinks)
            : null;
    }

    public bool TryGet(string subscriptionId, out EventSubscriptionExecutionReadinessDescriptor? readiness)
    {
        readiness = GetBySubscriptionId(subscriptionId);
        return readiness is not null;
    }

    private EventSubscriptionExecutionReadinessDescriptor CreateDescriptor(
        EventSubscriptionDescriptor subscription,
        Dictionary<string, IReadOnlyList<HostedExecutionDescriptor>> hostedExecutionLinks)
    {
        var linkedHostedExecutions = hostedExecutionLinks.TryGetValue(subscription.Id, out var matches)
            ? matches
            : [];
        var hasRuntimeState = runtimeStates.TryGet(subscription.Id, out var runtimeState) && runtimeState is not null;
        if (executionBindings.TryGet(subscription.Id, out var binding) && binding is not null)
        {
            var metadata = new Dictionary<string, string>(binding.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["runtimeState"] = hasRuntimeState ? "reported" : "not-reported"
            };

            if (linkedHostedExecutions.Count > 0)
            {
                metadata["hostedExecutionIds"] = string.Join(",", linkedHostedExecutions.Select(static execution => execution.Id));
            }

            return new EventSubscriptionExecutionReadinessDescriptor(
                subscriptionId: subscription.Id,
                readinessState: EventSubscriptionExecutionReadinessStates.RuntimeBound,
                executionOwnership: binding.ExecutionOwnership,
                executionMode: binding.ExecutionMode,
                executionRuntimeId: binding.ExecutionRuntimeId,
                reasons: hasRuntimeState
                    ? ["managed-binding-available", "runtime-state-reported"]
                    : ["managed-binding-available"],
                metadata: metadata);
        }

        if (linkedHostedExecutions.Count > 0)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["hostedExecutionCount"] = linkedHostedExecutions.Count.ToString(CultureInfo.InvariantCulture),
                ["hostedExecutionIds"] = string.Join(",", linkedHostedExecutions.Select(static execution => execution.Id)),
                ["runtimeState"] = hasRuntimeState ? "reported" : "not-reported"
            };

            return new EventSubscriptionExecutionReadinessDescriptor(
                subscriptionId: subscription.Id,
                readinessState: EventSubscriptionExecutionReadinessStates.HostedExecutionLinked,
                executionOwnership: "application-managed",
                executionMode: "hosted-execution",
                reasons: hasRuntimeState
                    ? ["hosted-execution-linked", "runtime-state-reported"]
                    : ["hosted-execution-linked"],
                metadata: metadata);
        }

        if (hasRuntimeState)
        {
            return new EventSubscriptionExecutionReadinessDescriptor(
                subscriptionId: subscription.Id,
                readinessState: EventSubscriptionExecutionReadinessStates.ApplicationManagedState,
                executionOwnership: "application-managed",
                executionMode: "runtime-reported",
                reasons: ["runtime-state-reported"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["runtimeState"] = "reported"
                });
        }

        return new EventSubscriptionExecutionReadinessDescriptor(
            subscriptionId: subscription.Id,
            readinessState: EventSubscriptionExecutionReadinessStates.DeclaredOnly,
            executionOwnership: "not-configured",
            executionMode: "not-configured",
            reasons: ["no-execution-path-observed"],
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["runtimeState"] = "not-reported"
            });
    }

    private Dictionary<string, IReadOnlyList<HostedExecutionDescriptor>> BuildHostedExecutionLinks()
    {
        var links = new Dictionary<string, List<HostedExecutionDescriptor>>(StringComparer.OrdinalIgnoreCase);
        foreach (var hostedExecution in hostedExecutions.HostedExecutions)
        {
            foreach (var subscriptionId in GetLinkedSubscriptionIds(hostedExecution))
            {
                if (!subscriptions.TryGet(subscriptionId, out _))
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
}
