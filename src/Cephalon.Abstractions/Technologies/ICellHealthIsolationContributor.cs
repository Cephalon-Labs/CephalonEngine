namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Allows a module to contribute cell health-isolation answers to the active runtime.
/// </summary>
public interface ICellHealthIsolationContributor
{
    /// <summary>
    /// Registers the cell health-isolation answers owned by the contributing module.
    /// </summary>
    /// <param name="healthIsolations">The registry that receives cell health-isolation descriptors.</param>
    void RegisterCellHealthIsolations(ICellHealthIsolationRegistry healthIsolations);
}
