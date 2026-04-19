namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Exposes the operator-facing durable-execution runtime state currently reported for active streams.
/// </summary>
public interface IDurableExecutionRuntimeStateCatalog
{
    /// <summary>
    /// Gets the reported durable-execution state entries visible to the current runtime.
    /// </summary>
    IReadOnlyList<DurableExecutionRuntimeState> States { get; }

    /// <summary>
    /// Gets the latest reported durable-execution state for one stream.
    /// </summary>
    /// <param name="streamId">The stable stream identifier to resolve.</param>
    /// <returns>The latest reported state, or <see langword="null" /> when that stream has not reported runtime state.</returns>
    DurableExecutionRuntimeState? GetByStreamId(string streamId);

    /// <summary>
    /// Gets the reported durable-execution state entries for one durable behavior.
    /// </summary>
    /// <param name="behaviorId">The stable durable behavior identifier to filter by.</param>
    /// <returns>The matching state entries, or an empty list when the behavior has not reported runtime state.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetByBehaviorId(string behaviorId);

    /// <summary>
    /// Gets the reported durable-execution state entries contributed by one source module.
    /// </summary>
    /// <param name="sourceModuleId">The source module identifier to filter by.</param>
    /// <returns>The matching state entries, or an empty list when the module has not reported runtime state.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets the reported durable-execution state entries exposed over one transport.
    /// </summary>
    /// <param name="transportId">The stable transport identifier to filter by.</param>
    /// <returns>The matching state entries, or an empty list when none reported runtime state for that transport.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetByTransportId(string transportId);

    /// <summary>
    /// Gets the reported durable-execution state entries that currently have one or more pending timers.
    /// </summary>
    /// <returns>The matching state entries, or an empty list when no stream currently reports pending timers.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetWithPendingTimers();

    /// <summary>
    /// Gets the reported durable-execution state entries that currently have one or more pending signals.
    /// </summary>
    /// <returns>The matching state entries, or an empty list when no stream currently reports pending signals.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetWithPendingSignals();

    /// <summary>
    /// Gets the reported durable-execution state entries that currently include the requested pending timer.
    /// </summary>
    /// <param name="timerId">The stable timer identifier to filter by.</param>
    /// <returns>The matching state entries, or an empty list when no stream currently reports that timer.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetByPendingTimerId(string timerId);

    /// <summary>
    /// Gets the reported durable-execution state entries that currently include the requested pending signal.
    /// </summary>
    /// <param name="signalId">The stable signal identifier to filter by.</param>
    /// <returns>The matching state entries, or an empty list when no stream currently reports that signal.</returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetByPendingSignalId(string signalId);

    /// <summary>
    /// Gets the reported durable-execution state entries that currently expose one or more compensation actions.
    /// </summary>
    /// <returns>
    /// The matching state entries, or an empty list when no stream currently reports compensation actions.
    /// </returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetWithCompensationActions();

    /// <summary>
    /// Gets the reported durable-execution state entries that currently include the requested compensation action.
    /// </summary>
    /// <param name="compensationActionId">The stable compensation-action identifier to filter by.</param>
    /// <returns>
    /// The matching state entries, or an empty list when no stream currently reports that compensation action.
    /// </returns>
    IReadOnlyList<DurableExecutionRuntimeState> GetByCompensationActionId(string compensationActionId);

    /// <summary>
    /// Tries to get the latest reported durable-execution state for one stream.
    /// </summary>
    /// <param name="streamId">The stable stream identifier to resolve.</param>
    /// <param name="state">Receives the latest reported state when the stream has reported one.</param>
    /// <returns><see langword="true" /> when a reported state exists; otherwise, <see langword="false" />.</returns>
    bool TryGetByStreamId(string streamId, out DurableExecutionRuntimeState? state);
}
