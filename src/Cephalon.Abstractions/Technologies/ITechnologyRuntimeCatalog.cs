namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Exposes the merged runtime surfaces projected by active technology packs.
/// </summary>
public interface ITechnologyRuntimeCatalog
{
    /// <summary>
    /// Gets all active technology runtime surfaces visible to the current runtime.
    /// </summary>
    IReadOnlyList<TechnologyRuntimeSurface> Surfaces { get; }

    /// <summary>
    /// Gets the runtime surfaces associated with a specific technology identifier.
    /// </summary>
    /// <param name="technologyId">The technology identifier to filter by.</param>
    /// <returns>The matching runtime surfaces, or an empty collection when none are active.</returns>
    IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId);
}
