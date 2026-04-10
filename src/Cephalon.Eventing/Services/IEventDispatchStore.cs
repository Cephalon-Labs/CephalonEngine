namespace Cephalon.Eventing.Services;

/// <summary>
/// Reads pending staged events and applies durable dispatch outcomes for the active eventing runtime.
/// </summary>
/// <remarks>
/// This contract stays runtime-neutral on purpose. It does not claim broker ownership, delivery guarantees,
/// or concurrency semantics beyond what the active outbox implementation actually provides.
/// </remarks>
public interface IEventDispatchStore
{
    /// <summary>
    /// Gets the outbox identifiers explicitly owned by the dispatch store.
    /// </summary>
    IReadOnlyList<string> OutboxIds { get; }

    /// <summary>
    /// Reads pending staged events that are eligible for dispatch.
    /// </summary>
    /// <param name="maximumCount">The maximum number of staged events to read.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The pending staged events that are currently eligible for dispatch.</returns>
    ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(int maximumCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies one durable dispatch outcome to the active staged-event store.
    /// </summary>
    /// <param name="report">The dispatch observation to apply.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the durable store has applied the dispatch observation.</returns>
    ValueTask ApplyReportAsync(EventDispatchExecutionReport report, CancellationToken cancellationToken = default);
}
