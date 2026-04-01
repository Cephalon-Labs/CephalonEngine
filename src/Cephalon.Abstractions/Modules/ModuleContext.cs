namespace Cephalon.Abstractions.Modules;

public sealed class ModuleContext
{
    public ModuleContext(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceProvider Services { get; }
}
