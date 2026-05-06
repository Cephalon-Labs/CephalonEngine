using System.Reflection;

namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Describes one compile-time discovered Cephalon module and the factory that creates it.
/// </summary>
/// <remarks>
/// Source generators register these descriptors so runtime discovery can remain deterministic
/// without scanning assembly types or invoking module constructors reflectively.
/// </remarks>
public sealed class ModuleDiscoveryDescriptor
{
    private readonly Func<IModule> moduleFactory;

    /// <summary>
    /// Creates a module discovery descriptor.
    /// </summary>
    /// <param name="moduleType">The concrete module type represented by the descriptor.</param>
    /// <param name="moduleFactory">The closed factory that creates module instances.</param>
    public ModuleDiscoveryDescriptor(Type moduleType, Func<IModule> moduleFactory)
    {
        ArgumentNullException.ThrowIfNull(moduleType);
        ArgumentNullException.ThrowIfNull(moduleFactory);

        if (!typeof(IModule).IsAssignableFrom(moduleType))
        {
            throw new ArgumentException(
                $"Module discovery type '{moduleType.FullName}' must implement {typeof(IModule).FullName}.",
                nameof(moduleType));
        }

        ModuleType = moduleType;
        this.moduleFactory = moduleFactory;
    }

    /// <summary>
    /// Gets the concrete module type represented by the descriptor.
    /// </summary>
    public Type ModuleType { get; }

    /// <summary>
    /// Gets the assembly that contains the module type.
    /// </summary>
    public Assembly Assembly => ModuleType.Assembly;

    /// <summary>
    /// Creates a module instance using the generated or explicitly registered factory.
    /// </summary>
    /// <returns>The module instance.</returns>
    public IModule CreateModule()
    {
        var module = moduleFactory()
            ?? throw new InvalidOperationException(
                $"Module discovery factory for '{ModuleType.FullName}' returned null.");

        if (!ModuleType.IsInstanceOfType(module))
        {
            throw new InvalidOperationException(
                $"Module discovery factory for '{ModuleType.FullName}' returned '{module.GetType().FullName}'.");
        }

        return module;
    }
}
