namespace Cephalon.Abstractions.Data;

/// <summary>
/// Receives CDC capture descriptors contributed by active modules or packages.
/// </summary>
public interface ICdcCaptureRegistry
{
    /// <summary>
    /// Adds a CDC capture to the current runtime composition.
    /// </summary>
    /// <param name="cdcCapture">The CDC capture descriptor to register.</param>
    void Add(CdcCaptureDescriptor cdcCapture);
}
