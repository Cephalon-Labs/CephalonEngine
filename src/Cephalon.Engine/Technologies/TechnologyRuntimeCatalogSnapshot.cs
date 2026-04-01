using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

public sealed class TechnologyRuntimeCatalogSnapshot : ITechnologyRuntimeCatalog
{
    private readonly Dictionary<string, TechnologyRuntimeSurface[]> index;

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

    public IReadOnlyList<TechnologyRuntimeSurface> Surfaces { get; }

    public IReadOnlyList<TechnologyRuntimeSurface> GetByTechnology(string technologyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(technologyId);

        return index.TryGetValue(technologyId.Trim(), out var surfaces)
            ? surfaces
            : Array.Empty<TechnologyRuntimeSurface>();
    }
}
