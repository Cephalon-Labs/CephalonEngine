namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Allows a module to contribute cell routes to the active runtime.
/// </summary>
public interface ICellRouteContributor
{
    /// <summary>
    /// Registers the cell routes owned by the contributing module.
    /// </summary>
    /// <param name="routes">The registry that receives cell-route descriptors.</param>
    void RegisterCellRoutes(ICellRouteRegistry routes);
}
