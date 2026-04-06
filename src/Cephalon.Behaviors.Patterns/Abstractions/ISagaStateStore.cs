namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>Provides saga state persistence for the saga-step execution pattern.</summary>
public interface ISagaStateStore
{
    /// <summary>Retrieves the saga state for the given saga identifier.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="sagaId">The unique identifier of the saga instance.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>The deserialized saga state, or <see langword="null"/> if not found.</returns>
    Task<T?> GetAsync<T>(string sagaId, CancellationToken ct = default);

    /// <summary>Persists the saga state for the given saga identifier.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="sagaId">The unique identifier of the saga instance.</param>
    /// <param name="state">The state to persist.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes when the state has been persisted.</returns>
    Task SaveAsync<T>(string sagaId, T state, CancellationToken ct = default);

    /// <summary>Removes the saga state for the given saga identifier.</summary>
    /// <param name="sagaId">The unique identifier of the saga instance to remove.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes when the state has been removed.</returns>
    Task DeleteAsync(string sagaId, CancellationToken ct = default);
}
