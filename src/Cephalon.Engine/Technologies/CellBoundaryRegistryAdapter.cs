using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellBoundaryRegistryAdapter(
    string moduleId,
    List<CellBoundaryDescriptor> cellBoundaries) : ICellBoundaryRegistry
{
    public void Add(CellBoundaryDescriptor cellBoundary)
    {
        ArgumentNullException.ThrowIfNull(cellBoundary);

        if (!string.Equals(cellBoundary.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cell boundary '{cellBoundary.Id}' declared source module '{cellBoundary.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        cellBoundaries.Add(cellBoundary);
    }
}
