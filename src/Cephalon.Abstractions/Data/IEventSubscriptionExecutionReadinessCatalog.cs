namespace Cephalon.Abstractions.Data;

/// <summary>
/// Reads execution-readiness posture for declared event subscriptions.
/// </summary>
public interface IEventSubscriptionExecutionReadinessCatalog
{
    /// <summary>
    /// Gets the current execution-readiness descriptors for declared event subscriptions.
    /// </summary>
    IReadOnlyList<EventSubscriptionExecutionReadinessDescriptor> Readiness { get; }

    /// <summary>
    /// Gets the current execution-readiness descriptor for a declared subscription.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <returns>The readiness descriptor when the subscription exists; otherwise, <see langword="null" />.</returns>
    EventSubscriptionExecutionReadinessDescriptor? GetBySubscriptionId(string subscriptionId);

    /// <summary>
    /// Attempts to get the current execution-readiness descriptor for a declared subscription.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <param name="readiness">The resolved readiness descriptor when the subscription exists.</param>
    /// <returns><see langword="true" /> when the subscription exists; otherwise, <see langword="false" />.</returns>
    bool TryGet(string subscriptionId, out EventSubscriptionExecutionReadinessDescriptor? readiness);
}
