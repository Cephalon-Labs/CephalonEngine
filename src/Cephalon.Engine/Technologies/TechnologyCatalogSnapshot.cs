using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

public sealed class TechnologyCatalogSnapshot
{
    public TechnologyCatalogSnapshot(IReadOnlyList<TechnologyDescriptor> technologies)
    {
        Technologies = technologies ?? throw new ArgumentNullException(nameof(technologies));
    }

    public IReadOnlyList<TechnologyDescriptor> Technologies { get; }
}
