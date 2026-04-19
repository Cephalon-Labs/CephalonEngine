namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Allows a module to contribute cell boundaries to the active runtime.
/// </summary>
public interface ICellBoundaryContributor
{
    /// <summary>
    /// Registers the cell boundaries owned by the contributing module.
    /// </summary>
    /// <param name="cells">The registry that receives cell-boundary descriptors.</param>
    void RegisterCellBoundaries(ICellBoundaryRegistry cells);
}
