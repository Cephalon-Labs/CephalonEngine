namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes one or more CDC capture descriptors to the active runtime.
/// </summary>
public interface ICdcCaptureContributor
{
    /// <summary>
    /// Registers one or more CDC capture descriptors with the supplied registry.
    /// </summary>
    /// <param name="cdcCaptures">The registry that collects contributed CDC capture descriptors.</param>
    void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures);
}
