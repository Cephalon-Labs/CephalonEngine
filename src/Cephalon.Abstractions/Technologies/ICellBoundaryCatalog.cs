namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Exposes the cell boundaries visible to the current runtime.
/// </summary>
public interface ICellBoundaryCatalog
{
    /// <summary>
    /// Gets all cell boundaries visible to the current runtime.
    /// </summary>
    IReadOnlyList<CellBoundaryDescriptor> CellBoundaries { get; }

    /// <summary>
    /// Gets one cell boundary by its stable identifier.
    /// </summary>
    /// <param name="cellId">The cell identifier to resolve.</param>
    /// <returns>The matching cell boundary, or <see langword="null" /> when it is not active.</returns>
    CellBoundaryDescriptor? GetById(string cellId);

    /// <summary>
    /// Gets all cell boundaries that include the requested module.
    /// </summary>
    /// <param name="moduleId">The module identifier to filter by.</param>
    /// <returns>The matching cell boundaries, or an empty list when none are active.</returns>
    IReadOnlyList<CellBoundaryDescriptor> GetByModule(string moduleId);
}
