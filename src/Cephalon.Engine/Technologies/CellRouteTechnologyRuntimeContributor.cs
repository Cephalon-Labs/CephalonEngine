using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellRouteTechnologyRuntimeContributor(
    TechnologySelection technologySelection,
    ICellRouteCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: BuiltInTechnologies.CellBasedArchitecture.Id,
            surfaceId: "cell-routes",
            displayName: "Cell Routes",
            description: "Module-owned cell-to-cell routing and governance answers visible to the active runtime.",
            entries: technologySelection.IsSelected(BuiltInTechnologies.CellBasedArchitecture.Id)
                ? catalog.Routes.Select(CreateEntry).ToArray()
                : []);
    }

    private static TechnologyRuntimeEntry CreateEntry(CellRouteDescriptor cellRoute)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = cellRoute.SourceModuleId,
            ["sourceCellId"] = cellRoute.SourceCellId,
            ["targetCellId"] = cellRoute.TargetCellId,
            ["routingStrategy"] = cellRoute.RoutingStrategy,
            ["governanceMode"] = cellRoute.GovernanceMode,
            ["transportCount"] = cellRoute.TransportIds.Count.ToString(CultureInfo.InvariantCulture),
            ["transportIds"] = string.Join(",", cellRoute.TransportIds)
        };

        if (!string.IsNullOrWhiteSpace(cellRoute.RequiredCapabilityKey))
        {
            metadata["requiredCapabilityKey"] = cellRoute.RequiredCapabilityKey;
        }

        foreach (var pair in cellRoute.Metadata)
        {
            metadata.TryAdd(pair.Key, pair.Value);
        }

        return new TechnologyRuntimeEntry(
            id: cellRoute.Id,
            displayName: cellRoute.DisplayName,
            description: cellRoute.Description,
            metadata: metadata);
    }
}
