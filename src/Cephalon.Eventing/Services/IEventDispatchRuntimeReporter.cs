namespace Cephalon.Eventing.Services;

/// <summary>
/// Accepts operator-facing dispatch runtime observations for durable event publication paths.
/// </summary>
public interface IEventDispatchRuntimeReporter
{
    /// <summary>
    /// Reports one dispatch observation for the active eventing runtime.
    /// </summary>
    /// <param name="report">The dispatch observation to capture.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the observation has been recorded.</returns>
    ValueTask ReportAsync(EventDispatchExecutionReport report, CancellationToken cancellationToken = default);
}
