namespace Cephalon.Eventing.Services;

/// <summary>
/// Handles one configured event subscription without directly implementing the executor contract.
/// </summary>
/// <remarks>
/// The native eventing pack adapts these handlers into <see cref="IEventSubscriptionExecutor" />
/// instances when a host declares handler bindings through configuration. This keeps the consumer
/// type focused on business handling while the subscription id and runtime binding stay
/// configuration-owned.
/// </remarks>
public interface IEventSubscriptionHandler
{
    /// <summary>
    /// Handles the current event subscription execution attempt.
    /// </summary>
    /// <param name="context">The host-agnostic event subscription execution context.</param>
    /// <param name="cancellationToken">The cancellation token for the current execution attempt.</param>
    /// <returns>A task that completes when the handler attempt finishes.</returns>
    ValueTask HandleAsync(
        EventSubscriptionExecutionContext context,
        CancellationToken cancellationToken = default);
}
