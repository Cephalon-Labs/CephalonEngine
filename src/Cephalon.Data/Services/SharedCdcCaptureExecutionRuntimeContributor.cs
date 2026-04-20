using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class SharedCdcCaptureExecutionRuntimeContributor(
    ICdcCaptureCatalog cdcCaptures) : ICdcCaptureExecutionRuntimeContributor
{
    public void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
    {
        ArgumentNullException.ThrowIfNull(executionRuntimes);

        executionRuntimes.Add(new CdcCaptureExecutionRuntimeDescriptor(
            id: DataRuntimeIds.CdcExecutionRuntimeId,
            displayName: "Shared CDC Capture Pump",
            description: "Runs the shared Cephalon.Data background pump that resolves active CDC captures, stages linked outbox publications, and optionally acknowledges durable provider progress.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data",
                ["executionOwnership"] = "host-managed",
                ["executionTopology"] = "shared-in-process-polling",
                ["acknowledgementMode"] = "post-stage-provider",
                ["hostedExecutionId"] = DataRuntimeIds.CdcHostedExecutionId,
                ["executionGraphId"] = DataRuntimeIds.CdcExecutionGraphId,
                ["surface"] = "shared-cdc-execution"
            },
            cdcCaptureIds: cdcCaptures.CdcCaptures
                .Select(static capture => capture.Id)
                .ToArray()));
    }
}
