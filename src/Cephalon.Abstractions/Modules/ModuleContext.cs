namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Provides runtime services shared with module lifecycle hooks.
/// </summary>
public sealed class ModuleContext
{
    /// <summary>
    /// Creates a module runtime context.
    /// </summary>
    /// <param name="services">The root service provider for the runtime.</param>
    public ModuleContext(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Gets the root service provider for the runtime.
    /// </summary>
    public IServiceProvider Services { get; }
}
