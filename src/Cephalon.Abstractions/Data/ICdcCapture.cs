namespace Cephalon.Abstractions.Data;

/// <summary>
/// Captures database changes for one stable CDC surface and shapes them into outbox-ready publications.
/// </summary>
public interface ICdcCapture
{
    /// <summary>
    /// Gets the stable CDC capture identifier owned by this implementation.
    /// </summary>
    string CdcCaptureId { get; }

    /// <summary>
    /// Reads one bounded capture batch and returns the resulting outbox publications plus any
    /// provider-facing execution metadata.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the capture stream.</param>
    /// <returns>The captured batch result for the active CDC surface.</returns>
    ValueTask<CdcCaptureExecutionResult> CaptureAsync(CancellationToken cancellationToken = default);
}
