namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Collects cell-route descriptors during runtime composition.
/// </summary>
public interface ICellRouteRegistry
{
    /// <summary>
    /// Adds one cell-route descriptor to the active runtime composition.
    /// </summary>
    /// <param name="cellRoute">The cell-route descriptor to add.</param>
    void Add(CellRouteDescriptor cellRoute);
}
