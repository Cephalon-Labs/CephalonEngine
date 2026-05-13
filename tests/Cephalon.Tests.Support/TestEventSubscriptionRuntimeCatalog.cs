using Cephalon.Eventing.Services;

namespace Cephalon.Tests.Support;

public sealed class TestEventSubscriptionRuntimeCatalog : IEventSubscriptionRuntimeCatalog
{
    private readonly IReadOnlyList<EventSubscriptionRuntimeState> states;
    private readonly Dictionary<string, EventSubscriptionRuntimeState> statesBySubscriptionId;

    public TestEventSubscriptionRuntimeCatalog(params EventSubscriptionRuntimeState[] states)
    {
        ArgumentNullException.ThrowIfNull(states);

        this.states = states
            .OrderBy(static state => state.SubscriptionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        statesBySubscriptionId = this.states.ToDictionary(static state => state.SubscriptionId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EventSubscriptionRuntimeState> States => states;

    public EventSubscriptionRuntimeState? GetById(string subscriptionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        return statesBySubscriptionId.TryGetValue(subscriptionId.Trim(), out var state)
            ? state
            : null;
    }

    public bool TryGet(string subscriptionId, out EventSubscriptionRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        return statesBySubscriptionId.TryGetValue(subscriptionId.Trim(), out state);
    }
}
