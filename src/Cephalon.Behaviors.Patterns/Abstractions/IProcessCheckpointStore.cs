namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>Provides process checkpoint persistence for the process-manager execution pattern.</summary>
public interface IProcessCheckpointStore
{
    /// <summary>Retrieves the checkpoint for the given process identifier.</summary>
    /// <param name="processId">The unique identifier of the process instance.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>The checkpoint, or <see langword="null"/> if not found.</returns>
    Task<ProcessCheckpoint?> GetAsync(string processId, CancellationToken ct = default);

    /// <summary>Persists the checkpoint for the given process identifier.</summary>
    /// <param name="processId">The unique identifier of the process instance.</param>
    /// <param name="checkpoint">The checkpoint to persist.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes when the checkpoint has been persisted.</returns>
    Task SaveAsync(string processId, ProcessCheckpoint checkpoint, CancellationToken ct = default);

    /// <summary>Removes the checkpoint for the given process identifier.</summary>
    /// <param name="processId">The unique identifier of the process instance to remove.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes when the checkpoint has been removed.</returns>
    Task DeleteAsync(string processId, CancellationToken ct = default);
}

/// <summary>Represents a snapshot of a process manager's current step and metadata.</summary>
public class ProcessCheckpoint
{
    /// <summary>Gets the unique identifier of the process instance.</summary>
    public required string ProcessId { get; init; }

    /// <summary>Gets the name of the current step within the process.</summary>
    public required string CurrentStep { get; init; }

    /// <summary>Gets the timestamp when this process was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the timestamp when this process completed, or <see langword="null"/> if still in progress.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Gets additional metadata associated with this checkpoint.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
