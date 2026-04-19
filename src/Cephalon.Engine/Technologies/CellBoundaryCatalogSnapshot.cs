using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellBoundaryCatalogSnapshot : ICellBoundaryCatalog
{
    private readonly CellBoundaryDescriptor[] cellBoundaries;
    private readonly Dictionary<string, CellBoundaryDescriptor> cellBoundariesById;
    private readonly Dictionary<string, IReadOnlyList<CellBoundaryDescriptor>> cellBoundariesByModule;

    public CellBoundaryCatalogSnapshot(IEnumerable<CellBoundaryDescriptor> cellBoundaries)
    {
        ArgumentNullException.ThrowIfNull(cellBoundaries);

        var duplicateCell = cellBoundaries
            .GroupBy(static cell => cell.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateCell is not null)
        {
            throw new InvalidOperationException(
                $"Cell boundary '{duplicateCell.Key}' is already registered.");
        }

        this.cellBoundaries = cellBoundaries
            .OrderBy(static cell => cell.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static cell => cell.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static cell => cell.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        cellBoundariesById = this.cellBoundaries.ToDictionary(static cell => cell.Id, StringComparer.OrdinalIgnoreCase);
        cellBoundariesByModule = this.cellBoundaries
            .SelectMany(static cell => cell.ModuleIds.Select(moduleId => new KeyValuePair<string, CellBoundaryDescriptor>(moduleId, cell)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellBoundaryDescriptor>)group
                    .Select(static pair => pair.Value)
                    .OrderBy(static cell => cell.SourceModuleId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static cell => cell.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static cell => cell.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CellBoundaryDescriptor> CellBoundaries => cellBoundaries;

    public CellBoundaryDescriptor? GetById(string cellId)
    {
        if (string.IsNullOrWhiteSpace(cellId))
        {
            return null;
        }

        return cellBoundariesById.TryGetValue(cellId.Trim(), out var cellBoundary)
            ? cellBoundary
            : null;
    }

    public IReadOnlyList<CellBoundaryDescriptor> GetByModule(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            return [];
        }

        return cellBoundariesByModule.TryGetValue(moduleId.Trim(), out var matches)
            ? matches
            : [];
    }
}
