using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

/// <summary>
/// Provides a lookup-friendly view of the active technology runtime surfaces.
/// </summary>
public sealed class TechnologyRuntimeCatalogSnapshot : ITechnologyRuntimeCatalog
{
    private readonly Dictionary<string, TechnologyRuntimeSurface[]> index;

    /// <summary>
    /// Initializes a new instance of the <see cref="TechnologyRuntimeCatalogSnapshot" /> class.
    /// </summary>
    /// <param name="surfaces">The active technology runtime surfaces.</param>
    public TechnologyRuntimeCatalogSnapshot(IReadOnlyList<TechnologyRuntimeSurface> surfaces)
    {
        Surfaces = surfaces ?? throw new ArgumentNullException(nameof(surfaces));
        index = Surfaces
            .GroupBy(static surface => surface.TechnologyId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the active technology runtime surfaces.
    /// </summary>
    public IReadOnlyList<TechnologyRuntimeSurface> Surfaces { get; }

    /// <summary>
    /// Gets the runtime surfaces for a specific technology.
    /// </summary>
    /// <param name="technologyId">The technology identifier to resolve.</param>
    /// <returns>The runtime surfaces registered for the specified technology.</returns>
    public IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(technologyId);

        return index.TryGetValue(technologyId.Trim(), out var surfaces)
            ? surfaces
            : Array.Empty<TechnologyRuntimeSurface>();
    }
}
