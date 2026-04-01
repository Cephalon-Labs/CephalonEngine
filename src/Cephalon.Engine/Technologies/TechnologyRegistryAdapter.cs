using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.AppModel;

namespace Cephalon.Engine.Technologies;

internal sealed class TechnologyRegistryAdapter : ITechnologyRegistry
{
    private readonly AppProfileBuilder builder;

    public TechnologyRegistryAdapter(AppProfileBuilder builder)
    {
        this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public void Add(TechnologyDescriptor technology)
    {
        builder.RegisterTechnology(technology);
    }
}
