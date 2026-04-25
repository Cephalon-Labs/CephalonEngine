namespace Cephalon.Eventing.Services;

/// <summary>
/// Executes one declared event subscription through a pack-owned managed runtime.
/// </summary>
public interface IEventSubscriptionExecutor
{
    /// <summary>
    /// Gets the stable declared subscription identifier owned by this executor.
    /// </summary>
    string SubscriptionId { get; }

    /// <summary>
    /// Executes the managed subscription against the supplied publication context.
    /// </summary>
    /// <param name="context">The host-agnostic execution context for the current subscription attempt.</param>
    /// <param name="cancellationToken">The cancellation token for the current execution attempt.</param>
    /// <returns>A task that completes when the managed subscription attempt finishes.</returns>
    ValueTask ExecuteAsync(
        EventSubscriptionExecutionContext context,
        CancellationToken cancellationToken = default);
}
