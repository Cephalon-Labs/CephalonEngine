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
            var metadata = new Dictionary<string, string>(cdcCapture.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["contributorModuleId"] = moduleId
            };

            cdcCapture = new CdcCaptureDescriptor(
                id: cdcCapture.Id,
                displayName: cdcCapture.DisplayName,
                description: cdcCapture.Description,
                sourceModuleId: cdcCapture.SourceModuleId,
                provider: cdcCapture.Provider,
                sourceId: cdcCapture.SourceId,
                outboxId: cdcCapture.OutboxId,
                executionBinding: cdcCapture.ExecutionBinding,
                mode: cdcCapture.Mode,
                eventFormat: cdcCapture.EventFormat,
                resourceIds: cdcCapture.ResourceIds,
                tags: cdcCapture.Tags,
                metadata: metadata);
        }

        cdcCaptures.Add(cdcCapture);
    }
}
