namespace Cephalon.Eventing.Services;

/// <summary>
/// Collects event subscription descriptors contributed to the active eventing runtime pack.
/// </summary>
public interface IEventSubscriptionRegistry
{
    /// <summary>
    /// Adds an event subscription descriptor to the registry.
    /// </summary>
    /// <param name="subscription">The subscription descriptor to contribute.</param>
    void Add(EventSubscriptionDescriptor subscription);
}
