namespace Cephalon.Data.Services;

/// <summary>
/// Accepts operator-facing CDC runtime observations for active capture surfaces.
/// </summary>
public interface ICdcCaptureRuntimeReporter
{
    /// <summary>
    /// Reports one CDC runtime observation for the active runtime.
    /// </summary>
    /// <param name="report">The CDC runtime observation to capture.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the observation has been recorded.</returns>
    ValueTask ReportAsync(CdcCaptureExecutionReport report, CancellationToken cancellationToken = default);
}
