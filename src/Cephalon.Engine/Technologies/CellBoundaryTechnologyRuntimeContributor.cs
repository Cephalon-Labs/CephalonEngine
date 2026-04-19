using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellBoundaryTechnologyRuntimeContributor(
    TechnologySelection technologySelection,
    ICellBoundaryCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: BuiltInTechnologies.CellBasedArchitecture.Id,
            surfaceId: "cell-boundaries",
            displayName: "Cell Boundaries",
            description: "Module-owned cell boundaries visible to the active runtime.",
            entries: technologySelection.IsSelected(BuiltInTechnologies.CellBasedArchitecture.Id)
                ? catalog.CellBoundaries.Select(CreateEntry).ToArray()
                : []);
    }

    private static TechnologyRuntimeEntry CreateEntry(CellBoundaryDescriptor cellBoundary)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = cellBoundary.SourceModuleId,
            ["blastRadius"] = cellBoundary.BlastRadius,
            ["routingStrategy"] = cellBoundary.RoutingStrategy,
            ["moduleCount"] = cellBoundary.ModuleIds.Count.ToString(CultureInfo.InvariantCulture),
            ["moduleIds"] = string.Join(",", cellBoundary.ModuleIds)
        };

        foreach (var pair in cellBoundary.Metadata)
        {
            metadata.TryAdd(pair.Key, pair.Value);
        }

        return new TechnologyRuntimeEntry(
            id: cellBoundary.Id,
            displayName: cellBoundary.DisplayName,
            description: cellBoundary.Description,
            metadata: metadata);
    }
}
