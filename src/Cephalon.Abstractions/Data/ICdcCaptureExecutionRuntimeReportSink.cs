namespace Cephalon.Abstractions.Data;

/// <summary>
/// Accepts operator-facing CDC runtime observations that are reported on behalf of one execution runtime.
/// </summary>
public interface ICdcCaptureExecutionRuntimeReportSink
{
    /// <summary>
    /// Reports one or more CDC runtime observations for the supplied execution runtime.
    /// </summary>
    /// <param name="executionRuntimeId">The stable execution-runtime identifier that owns the reported captures.</param>
    /// <param name="observations">The capture observations to merge into the active runtime-state catalog.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    ValueTask ReportAsync(
        string executionRuntimeId,
        IReadOnlyList<CdcCaptureRuntimeObservation> observations,
        CancellationToken cancellationToken = default);
}
