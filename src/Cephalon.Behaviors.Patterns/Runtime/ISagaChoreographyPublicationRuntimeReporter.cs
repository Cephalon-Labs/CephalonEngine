namespace Cephalon.Behaviors.Patterns.Runtime;

/// <summary>
/// Accepts operator-facing live saga-choreography publication observations for the active runtime.
/// </summary>
internal interface ISagaChoreographyPublicationRuntimeReporter
{
    /// <summary>
    /// Reports one choreography publication observation for the active runtime.
    /// </summary>
    /// <param name="report">The publication observation to capture.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the observation has been recorded.</returns>
    ValueTask ReportAsync(SagaChoreographyPublicationExecutionReport report, CancellationToken cancellationToken = default);
}
