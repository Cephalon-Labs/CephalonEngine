using System.Globalization;
using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventSubscriptionExecutorCatalog : IEventSubscriptionExecutionBindingContributor
{
    private readonly Dictionary<string, ManagedSubscriptionEntry> index;
    private readonly Dictionary<string, IReadOnlyList<ManagedSubscriptionEntry>> channelIndex;
    private readonly int maxAttempts;
    private readonly string retryPolicy;
    private readonly int retryDelayMilliseconds;
    private readonly string retryBackoff;
    private readonly int retryBackoffMultiplier;
    private readonly int retryMaxDelayMilliseconds;
    private readonly int retryJitterPercent;
    private readonly string idempotencyPolicy;
    private readonly string idempotencyKey;
    private readonly string idempotencyStore;
    private readonly string idempotencyScope;
    private readonly string idempotencyDurability;
    private readonly int idempotencyRetentionMinutes;
    private readonly EventSubscriptionExecutionPipelineDescriptor pipelineDescriptor;

    public InProcessEventSubscriptionExecutorCatalog(
        EventingOptions options,
        IEventSubscriptionCatalog subscriptions,
        IEnumerable<IEventSubscriptionExecutor> executors,
        EventSubscriptionExecutionPipelineDescriptor pipelineDescriptor)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(executors);
        ArgumentNullException.ThrowIfNull(pipelineDescriptor);

        this.pipelineDescriptor = pipelineDescriptor;
        maxAttempts = InProcessEventingRetryPolicy.GetMaxAttempts(options);
        retryPolicy = InProcessEventingRetryPolicy.GetPolicyId(options);
        retryDelayMilliseconds = InProcessEventingRetryPolicy.GetRetryDelayMilliseconds(options);
        retryBackoff = InProcessEventingRetryPolicy.GetBackoff(options);
        retryBackoffMultiplier = InProcessEventingRetryPolicy.GetBackoffMultiplier(options);
        retryMaxDelayMilliseconds = InProcessEventingRetryPolicy.GetMaxDelayMilliseconds(options);
        retryJitterPercent = InProcessEventingRetryPolicy.GetJitterPercent(options);
        idempotencyPolicy = InProcessEventingIdempotencyPolicy.GetPolicyId(options);
        idempotencyKey = InProcessEventingIdempotencyPolicy.GetKeyShape(options);
        idempotencyStore = InProcessEventingIdempotencyPolicy.GetStore(options);
        idempotencyScope = InProcessEventingIdempotencyPolicy.GetScope(options);
        idempotencyDurability = InProcessEventingIdempotencyPolicy.GetDurability(options);
        idempotencyRetentionMinutes = InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options);
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
                    ["subscriptionDescriptorDiscovery"] = GetSubscriptionDescriptorDiscovery(entry.Subscription),
                    ["subscriptionExecutionPipeline"] = pipelineDescriptor.PipelineId,
                    ["subscriptionExecutionMiddlewareCount"] = pipelineDescriptor.MiddlewareCount.ToString(CultureInfo.InvariantCulture),
                    ["retryPolicy"] = retryPolicy,
                    ["retryMaxAttempts"] = maxAttempts.ToString(CultureInfo.InvariantCulture),
                    ["retryDelayMilliseconds"] = retryDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
                    ["retryBackoff"] = retryBackoff,
                    ["retryBackoffMultiplier"] = retryBackoffMultiplier.ToString(CultureInfo.InvariantCulture),
                    ["retryMaxDelayMilliseconds"] = retryMaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
                    ["retryJitterPercent"] = retryJitterPercent.ToString(CultureInfo.InvariantCulture),
                    ["retryDurability"] = "none",
                    ["retryScope"] = "process-local",
                    ["idempotencyPolicy"] = idempotencyPolicy,
                    ["idempotencyKey"] = idempotencyKey,
                    ["idempotencyStore"] = idempotencyStore,
                    ["idempotencyRetentionMinutes"] = idempotencyRetentionMinutes.ToString(CultureInfo.InvariantCulture),
                    ["idempotencyDurability"] = idempotencyDurability,
                    ["idempotencyScope"] = idempotencyScope,
                    ["channelId"] = entry.Subscription.ChannelId,
                    ["handlerId"] = entry.Subscription.HandlerId,
                    ["subscriptionTagCount"] = entry.Subscription.Tags.Count.ToString(CultureInfo.InvariantCulture)
                }))
            .ToArray();
    }

    private static string GetSubscriptionDescriptorDiscovery(EventSubscriptionDescriptor subscription)
    {
        return subscription.Metadata.TryGetValue("descriptorDiscovery", out var discovery) &&
            !string.IsNullOrWhiteSpace(discovery)
            ? discovery
            : "none";
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
