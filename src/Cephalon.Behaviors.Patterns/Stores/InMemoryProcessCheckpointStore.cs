using System.Collections.Concurrent;
using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Stores;

/// <summary>
/// An in-memory implementation of <see cref="IProcessCheckpointStore"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Suitable for development and testing; replace with a durable store for production use.
/// </summary>
public sealed class InMemoryProcessCheckpointStore : IProcessCheckpointStore
{
    private readonly ConcurrentDictionary<string, ProcessCheckpoint> _store =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves the checkpoint for the given process identifier.
    /// </summary>
    /// <param name="processId">The unique identifier of the process instance.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>The checkpoint, or <see langword="null"/> if not found.</returns>
    public Task<ProcessCheckpoint?> GetAsync(string processId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processId);

        _store.TryGetValue(processId, out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    /// <summary>
    /// Upserts the checkpoint for the given process identifier.
    /// </summary>
    /// <param name="processId">The unique identifier of the process instance.</param>
    /// <param name="checkpoint">The checkpoint to persist.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A completed task.</returns>
    public Task SaveAsync(string processId, ProcessCheckpoint checkpoint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processId);
        ArgumentNullException.ThrowIfNull(checkpoint);

        _store[processId] = checkpoint;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the checkpoint for the given process identifier.
    /// </summary>
    /// <param name="processId">The unique identifier of the process instance to remove.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A completed task.</returns>
    public Task DeleteAsync(string processId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processId);

        _store.TryRemove(processId, out _);
        return Task.CompletedTask;
    }
}
