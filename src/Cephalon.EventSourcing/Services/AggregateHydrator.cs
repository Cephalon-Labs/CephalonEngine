using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Rehydrates aggregate state by replaying domain events from an event store.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type that applies domain events.</typeparam>
/// <typeparam name="TState">The aggregate state shape.</typeparam>
public sealed class AggregateHydrator<TAggregate, TState>
    where TAggregate : IAggregate<TState>, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateHydrator{TAggregate, TState}" /> class.
    /// </summary>
    public AggregateHydrator()
    {
    }

    /// <summary>
    /// Rehydrates one aggregate state by replaying the requested event stream.
    /// </summary>
    /// <param name="eventStore">The event store to read from.</param>
    /// <param name="streamId">The stable stream identifier.</param>
    /// <param name="fromVersion">The first version to replay. The default is <c>0</c>.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The rehydrated aggregate state and the latest version applied.</returns>
    public async Task<(TState State, long Version)> HydrateAsync(
        IEventStore eventStore,
        string streamId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventStore);

        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var aggregate = new TAggregate();
        var state = default(TState)!;
        var version = fromVersion <= 0 ? -1 : fromVersion - 1;

        await foreach (var evt in eventStore.ReadStreamAsync(streamId.Trim(), fromVersion, cancellationToken))
        {
            state = aggregate.Apply(state, evt);
            version = evt.StreamVersion;
        }

        return (state, version);
    }
}
