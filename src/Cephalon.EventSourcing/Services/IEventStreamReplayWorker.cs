using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Replays event streams through aggregate hydration, optional snapshots, and registered projection rebuild handlers.
/// </summary>
public interface IEventStreamReplayWorker
{
    /// <summary>
    /// Replays one aggregate stream from the supplied event store.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type that applies domain events.</typeparam>
    /// <typeparam name="TState">The aggregate state shape.</typeparam>
    /// <param name="eventStore">The event store to read from.</param>
    /// <param name="request">The replay request.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The aggregate state and replay evidence produced by the operation.</returns>
    Task<EventStreamReplayResult<TState>> ReplayAggregateAsync<TAggregate, TState>(
        IEventStore eventStore,
        EventStreamReplayRequest request,
        CancellationToken cancellationToken = default)
        where TAggregate : IAggregate<TState>, new();
}
