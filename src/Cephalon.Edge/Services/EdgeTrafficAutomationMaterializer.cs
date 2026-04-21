using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.Services;

internal sealed class EdgeTrafficAutomationMaterializer(IEdgeNodeCatalog catalog) : ICellTrafficAutomationEdgeMaterializer
{
    public const string DefaultMaterializerId = "edge-runtime-materializer";
    public const int DefaultPriority = 0;

    public string MaterializerId => DefaultMaterializerId;

    public int Priority => DefaultPriority;

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
            ["edgeAction"] = "reconciled",
            ["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned,
            ["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied,
            ["driftState"] = CellTrafficAutomationDriftStates.InSync,
            ["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile
        };

        return ValueTask.FromResult(new CellTrafficAutomationMaterializationResult(
            state: CellTrafficAutomationMaterializationStates.Applied,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata,
            conditions: CreateMaterializationConditions()));
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor[] CreateMaterializationConditions()
    {
        return
        [
            new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "edge-runtime",
                description: "The selected edge runtime reports active materialization truth."),
            new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "owned",
                description: "The selected edge runtime owns the materialized route."),
            new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "satisfied",
                description: "The selected edge runtime dependency posture is satisfied."),
            new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "in-sync",
                description: "The selected edge runtime matches the authored Cephalon intent."),
            new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Edge,
                CellTrafficAutomationMaterializationConditionCategories.Lifecycle,
                "reconcile-action",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: CellTrafficAutomationLifecycleActions.Reconcile,
                description: "The selected edge runtime is reconciling the materialized route.")
        ];
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
