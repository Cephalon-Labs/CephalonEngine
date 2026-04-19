using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellHealthIsolationTechnologyRuntimeContributor(
    TechnologySelection technologySelection,
    ICellHealthIsolationCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: BuiltInTechnologies.CellBasedArchitecture.Id,
            surfaceId: "cell-health-isolations",
            displayName: "Cell Health Isolations",
            description: "Module-owned cell health-isolation answers visible to the active runtime.",
            entries: technologySelection.IsSelected(BuiltInTechnologies.CellBasedArchitecture.Id)
                ? catalog.HealthIsolations.Select(CreateEntry).ToArray()
                : []);
    }

    private static TechnologyRuntimeEntry CreateEntry(CellHealthIsolationDescriptor healthIsolation)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = healthIsolation.SourceModuleId,
            ["cellId"] = healthIsolation.CellId,
            ["failureIsolationMode"] = healthIsolation.FailureIsolationMode,
            ["readinessScope"] = healthIsolation.ReadinessScope,
            ["restartScope"] = healthIsolation.RestartScope,
            ["dependencyCount"] = healthIsolation.DependencyIds.Count.ToString(CultureInfo.InvariantCulture),
            ["dependencyIds"] = string.Join(",", healthIsolation.DependencyIds)
        };

        foreach (var pair in healthIsolation.Metadata)
        {
            metadata.TryAdd(pair.Key, pair.Value);
        }

        return new TechnologyRuntimeEntry(
            id: healthIsolation.Id,
            displayName: healthIsolation.DisplayName,
            description: healthIsolation.Description,
            metadata: metadata);
    }
}
