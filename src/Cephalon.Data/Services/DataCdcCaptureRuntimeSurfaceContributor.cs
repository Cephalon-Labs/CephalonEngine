using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Data.Services;

internal sealed class DataCdcCaptureRuntimeSurfaceContributor(ICdcCaptureCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "data-management",
            surfaceId: "cdc-captures",
            displayName: "CDC Captures",
            description: "Change-data-capture surfaces contributed by data provider packs and bound to the active execution runtimes.",
            entries: catalog.CdcCaptures
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(CdcCaptureDescriptor capture)
    {
        var metadata = DataCdcRuntimeSurfaceMetadata.CreateSanitized(capture.Metadata);
        var binding = capture.ExecutionBinding;

        metadata["sourceModuleId"] = capture.SourceModuleId;
        metadata["provider"] = capture.Provider;
        metadata["sourceId"] = capture.SourceId;
        metadata["outboxId"] = capture.OutboxId;
        metadata["mode"] = capture.Mode;
        metadata["eventFormat"] = capture.EventFormat;
        metadata["executionOwnership"] = binding.ExecutionOwnership;
        metadata["executionTopology"] = binding.ExecutionTopology;
        metadata["executionBindingResolutionMode"] = binding.ResolutionMode;

        DataCdcRuntimeSurfaceMetadata.UpsertSanitized(metadata, "authoredExecutionRuntimeId", binding.AuthoredExecutionRuntimeId);
        DataCdcRuntimeSurfaceMetadata.UpsertSanitized(metadata, "requestedExecutionRuntimeId", binding.RequestedExecutionRuntimeId);
        DataCdcRuntimeSurfaceMetadata.UpsertSanitized(metadata, "effectiveExecutionRuntimeId", binding.EffectiveExecutionRuntimeId);
        DataCdcRuntimeSurfaceMetadata.AddSanitized(metadata, binding.Metadata);

        if (capture.ResourceIds.Count > 0)
        {
            metadata["resourceIds"] = string.Join(",", capture.ResourceIds);
        }

        if (capture.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", capture.Tags);
        }

        return new TechnologyRuntimeEntry(
            id: capture.Id,
            displayName: capture.DisplayName,
            description: capture.Description,
            metadata: metadata);
    }
}
