namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Contributes technology descriptors to the runtime catalog.
/// </summary>
public interface ITechnologyContributor
{
    /// <summary>
    /// Registers one or more technology descriptors.
    /// </summary>
    /// <param name="technologies">The technology registry receiving contributed technologies.</param>
    void RegisterTechnologies(ITechnologyRegistry technologies);
}
