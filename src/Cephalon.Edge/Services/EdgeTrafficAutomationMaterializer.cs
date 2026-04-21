using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.Services;

internal sealed class EdgeTrafficAutomationMaterializer(IEdgeNodeCatalog catalog) : ICellTrafficAutomationEdgeMaterializer
{
    public const string DefaultMaterializerId = "edge-runtime-materializer";

    public string MaterializerId => DefaultMaterializerId;

    public bool CanMaterialize(CellTrafficAutomationRuntimeDescriptor automation)
    {
        ArgumentNullException.ThrowIfNull(automation);

        if (automation.EdgeNodeIds.Count == 0 || !UsesEdgeMaterialization(automation.MaterializationMode))
        {
            return false;
        }

        return automation.EdgeNodeIds.All(edgeNodeId => catalog.TryGet(edgeNodeId, out _));
    }

    public ValueTask<CellTrafficAutomationMaterializationResult> MaterializeAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["materializedEdgeNodeIds"] = string.Join(",", automation.EdgeNodeIds),
            ["materializedEdgeNodeCount"] = automation.EdgeNodeIds.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["edgeAction"] = "reconciled"
        };

        return ValueTask.FromResult(new CellTrafficAutomationMaterializationResult(
            state: CellTrafficAutomationMaterializationStates.Applied,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata));
    }

    private static bool UsesEdgeMaterialization(string materializationMode)
    {
        return materializationMode.Trim().ToLowerInvariant() switch
        {
            "edge-managed" => true,
            "provider-and-edge-managed" => true,
            _ => false
        };
    }
}
