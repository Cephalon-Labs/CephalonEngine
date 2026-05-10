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
    /// Gets the bounded remediation command-history retention posture visible to the current runtime.
    /// </summary>
    EventDispatchRemediationRuntimeRetention Retention { get; }

    /// <summary>
    /// Gets the most recently observed remediation command-state entry visible to the current runtime.
    /// </summary>
    EventDispatchRemediationRuntimeState? Latest { get; }

    /// <summary>
    /// Gets the remediation command-state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<EventDispatchRemediationRuntimeState> States { get; }

    /// <summary>
    /// Gets remediation command-state entries that reserved a command identifier before dispatch-store mutation and have not finalized.
    /// </summary>
    /// <returns>The in-doubt command states, ordered by descending observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetInDoubt();

    /// <summary>
    /// Gets in-doubt remediation command-state entries observed at or before an optional UTC cutoff.
    /// </summary>
    /// <param name="beforeObservedAtUtc">The inclusive upper UTC observation cutoff, or <see langword="null" /> to return all in-doubt entries.</param>
    /// <returns>The in-doubt command states at or before the cutoff, ordered by descending observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetInDoubtBefore(DateTimeOffset? beforeObservedAtUtc);

    /// <summary>
    /// Gets the oldest retained in-doubt remediation command-state entry observed at or before an optional UTC cutoff.
    /// </summary>
    /// <param name="beforeObservedAtUtc">The inclusive upper UTC observation cutoff, or <see langword="null" /> to search all retained in-doubt entries.</param>
    /// <returns>The oldest matching in-doubt command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestInDoubtBefore(DateTimeOffset? beforeObservedAtUtc);

    /// <summary>
    /// Summarizes retained in-doubt remediation command-state entries observed at or before an optional UTC cutoff.
    /// </summary>
    /// <param name="beforeObservedAtUtc">The inclusive upper UTC observation cutoff, or <see langword="null" /> to summarize all retained in-doubt entries.</param>
    /// <returns>The aggregate in-doubt command summary for retained records at or before the cutoff.</returns>
    EventDispatchRemediationRuntimeSummary GetInDoubtSummaryBefore(DateTimeOffset? beforeObservedAtUtc);

    /// <summary>
    /// Gets remediation command-state entries observed inside an optional UTC observation window.
    /// </summary>
    /// <param name="fromObservedAtUtc">The inclusive lower UTC observation bound, or <see langword="null" /> to leave the start open.</param>
    /// <param name="toObservedAtUtc">The inclusive upper UTC observation bound, or <see langword="null" /> to leave the end open.</param>
    /// <returns>The recorded command states in the observation window, ordered by descending observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByObservedAt(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc);

    /// <summary>
    /// Summarizes remediation command-state entries observed inside an optional UTC observation window.
    /// </summary>
    /// <param name="fromObservedAtUtc">The inclusive lower UTC observation bound, or <see langword="null" /> to leave the start open.</param>
    /// <param name="toObservedAtUtc">The inclusive upper UTC observation bound, or <see langword="null" /> to leave the end open.</param>
    /// <returns>The aggregate remediation command summary for retained records in the observation window.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByObservedAt(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc);

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
    /// Summarizes remediation command-state entries recorded for one outbox identifier.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the outbox.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByOutboxId(string outboxId);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one outbox identifier.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to inspect.</param>
    /// <returns>The retained command-history posture for the outbox.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByOutboxId(string outboxId);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one outbox identifier.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByOutboxId(string outboxId);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one outbox identifier.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByOutboxId(string outboxId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one event message.
    /// </summary>
    /// <param name="messageId">The stable event message identifier to resolve.</param>
    /// <returns>The recorded command states for the message, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByMessageId(string messageId);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one event message.
    /// </summary>
    /// <param name="messageId">The stable event message identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the message.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByMessageId(string messageId);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one event message.
    /// </summary>
    /// <param name="messageId">The stable event message identifier to inspect.</param>
    /// <returns>The retained command-history posture for the message.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByMessageId(string messageId);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one event message.
    /// </summary>
    /// <param name="messageId">The stable event message identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByMessageId(string messageId);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one event message.
    /// </summary>
    /// <param name="messageId">The stable event message identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByMessageId(string messageId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one event channel.
    /// </summary>
    /// <param name="channelId">The stable event channel identifier to resolve.</param>
    /// <returns>The recorded command states for the channel, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByChannelId(string channelId);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one event channel.
    /// </summary>
    /// <param name="channelId">The stable event channel identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the channel.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByChannelId(string channelId);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one event channel.
    /// </summary>
    /// <param name="channelId">The stable event channel identifier to inspect.</param>
    /// <returns>The retained command-history posture for the channel.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByChannelId(string channelId);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one event channel.
    /// </summary>
    /// <param name="channelId">The stable event channel identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByChannelId(string channelId);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one event channel.
    /// </summary>
    /// <param name="channelId">The stable event channel identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByChannelId(string channelId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one command operation.
    /// </summary>
    /// <param name="operationId">The stable command operation identifier to resolve.</param>
    /// <returns>The recorded command states for the operation, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOperationId(string operationId);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one command operation.
    /// </summary>
    /// <param name="operationId">The stable command operation identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the operation.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByOperationId(string operationId);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one command operation.
    /// </summary>
    /// <param name="operationId">The stable command operation identifier to inspect.</param>
    /// <returns>The retained command-history posture for the operation.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByOperationId(string operationId);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one command operation.
    /// </summary>
    /// <param name="operationId">The stable command operation identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByOperationId(string operationId);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one command operation.
    /// </summary>
    /// <param name="operationId">The stable command operation identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByOperationId(string operationId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one operator actor.
    /// </summary>
    /// <param name="actorId">The stable operator actor identifier to resolve.</param>
    /// <returns>The recorded command states for the actor, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByActorId(string actorId);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one operator actor.
    /// </summary>
    /// <param name="actorId">The stable operator actor identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the actor.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByActorId(string actorId);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one operator actor.
    /// </summary>
    /// <param name="actorId">The stable operator actor identifier to inspect.</param>
    /// <returns>The retained command-history posture for the actor.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByActorId(string actorId);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one operator actor.
    /// </summary>
    /// <param name="actorId">The stable operator actor identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByActorId(string actorId);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one operator actor.
    /// </summary>
    /// <param name="actorId">The stable operator actor identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByActorId(string actorId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one operator correlation identifier.
    /// </summary>
    /// <param name="correlationId">The stable operator correlation identifier to resolve.</param>
    /// <returns>The recorded command states for the correlation identifier, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByCorrelationId(string correlationId);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one operator correlation identifier.
    /// </summary>
    /// <param name="correlationId">The stable operator correlation identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the correlation identifier.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByCorrelationId(string correlationId);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one operator correlation identifier.
    /// </summary>
    /// <param name="correlationId">The stable operator correlation identifier to inspect.</param>
    /// <returns>The retained command-history posture for the correlation identifier.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByCorrelationId(string correlationId);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one operator correlation identifier.
    /// </summary>
    /// <param name="correlationId">The stable operator correlation identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByCorrelationId(string correlationId);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one operator correlation identifier.
    /// </summary>
    /// <param name="correlationId">The stable operator correlation identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByCorrelationId(string correlationId);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one operator command reason.
    /// </summary>
    /// <param name="reason">The stable operator-facing command reason to resolve.</param>
    /// <returns>The recorded command states for the reason, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByReason(string reason);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one operator command reason.
    /// </summary>
    /// <param name="reason">The stable operator-facing command reason to summarize.</param>
    /// <returns>The aggregate remediation command summary for the reason.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByReason(string reason);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one operator command reason.
    /// </summary>
    /// <param name="reason">The stable operator-facing command reason to inspect.</param>
    /// <returns>The retained command-history posture for the reason.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByReason(string reason);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one operator command reason.
    /// </summary>
    /// <param name="reason">The stable operator-facing command reason to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByReason(string reason);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one operator command reason.
    /// </summary>
    /// <param name="reason">The stable operator-facing command reason to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByReason(string reason);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to resolve.</param>
    /// <returns>The recorded command states for the outcome, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the command outcome.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByOutcome(string outcome);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to inspect.</param>
    /// <returns>The retained command-history posture for the command outcome.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByOutcome(string outcome);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByOutcome(string outcome);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one command outcome.
    /// </summary>
    /// <param name="outcome">The stable command outcome identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByOutcome(string outcome);

    /// <summary>
    /// Gets the remediation command-state entries recorded for one dispatch-store outcome.
    /// </summary>
    /// <param name="dispatchOutcome">The stable dispatch-store outcome identifier to resolve.</param>
    /// <returns>The recorded command states for the dispatch outcome, ordered by observed time and command identifier.</returns>
    IReadOnlyList<EventDispatchRemediationRuntimeState> GetByDispatchOutcome(string dispatchOutcome);

    /// <summary>
    /// Summarizes remediation command-state entries recorded for one dispatch-store outcome.
    /// </summary>
    /// <param name="dispatchOutcome">The stable dispatch-store outcome identifier to summarize.</param>
    /// <returns>The aggregate remediation command summary for the dispatch-store outcome.</returns>
    EventDispatchRemediationRuntimeSummary GetSummaryByDispatchOutcome(string dispatchOutcome);

    /// <summary>
    /// Gets the retained remediation command-history posture recorded for one dispatch-store outcome.
    /// </summary>
    /// <param name="dispatchOutcome">The stable dispatch-store outcome identifier to inspect.</param>
    /// <returns>The retained command-history posture for the dispatch-store outcome.</returns>
    EventDispatchRemediationRuntimeRetention GetRetentionByDispatchOutcome(string dispatchOutcome);

    /// <summary>
    /// Gets the most recently observed remediation command-state entry recorded for one dispatch-store outcome.
    /// </summary>
    /// <param name="dispatchOutcome">The stable dispatch-store outcome identifier to resolve.</param>
    /// <returns>The latest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetLatestByDispatchOutcome(string dispatchOutcome);

    /// <summary>
    /// Gets the oldest retained remediation command-state entry recorded for one dispatch-store outcome.
    /// </summary>
    /// <param name="dispatchOutcome">The stable dispatch-store outcome identifier to resolve.</param>
    /// <returns>The oldest matching command state, or <see langword="null" /> when none is retained.</returns>
    EventDispatchRemediationRuntimeState? GetOldestByDispatchOutcome(string dispatchOutcome);
}
