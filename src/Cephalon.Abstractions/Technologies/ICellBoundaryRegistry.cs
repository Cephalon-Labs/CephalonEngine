namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Collects cell-boundary descriptors during runtime composition.
/// </summary>
public interface ICellBoundaryRegistry
{
    /// <summary>
    /// Adds one cell-boundary descriptor to the active runtime composition.
    /// </summary>
    /// <param name="cellBoundary">The cell-boundary descriptor to add.</param>
    void Add(CellBoundaryDescriptor cellBoundary);
}
