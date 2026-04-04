using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

/// <summary>
/// Provides a lookup-friendly view of the active technology runtime surfaces.
/// </summary>
public sealed class TechnologyRuntimeCatalogSnapshot : ITechnologyRuntimeCatalog
{
    private readonly ITechnologyRuntimeContributor[]? contributors;
    private readonly IReadOnlyList<TechnologyRuntimeSurface>? frozenSurfaces;

    /// <summary>
    /// Initializes a new instance of the <see cref="TechnologyRuntimeCatalogSnapshot" /> class.
    /// </summary>
    /// <param name="surfaces">The active technology runtime surfaces.</param>
    public TechnologyRuntimeCatalogSnapshot(IReadOnlyList<TechnologyRuntimeSurface> surfaces)
    {
        frozenSurfaces = surfaces ?? throw new ArgumentNullException(nameof(surfaces));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TechnologyRuntimeCatalogSnapshot" /> class from runtime contributors.
    /// </summary>
    /// <param name="contributors">The technology runtime contributors that should be projected on demand.</param>
    public TechnologyRuntimeCatalogSnapshot(IEnumerable<ITechnologyRuntimeContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        this.contributors = contributors.ToArray();
    }

    /// <summary>
    /// Gets the active technology runtime surfaces.
    /// </summary>
    public IReadOnlyList<TechnologyRuntimeSurface> Surfaces => frozenSurfaces ?? BuildSurfaces();

    /// <summary>
    /// Gets the runtime surfaces for a specific technology.
    /// </summary>
    /// <param name="technologyId">The technology identifier to resolve.</param>
    /// <returns>The runtime surfaces registered for the specified technology.</returns>
    public IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(technologyId);

        return Surfaces
            .Where(surface => string.Equals(surface.TechnologyId, technologyId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private TechnologyRuntimeSurface[] BuildSurfaces()
    {
        return contributors?
            .Select(static contributor => contributor.DescribeRuntimeSurface())
            .OrderBy(static surface => surface.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
