namespace Cephalon.Eventing.Services;

/// <summary>
/// Exposes the merged set of declared event subscriptions available to the active eventing runtime.
/// </summary>
public interface IEventSubscriptionCatalog
{
    /// <summary>
    /// Gets the effective subscription set after host options and module contributors have both been applied.
    /// </summary>
    IReadOnlyList<EventSubscriptionDescriptor> Subscriptions { get; }

    /// <summary>
    /// Attempts to resolve a subscription descriptor by identifier.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier to resolve.</param>
    /// <param name="subscription">When this method returns, contains the resolved subscription if found.</param>
    /// <returns><see langword="true" /> when the subscription exists; otherwise <see langword="false" />.</returns>
    bool TryGet(string subscriptionId, out EventSubscriptionDescriptor subscription);

    /// <summary>
    /// Gets the subscriptions currently bound to one event channel identifier.
    /// </summary>
    /// <param name="channelId">The event channel identifier to filter by.</param>
    /// <returns>The subscriptions declared for the requested channel.</returns>
    IReadOnlyList<EventSubscriptionDescriptor> GetByChannelId(string channelId);
}
