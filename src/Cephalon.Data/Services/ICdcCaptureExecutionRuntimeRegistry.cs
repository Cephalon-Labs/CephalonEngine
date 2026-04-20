using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

/// <summary>
/// Receives operator-facing CDC capture execution runtime descriptors contributed by active data packs.
/// </summary>
public interface ICdcCaptureExecutionRuntimeRegistry
{
    /// <summary>
    /// Adds one CDC capture execution runtime to the current data-runtime composition.
    /// </summary>
    /// <param name="executionRuntime">The execution runtime to register.</param>
    void Add(CdcCaptureExecutionRuntimeDescriptor executionRuntime);
}
