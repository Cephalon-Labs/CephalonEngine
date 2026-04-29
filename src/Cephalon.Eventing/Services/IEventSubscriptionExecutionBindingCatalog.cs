namespace Cephalon.Eventing.Services;

/// <summary>
/// Exposes managed execution bindings for declared event subscriptions.
/// </summary>
/// <remarks>
/// The catalog is a host-agnostic read contract for companion packs that bind declared subscriptions
/// to a real execution runtime. An empty catalog is a valid answer and means the active eventing
/// pack is still descriptor-first or application-managed for subscription execution.
/// </remarks>
public interface IEventSubscriptionExecutionBindingCatalog
{
    /// <summary>
    /// Gets the currently active managed execution bindings ordered by subscription identifier.
    /// </summary>
    IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> Bindings { get; }

    /// <summary>
    /// Looks up the managed execution binding for one declared subscription.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <returns>The managed execution binding when one is active; otherwise, <see langword="null" />.</returns>
    EventSubscriptionExecutionBindingDescriptor? GetBySubscriptionId(string subscriptionId);

    /// <summary>
    /// Attempts to resolve the managed execution binding for one declared subscription.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <param name="binding">When this method returns, contains the resolved binding when one is active.</param>
    /// <returns><see langword="true" /> when a binding exists; otherwise, <see langword="false" />.</returns>
    bool TryGet(string subscriptionId, out EventSubscriptionExecutionBindingDescriptor? binding);
}
