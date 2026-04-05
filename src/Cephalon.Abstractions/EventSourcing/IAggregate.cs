namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Applies domain events to an aggregate state projection.
/// </summary>
/// <typeparam name="TState">The state shape produced by the aggregate.</typeparam>
public interface IAggregate<TState>
{
    /// <summary>
    /// Applies one event to the current state and returns the next state snapshot.
    /// </summary>
    /// <param name="current">The current aggregate state.</param>
    /// <param name="evt">The event to apply.</param>
    /// <returns>The updated aggregate state.</returns>
    TState Apply(TState current, IDomainEvent evt);
}
