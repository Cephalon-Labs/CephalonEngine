namespace Cephalon.Eventing.Services;

/// <summary>
/// Contributes one or more managed execution bindings for declared event subscriptions.
/// </summary>
public interface IEventSubscriptionExecutionBindingContributor
{
    /// <summary>
    /// Returns the managed execution bindings owned by the contributor.
    /// </summary>
    /// <returns>The managed execution bindings for declared subscriptions.</returns>
    IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> GetExecutionBindings();
}
