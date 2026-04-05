using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class EventSubscriptionCatalog : IEventSubscriptionCatalog
{
    private readonly Dictionary<string, EventSubscriptionDescriptor> index;
    private readonly Dictionary<string, IReadOnlyList<EventSubscriptionDescriptor>> channelIndex;

    public EventSubscriptionCatalog(
        EventingOptions options,
        IEventChannelCatalog channels,
        IEnumerable<IEventSubscriptionContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new EventSubscriptionRegistry();
        foreach (var subscription in options.Subscriptions)
        {
            registry.Add(subscription);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterSubscriptions(registry);
        }

        var subscriptions = registry.Build();
        foreach (var subscription in subscriptions)
        {
            if (!channels.TryGet(subscription.ChannelId, out _))
            {
                throw new InvalidOperationException(
                    $"Event subscription '{subscription.Id}' references unknown event channel '{subscription.ChannelId}'. Register the channel before contributing the subscription.");
            }
        }

        Subscriptions = subscriptions;
        index = Subscriptions.ToDictionary(static subscription => subscription.Id, StringComparer.OrdinalIgnoreCase);
        channelIndex = Subscriptions
            .GroupBy(static subscription => subscription.ChannelId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<EventSubscriptionDescriptor>)group
                    .OrderBy(static subscription => subscription.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventSubscriptionDescriptor> Subscriptions { get; }

    public bool TryGet(string subscriptionId, out EventSubscriptionDescriptor subscription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        return index.TryGetValue(subscriptionId.Trim(), out subscription!);
    }

    public IReadOnlyList<EventSubscriptionDescriptor> GetByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        return channelIndex.TryGetValue(channelId.Trim(), out var subscriptions)
            ? subscriptions
            : [];
    }
}
