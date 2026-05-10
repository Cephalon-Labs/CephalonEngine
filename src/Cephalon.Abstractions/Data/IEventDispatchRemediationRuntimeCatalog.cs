namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes operator-facing runtime state recorded by event-dispatch remediation command paths.
/// </summary>
public interface IEventDispatchRemediationRuntimeCatalog
{
    /// <summary>
    /// Gets the aggregate remediation command-state summary visible to the current runtime.
    /// </summary>
    EventDispatchRemediationRuntimeSummary Summary { get; }

    /// <summary>
    /// Gets the most recently observed remediation command-state entry visible to the current runtime.
    /// </summary>
    EventDispatchRemediationRuntimeState? Latest { get; }

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
    /// Gets the remediation command-state entries recorded for one event message.
    /// </summary>
    /// <param name="messageId">The stable event message identifier to resolve.</param>
    /// <returns>The recorded command states for the message, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByMessageId(string messageId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one event channel.
    /// </summary>
    /// <param name="channelId">The stable event channel identifier to resolve.</param>
    /// <returns>The recorded command states for the channel, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByChannelId(string channelId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one command operation.
    /// </summary>
    /// <param name="operationId">The stable command operation identifier to resolve.</param>
    /// <returns>The recorded command states for the operation, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOperationId(string operationId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one operator actor.
    /// </summary>
    /// <param name="actorId">The stable operator actor identifier to resolve.</param>
    /// <returns>The recorded command states for the actor, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByActorId(string actorId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one operator correlation identifier.
    /// </summary>
    /// <param name="correlationId">The stable operator correlation identifier to resolve.</param>
    /// <returns>The recorded command states for the correlation identifier, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByCorrelationId(string correlationId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one operator command reason.
    /// </summary>
    /// <param name="reason">The stable operator-facing command reason to resolve.</param>
    /// <returns>The recorded command states for the reason, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByReason(string reason);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to resolve.</param>
    /// <returns>The recorded command states for the outcome, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one dispatch-store outcome.
    /// </summary>
    /// <param name="dispatchOutcome">The stable dispatch-store outcome identifier to resolve.</param>
    /// <returns>The recorded command states for the dispatch outcome, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByDispatchOutcome(string dispatchOutcome);
}
