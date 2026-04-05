namespace Cephalon.Eventing.Services;

internal sealed class EventSubscriptionRegistry : IEventSubscriptionRegistry
{
    private readonly List<EventSubscriptionDescriptor> subscriptions = [];

    public void Add(EventSubscriptionDescriptor subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        subscriptions.Add(subscription);
    }

    public IReadOnlyList<EventSubscriptionDescriptor> Build()
    {
        return subscriptions
            .GroupBy(static subscription => subscription.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static subscription => subscription.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
