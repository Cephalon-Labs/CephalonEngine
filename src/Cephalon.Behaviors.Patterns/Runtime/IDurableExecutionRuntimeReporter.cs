namespace Cephalon.Behaviors.Patterns.Runtime;

/// <summary>
/// Accepts operator-facing durable-execution runtime observations for active streams.
/// </summary>
internal interface IDurableExecutionRuntimeReporter
{
    /// <summary>
    /// Reports one durable-execution observation for the active runtime.
    /// </summary>
    /// <param name="report">The durable-execution observation to capture.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the observation has been recorded.</returns>
    ValueTask ReportAsync(DurableExecutionExecutionReport report, CancellationToken cancellationToken = default);
}
