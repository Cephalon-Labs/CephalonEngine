using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Data.Services;

internal sealed class DataCdcExecutionRuntimeSurfaceContributor(ICdcCaptureExecutionRuntimeCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "data-management",
            surfaceId: "cdc-capture-runtimes",
            displayName: "CDC Capture Runtimes",
            description: "Execution runtimes that own or observe active change-data-capture surfaces.",
            entries: catalog.Runtimes
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        var metadata = DataCdcRuntimeSurfaceMetadata.CreateSanitized(runtime.Metadata);

        metadata["executionOwnership"] = runtime.ExecutionOwnership;
        metadata["executionTopology"] = runtime.ExecutionTopology;

        DataCdcRuntimeSurfaceMetadata.UpsertSanitized(metadata, "acknowledgementMode", runtime.AcknowledgementMode);
        DataCdcRuntimeSurfaceMetadata.UpsertSanitized(metadata, "hostedExecutionId", runtime.HostedExecutionId);
        DataCdcRuntimeSurfaceMetadata.UpsertSanitized(metadata, "executionGraphId", runtime.ExecutionGraphId);

        if (runtime.ObservationStaleAfterSeconds is { } observationStaleAfterSeconds)
        {
            metadata["observationStaleAfterSeconds"] = observationStaleAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        if (runtime.ReporterLeaseSeconds is { } reporterLeaseSeconds)
        {
            metadata["reporterLeaseSeconds"] = reporterLeaseSeconds.ToString(CultureInfo.InvariantCulture);
        }

        if (runtime.RejectOutOfOrderReports)
        {
            metadata["rejectOutOfOrderReports"] = bool.TrueString;
        }

        if (runtime.RejectConflictingReporterIds)
        {
            metadata["rejectConflictingReporterIds"] = bool.TrueString;
        }

        if (runtime.CdcCaptureIds.Count > 0)
        {
            metadata["cdcCaptureIds"] = string.Join(",", runtime.CdcCaptureIds);
        }

        if (runtime.EdgeNodeIds.Count > 0)
        {
            metadata["edgeNodeIds"] = string.Join(",", runtime.EdgeNodeIds);
        }

        return new TechnologyRuntimeEntry(
            id: runtime.Id,
            displayName: runtime.DisplayName,
            description: runtime.Description,
            metadata: metadata);
    }
}
