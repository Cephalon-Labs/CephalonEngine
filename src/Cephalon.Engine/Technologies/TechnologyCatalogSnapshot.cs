using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

/// <summary>
/// Captures the built-in and registered technology catalog visible to the runtime.
/// </summary>
public sealed class TechnologyCatalogSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TechnologyCatalogSnapshot" /> class.
    /// </summary>
    /// <param name="technologies">The technologies visible in the catalog.</param>
    public TechnologyCatalogSnapshot(IReadOnlyList<TechnologyDescriptor> technologies)
    {
        Technologies = technologies ?? throw new ArgumentNullException(nameof(technologies));
    }

    /// <summary>
    /// Gets the technologies visible in the catalog.
    /// </summary>
    public IReadOnlyList<TechnologyDescriptor> Technologies { get; }
}
