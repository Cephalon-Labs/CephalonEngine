namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Carries the replayed durable-execution state for one workflow invocation.
/// </summary>
/// <typeparam name="TState">The workflow state shape.</typeparam>
public sealed class DurableExecutionState<TState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurableExecutionState{TState}" /> class.
    /// </summary>
    /// <param name="streamId">The event-store stream that owns the workflow history.</param>
    /// <param name="state">The replayed workflow state.</param>
    /// <param name="version">The latest replayed stream version, or <c>-1</c> when no stream exists yet.</param>
    public DurableExecutionState(
        string streamId,
        TState state,
        long version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        StreamId = streamId.Trim();
        State = state;
        Version = version;
    }

    /// <summary>
    /// Gets the event-store stream that owns the workflow history.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the replayed workflow state.
    /// </summary>
    public TState State { get; }

    /// <summary>
    /// Gets the latest replayed stream version, or <c>-1</c> when no stream exists yet.
    /// </summary>
    public long Version { get; }

    /// <summary>
    /// Gets a value indicating whether this execution already has persisted history.
    /// </summary>
    public bool Exists => Version >= 0;
}
