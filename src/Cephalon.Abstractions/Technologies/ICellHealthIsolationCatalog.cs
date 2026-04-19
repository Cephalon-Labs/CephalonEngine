namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Exposes the cell health-isolation answers visible to the current runtime.
/// </summary>
public interface ICellHealthIsolationCatalog
{
    /// <summary>
    /// Gets all cell health-isolation answers visible to the current runtime.
    /// </summary>
    IReadOnlyList<CellHealthIsolationDescriptor> HealthIsolations { get; }

    /// <summary>
    /// Gets one cell health-isolation answer by its stable identifier.
    /// </summary>
    /// <param name="healthIsolationId">The health-isolation identifier to resolve.</param>
    /// <returns>
    /// The matching cell health-isolation answer, or <see langword="null" /> when it is not active.
    /// </returns>
    CellHealthIsolationDescriptor? GetById(string healthIsolationId);

    /// <summary>
    /// Gets all cell health-isolation answers owned by the requested source module.
    /// </summary>
    /// <param name="sourceModuleId">The source-module identifier to filter by.</param>
    /// <returns>The matching cell health-isolation answers, or an empty list when none are active.</returns>
    IReadOnlyList<CellHealthIsolationDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all cell health-isolation answers that govern the requested cell.
    /// </summary>
    /// <param name="cellId">The cell identifier to filter by.</param>
    /// <returns>The matching cell health-isolation answers, or an empty list when none are active.</returns>
    IReadOnlyList<CellHealthIsolationDescriptor> GetByCellId(string cellId);

    /// <summary>
    /// Gets all cell health-isolation answers that reference the requested dependency.
    /// </summary>
    /// <param name="dependencyId">The dependency identifier to filter by.</param>
    /// <returns>The matching cell health-isolation answers, or an empty list when none are active.</returns>
    IReadOnlyList<CellHealthIsolationDescriptor> GetByDependencyId(string dependencyId);
}
