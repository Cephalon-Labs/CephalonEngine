using Cephalon.Data.Configuration;

namespace Cephalon.Data.Services;

internal sealed class ConfiguredCdcCaptureExecutionRuntimeContributor(DataRuntimeOptions options)
    : ICdcCaptureExecutionRuntimeContributor
{
    public void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
    {
        ArgumentNullException.ThrowIfNull(executionRuntimes);

        foreach (var runtime in options.CdcExecutionRuntimes)
        {
            ArgumentNullException.ThrowIfNull(runtime);
            executionRuntimes.Add(runtime.ToDescriptor());
        }
    }
}
