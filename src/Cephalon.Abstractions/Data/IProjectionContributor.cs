namespace Cephalon.Abstractions.Data;

/// <summary>
/// Contributes one or more projection descriptors to the active runtime.
/// </summary>
public interface IProjectionContributor
{
    /// <summary>
    /// Registers one or more projection descriptors with the supplied registry.
    /// </summary>
    /// <param name="projections">The registry that collects contributed projection descriptors.</param>
    void RegisterProjections(IProjectionRegistry projections);
}
