namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Registers technology descriptors for the runtime catalog.
/// </summary>
public interface ITechnologyRegistry
{
    /// <summary>
    /// Adds a technology descriptor to the registry.
    /// </summary>
    /// <param name="technology">The technology descriptor to register.</param>
    void Add(TechnologyDescriptor technology);
}
