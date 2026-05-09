namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes operator-facing runtime state recorded by event-dispatch remediation command paths.
/// </summary>
public interface IEventDispatchRemediationRuntimeCatalog
{
    /// <summary>
    /// Gets the remediation command-state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<EventDispatchRemediationRuntimeState> States { get; }

    /// <summary>
    /// Gets one remediation command-state entry by command identifier.
    /// </summary>
    /// <param name="commandId">The stable remediation command identifier to resolve.</param>
    /// <returns>The recorded command state, or <see langword="null" /> when the command has not reported runtime state.</returns>
    EventDispatchRemediationRuntimeState? GetByCommandId(string commandId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one outbox identifier.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to resolve.</param>
    /// <returns>The recorded command states for the outbox, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutboxId(string outboxId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to resolve.</param>
    /// <returns>The recorded command states for the outcome, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome);
}
