namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Exposes the effective cell traffic-automation answers visible to the current runtime.
/// </summary>
public interface ICellTrafficAutomationRuntimeCatalog
{
    /// <summary>
    /// Gets all effective cell traffic-automation answers visible to the current runtime.
    /// </summary>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> Automations { get; }

    /// <summary>
    /// Gets one effective cell traffic-automation answer by its stable identifier.
    /// </summary>
    /// <param name="automationId">The traffic-automation identifier to resolve.</param>
    /// <returns>The matching runtime descriptor, or <see langword="null" /> when it is not active.</returns>
    CellTrafficAutomationRuntimeDescriptor? GetById(string automationId);

    /// <summary>
    /// Gets one effective cell traffic-automation answer by its governed route identifier.
    /// </summary>
    /// <param name="routeId">The governed route identifier to resolve.</param>
    /// <returns>The matching runtime descriptor, or <see langword="null" /> when it is not active.</returns>
    CellTrafficAutomationRuntimeDescriptor? GetByRouteId(string routeId);

    /// <summary>
    /// Gets all effective cell traffic-automation answers owned by the requested module.
    /// </summary>
    /// <param name="sourceModuleId">The module identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all effective cell traffic-automation answers that originate from the requested source cell.
    /// </summary>
    /// <param name="sourceCellId">The source-cell identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceCellId(string sourceCellId);

    /// <summary>
    /// Gets all effective cell traffic-automation answers that target the requested cell.
    /// </summary>
    /// <param name="targetCellId">The target-cell identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByTargetCellId(string targetCellId);

    /// <summary>
    /// Gets all effective cell traffic-automation answers that target the requested external provider.
    /// </summary>
    /// <param name="provider">The provider identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByProvider(string provider);

    /// <summary>
    /// Gets all effective cell traffic-automation answers that target the requested edge node.
    /// </summary>
    /// <param name="edgeNodeId">The edge-node identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId);

    /// <summary>
    /// Gets all effective cell traffic-automation answers that reference the requested health-isolation identifier.
    /// </summary>
    /// <param name="healthIsolationId">The health-isolation identifier to filter by.</param>
    /// <returns>The matching runtime descriptors, or an empty list when none are active.</returns>
    IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByHealthIsolationId(string healthIsolationId);
}
