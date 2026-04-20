using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeRegistry : ICdcCaptureExecutionRuntimeRegistry
{
    private readonly List<CdcCaptureExecutionRuntimeDescriptor> runtimes = [];

    public void Add(CdcCaptureExecutionRuntimeDescriptor executionRuntime)
    {
        ArgumentNullException.ThrowIfNull(executionRuntime);

        runtimes.Add(executionRuntime);
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> Build()
    {
        return runtimes
            .GroupBy(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
