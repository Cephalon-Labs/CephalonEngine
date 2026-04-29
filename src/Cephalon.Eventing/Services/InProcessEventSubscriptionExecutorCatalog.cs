using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventSubscriptionExecutorCatalog : IEventSubscriptionExecutionBindingContributor
{
    private readonly Dictionary<string, ManagedSubscriptionEntry> index;
    private readonly Dictionary<string, IReadOnlyList<ManagedSubscriptionEntry>> channelIndex;

    public InProcessEventSubscriptionExecutorCatalog(
        IEventSubscriptionCatalog subscriptions,
        IEnumerable<IEventSubscriptionExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(executors);

        index = new Dictionary<string, ManagedSubscriptionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var executor in executors)
        {
            if (!subscriptions.TryGet(executor.SubscriptionId, out var subscription))
            {
                throw new InvalidOperationException(
                    $"In-process event-subscription executor '{executor.GetType().FullName ?? executor.GetType().Name}' references unknown declared subscription '{executor.SubscriptionId}'. Register the subscription before enabling in-process subscription execution.");
            }

            if (!index.TryAdd(subscription.Id, new ManagedSubscriptionEntry(subscription, executor)))
            {
                throw new InvalidOperationException(
                    $"In-process event-subscription executor for subscription '{subscription.Id}' was registered more than once. Only one active executor is allowed per declared subscription.");
            }
        }

        channelIndex = index.Values
            .GroupBy(static entry => entry.Subscription.ChannelId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<ManagedSubscriptionEntry>)group
                    .OrderBy(static entry => entry.Subscription.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ManagedSubscriptionEntry> Entries => index.Values
        .OrderBy(static entry => entry.Subscription.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> GetExecutionBindings()
    {
        return Entries
            .Select(entry => new EventSubscriptionExecutionBindingDescriptor(
                subscriptionId: entry.Subscription.Id,
                executionRuntimeId: InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                executionOwnership: "cephalon-managed",
                executionMode: "in-process-direct",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["adapter"] = "none",
                    ["technology"] = "event-driven-integration",
                    ["trigger"] = InProcessEventingRuntimeIds.PublisherId,
                    ["deliveryMode"] = "direct",
                    ["retryPolicy"] = "none",
                    ["channelId"] = entry.Subscription.ChannelId,
                    ["handlerId"] = entry.Subscription.HandlerId,
                    ["subscriptionTagCount"] = entry.Subscription.Tags.Count.ToString(CultureInfo.InvariantCulture)
                }))
            .ToArray();
    }

    public IReadOnlyList<ManagedSubscriptionEntry> GetByChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            return [];
        }

        return channelIndex.TryGetValue(channelId.Trim(), out var entries)
            ? entries
            : [];
    }

    internal sealed record ManagedSubscriptionEntry(
        EventSubscriptionDescriptor Subscription,
        IEventSubscriptionExecutor Executor);
}
