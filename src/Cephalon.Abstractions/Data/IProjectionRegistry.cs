namespace Cephalon.Abstractions.Data;

/// <summary>
/// Receives projection descriptors contributed by active modules or packages.
/// </summary>
public interface IProjectionRegistry
{
    /// <summary>
    /// Adds a projection to the current runtime composition.
    /// </summary>
    /// <param name="projection">The projection descriptor to register.</param>
    void Add(ProjectionDescriptor projection);
}
