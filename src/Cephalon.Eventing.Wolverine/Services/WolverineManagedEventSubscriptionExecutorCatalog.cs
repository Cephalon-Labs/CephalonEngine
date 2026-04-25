using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineManagedEventSubscriptionExecutorCatalog
    : IEventSubscriptionExecutionBindingContributor
{
    private readonly Dictionary<string, ManagedSubscriptionEntry> index;
    private readonly Dictionary<string, IReadOnlyList<ManagedSubscriptionEntry>> channelIndex;
    private readonly WolverineEventingOptions options;

    public WolverineManagedEventSubscriptionExecutorCatalog(
        WolverineEventingOptions options,
        IEventSubscriptionCatalog subscriptions,
        IEnumerable<IEventSubscriptionExecutor> executors)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(executors);

        index = new Dictionary<string, ManagedSubscriptionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var executor in executors)
        {
            if (!subscriptions.TryGet(executor.SubscriptionId, out var subscription))
            {
                throw new InvalidOperationException(
                    $"Managed event-subscription executor '{executor.GetType().FullName ?? executor.GetType().Name}' references unknown declared subscription '{executor.SubscriptionId}'. Register the subscription before enabling Wolverine-managed subscription execution.");
            }

            if (!index.TryAdd(subscription.Id, new ManagedSubscriptionEntry(subscription, executor)))
            {
                throw new InvalidOperationException(
                    $"Managed event-subscription executor for subscription '{subscription.Id}' was registered more than once. Only one active executor is allowed per declared subscription.");
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
                executionRuntimeId: WolverineEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                executionOwnership: "wolverine-managed",
                executionMode: "message-handler",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["adapter"] = "wolverine",
                    ["technology"] = "event-driven-integration",
                    ["trigger"] = WolverineEventingRuntimeIds.DispatchRuntimeId,
                    ["retryPolicy"] = "fixed-delay",
                    ["retryDelaySeconds"] = Math.Max(1, options.SubscriptionRetryDelaySeconds).ToString(CultureInfo.InvariantCulture)
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

    public bool TryGet(string subscriptionId, out ManagedSubscriptionEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        return index.TryGetValue(subscriptionId.Trim(), out entry!);
    }

    internal sealed record ManagedSubscriptionEntry(
        EventSubscriptionDescriptor Subscription,
        IEventSubscriptionExecutor Executor);
}
