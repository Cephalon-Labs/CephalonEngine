using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class ProjectionRegistryAdapter(
    string moduleId,
    List<ProjectionDescriptor> projections) : IProjectionRegistry
{
    public void Add(ProjectionDescriptor projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        if (!string.Equals(projection.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Projection '{projection.Id}' declared source module '{projection.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        projections.Add(projection);
    }
}
