namespace Cephalon.Eventing.Services;

/// <summary>
/// Exposes the latest reported runtime state for declared event subscriptions.
/// </summary>
public interface IEventSubscriptionRuntimeCatalog
{
    /// <summary>
    /// Gets the currently known runtime-state entries ordered by subscription identifier.
    /// </summary>
    IReadOnlyList<EventSubscriptionRuntimeState> States { get; }

    /// <summary>
    /// Looks up one reported runtime-state entry by declared subscription identifier.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <returns>The current runtime state when one has been reported; otherwise, <see langword="null" />.</returns>
    EventSubscriptionRuntimeState? GetById(string subscriptionId);

    /// <summary>
    /// Attempts to look up one reported runtime-state entry by declared subscription identifier.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <param name="state">The current runtime state when one has been reported.</param>
    /// <returns><see langword="true" /> when one runtime-state entry is available; otherwise, <see langword="false" />.</returns>
    bool TryGet(string subscriptionId, out EventSubscriptionRuntimeState? state);
}
