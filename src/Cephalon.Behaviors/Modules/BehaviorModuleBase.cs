using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Behaviors.Modules;

/// <summary>
/// Base class for modules that explicitly own Cephalon behaviors.
/// </summary>
/// <remarks>
/// This base class keeps behavior ownership host-agnostic. Derived modules can still remain
/// process-only, or they can layer one or more public transport adapters on top separately.
/// </remarks>
public abstract class BehaviorModuleBase : ModuleBase, IBehaviorOwnerModule
{
    /// <summary>
    /// Registers the behaviors owned by the current module.
    /// </summary>
    /// <param name="behaviors">The module-owned behavior builder.</param>
    public abstract void ConfigureBehaviors(IBehaviorModuleBuilder behaviors);
}
