using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkProjectionRuntimeSurfaceContributor(IProjectionCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "data-management",
            surfaceId: "projections",
            displayName: "Projections",
            description: "Read-model projection infrastructure registered through the active data provider.",
            entries: catalog.GetBySourceModule("entity-framework-data")
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(ProjectionDescriptor projection)
    {
        var metadata = new Dictionary<string, string>(projection.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = projection.SourceModuleId,
            ["targetStoreId"] = projection.TargetStoreId,
            ["mode"] = projection.Mode
        };

        if (projection.SourceContracts is { Count: > 0 })
        {
            metadata["sourceContracts"] = string.Join(",", projection.SourceContracts);
        }

        if (projection.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", projection.Tags);
        }

        return new TechnologyRuntimeEntry(
            id: projection.Id,
            displayName: projection.DisplayName,
            description: projection.Description,
            metadata: metadata);
    }
}
