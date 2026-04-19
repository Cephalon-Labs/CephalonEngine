namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Exposes the cell-to-cell routing and governance answers visible to the current runtime.
/// </summary>
public interface ICellRouteCatalog
{
    /// <summary>
    /// Gets all cell routes visible to the current runtime.
    /// </summary>
    IReadOnlyList<CellRouteDescriptor> Routes { get; }

    /// <summary>
    /// Gets one cell route by its stable identifier.
    /// </summary>
    /// <param name="routeId">The cell-route identifier to resolve.</param>
    /// <returns>The matching cell route, or <see langword="null" /> when it is not active.</returns>
    CellRouteDescriptor? GetById(string routeId);

    /// <summary>
    /// Gets all cell routes owned by the requested source module.
    /// </summary>
    /// <param name="sourceModuleId">The source-module identifier to filter by.</param>
    /// <returns>The matching cell routes, or an empty list when none are active.</returns>
    IReadOnlyList<CellRouteDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all cell routes that originate from the requested source cell.
    /// </summary>
    /// <param name="sourceCellId">The source-cell identifier to filter by.</param>
    /// <returns>The matching cell routes, or an empty list when none are active.</returns>
    IReadOnlyList<CellRouteDescriptor> GetBySourceCellId(string sourceCellId);

    /// <summary>
    /// Gets all cell routes that target the requested cell.
    /// </summary>
    /// <param name="targetCellId">The target-cell identifier to filter by.</param>
    /// <returns>The matching cell routes, or an empty list when none are active.</returns>
    IReadOnlyList<CellRouteDescriptor> GetByTargetCellId(string targetCellId);
}
