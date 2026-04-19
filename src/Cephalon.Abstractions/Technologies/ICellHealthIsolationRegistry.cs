namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Collects cell health-isolation descriptors during runtime composition.
/// </summary>
public interface ICellHealthIsolationRegistry
{
    /// <summary>
    /// Adds one cell health-isolation descriptor to the active runtime composition.
    /// </summary>
    /// <param name="healthIsolation">The cell health-isolation descriptor to add.</param>
    void Add(CellHealthIsolationDescriptor healthIsolation);
}
