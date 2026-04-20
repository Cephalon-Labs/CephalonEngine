using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

/// <summary>
/// Contributes one or more operator-facing CDC capture execution runtimes to the active data runtime.
/// </summary>
public interface ICdcCaptureExecutionRuntimeContributor
{
    /// <summary>
    /// Registers one or more CDC capture execution runtime descriptors owned by the contributor.
    /// </summary>
    /// <param name="executionRuntimes">The execution-runtime registry receiving contributed descriptors.</param>
    void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes);
}
