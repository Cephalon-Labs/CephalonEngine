using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellTrafficAutomationTechnologyRuntimeContributor(
    TechnologySelection technologySelection,
    ICellTrafficAutomationRuntimeCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: BuiltInTechnologies.CellBasedArchitecture.Id,
            surfaceId: "cell-traffic-automations",
            displayName: "Cell Traffic Automations",
            description: "Configuration-driven cell traffic-automation answers visible to the active runtime.",
            entries: technologySelection.IsSelected(BuiltInTechnologies.CellBasedArchitecture.Id)
                ? catalog.Automations.Select(CreateEntry).ToArray()
                : []);
    }

    private static TechnologyRuntimeEntry CreateEntry(CellTrafficAutomationRuntimeDescriptor automation)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["routeId"] = automation.RouteId,
            ["sourceModuleId"] = automation.SourceModuleId,
            ["sourceCellId"] = automation.SourceCellId,
            ["targetCellId"] = automation.TargetCellId,
            ["routingStrategy"] = automation.RoutingStrategy,
            ["governanceMode"] = automation.GovernanceMode,
            ["automationMode"] = automation.AutomationMode,
            ["triggerMode"] = automation.TriggerMode,
            ["actionMode"] = automation.ActionMode,
            ["materializationMode"] = automation.MaterializationMode,
            ["policySource"] = automation.PolicySource,
            ["sourceHealthIsolationCount"] = automation.SourceHealthIsolationIds.Count.ToString(CultureInfo.InvariantCulture),
            ["targetHealthIsolationCount"] = automation.TargetHealthIsolationIds.Count.ToString(CultureInfo.InvariantCulture),
            ["dependencyCount"] = automation.DependencyIds.Count.ToString(CultureInfo.InvariantCulture),
            ["sourceHealthIsolationIds"] = string.Join(",", automation.SourceHealthIsolationIds),
            ["targetHealthIsolationIds"] = string.Join(",", automation.TargetHealthIsolationIds),
            ["dependencyIds"] = string.Join(",", automation.DependencyIds),
            ["transportIds"] = string.Join(",", automation.TransportIds),
            ["edgeNodeCount"] = automation.EdgeNodeIds.Count.ToString(CultureInfo.InvariantCulture),
            ["edgeNodeIds"] = string.Join(",", automation.EdgeNodeIds)
        };

        if (!string.IsNullOrWhiteSpace(automation.RequiredCapabilityKey))
        {
            metadata["requiredCapabilityKey"] = automation.RequiredCapabilityKey!;
        }

        if (!string.IsNullOrWhiteSpace(automation.ProviderId))
        {
            metadata["providerId"] = automation.ProviderId!;
        }

        foreach (var pair in automation.Metadata)
        {
            metadata.TryAdd(pair.Key, pair.Value);
        }

        foreach (var pair in automation.RuntimeMetadata)
        {
            metadata.TryAdd(pair.Key, pair.Value);
        }

        return new TechnologyRuntimeEntry(
            id: automation.Id,
            displayName: automation.DisplayName,
            description: automation.Description,
            metadata: metadata);
    }
}
