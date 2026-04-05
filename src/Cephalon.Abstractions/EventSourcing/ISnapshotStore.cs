namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Persists and rehydrates optional aggregate snapshots for event-sourced workloads.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves one snapshot for the requested stream.
    /// </summary>
    /// <typeparam name="TState">The aggregate state type.</typeparam>
    /// <param name="streamId">The stable stream identifier.</param>
    /// <param name="version">The stream version represented by the snapshot.</param>
    /// <param name="state">The state payload to persist.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the snapshot has been persisted.</returns>
    Task SaveSnapshotAsync<TState>(
        string streamId,
        long version,
        TState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the latest snapshot for the requested stream.
    /// </summary>
    /// <typeparam name="TState">The aggregate state type.</typeparam>
    /// <param name="streamId">The stable stream identifier.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>
    /// A task that returns the snapshot state and version, or the default state and <c>-1</c> when none exists.
    /// </returns>
    Task<(TState? State, long Version)> LoadSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default);
}
