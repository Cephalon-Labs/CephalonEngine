namespace Cephalon.EventSourcing.Services;

/// <summary>
/// Returns the aggregate state and replay evidence for one event-stream replay operation.
/// </summary>
/// <typeparam name="TState">The aggregate state shape produced by replay.</typeparam>
public sealed class EventStreamReplayResult<TState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamReplayResult{TState}" /> class.
    /// </summary>
    /// <param name="state">The aggregate state produced by replay.</param>
    /// <param name="report">The replay evidence emitted by the worker.</param>
    public EventStreamReplayResult(TState state, EventStreamReplayReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        State = state;
        Report = report;
    }

    /// <summary>
    /// Gets the aggregate state produced by replay.
    /// </summary>
    public TState State { get; }

    /// <summary>
    /// Gets the replay evidence emitted by the worker.
    /// </summary>
    public EventStreamReplayReport Report { get; }
}
