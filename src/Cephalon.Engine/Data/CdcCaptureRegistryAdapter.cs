using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class CdcCaptureRegistryAdapter(
    string moduleId,
    List<CdcCaptureDescriptor> cdcCaptures) : ICdcCaptureRegistry
{
    public void Add(CdcCaptureDescriptor cdcCapture)
    {
        ArgumentNullException.ThrowIfNull(cdcCapture);

        if (!string.Equals(cdcCapture.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"CDC capture '{cdcCapture.Id}' declared source module '{cdcCapture.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        cdcCaptures.Add(cdcCapture);
    }
}
