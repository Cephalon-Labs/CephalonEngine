using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class ProjectionCatalogSnapshot : IProjectionCatalog
{
    private readonly IReadOnlyList<ProjectionDescriptor> projections;
    private readonly Dictionary<string, ProjectionDescriptor> projectionsById;
    private readonly Dictionary<string, IReadOnlyList<ProjectionDescriptor>> projectionsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<ProjectionDescriptor>> projectionsByTargetStore;

    public ProjectionCatalogSnapshot(IEnumerable<ProjectionDescriptor> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);

        this.projections = projections.ToArray();
        projectionsById = this.projections.ToDictionary(static projection => projection.Id, StringComparer.OrdinalIgnoreCase);
        projectionsBySourceModule = this.projections
            .GroupBy(static projection => projection.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<ProjectionDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        projectionsByTargetStore = this.projections
            .GroupBy(static projection => projection.TargetStoreId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<ProjectionDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ProjectionDescriptor> Projections => projections;

    public ProjectionDescriptor? GetById(string projectionId)
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return null;
        }

        return projectionsById.TryGetValue(projectionId.Trim(), out var projection)
            ? projection
            : null;
    }

    public IReadOnlyList<ProjectionDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return projectionsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<ProjectionDescriptor> GetByTargetStore(string targetStoreId)
    {
        if (string.IsNullOrWhiteSpace(targetStoreId))
        {
            return [];
        }

        return projectionsByTargetStore.TryGetValue(targetStoreId.Trim(), out var matches)
            ? matches
            : [];
    }
}
