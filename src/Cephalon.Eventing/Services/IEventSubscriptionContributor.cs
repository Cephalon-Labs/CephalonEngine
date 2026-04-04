namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows a module to contribute declared event subscriptions into the active eventing runtime pack.
/// </summary>
public interface IEventSubscriptionContributor
{
    /// <summary>
    /// Registers one or more event subscription descriptors with the supplied registry.
    /// </summary>
    /// <param name="subscriptions">The registry that collects contributed subscription descriptors.</param>
    void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions);
}
