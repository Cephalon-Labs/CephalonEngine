using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeDescriptorCatalog
{
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> index;

    public CdcCaptureExecutionRuntimeDescriptorCatalog(
        IEnumerable<ICdcCaptureExecutionRuntimeContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new CdcCaptureExecutionRuntimeRegistry();
        foreach (var contributor in contributors)
        {
            contributor.RegisterExecutionRuntimes(registry);
        }

        index = registry.Build()
            .ToDictionary(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> Runtimes => index.Values
        .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public CdcCaptureExecutionRuntimeDescriptor? GetById(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return null;
        }

        return index.TryGetValue(executionRuntimeId.Trim(), out var runtime)
            ? runtime
            : null;
    }
}
