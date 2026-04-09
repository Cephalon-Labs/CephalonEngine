using Cephalon.Abstractions.Modules;

namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Declares that a module explicitly owns one or more Cephalon behaviors.
/// </summary>
/// <remarks>
/// Modules can use this contract to keep behavior ownership deterministic without relying only on
/// assembly scanning. A module may still choose to expose only some of its owned behaviors through
/// a host adapter such as ASP.NET Core REST.
/// </remarks>
public interface IBehaviorOwnerModule : IModule
{
    /// <summary>
    /// Registers the behaviors owned by the current module.
    /// </summary>
    /// <param name="behaviors">The builder that collects module-owned behavior registrations.</param>
    void ConfigureBehaviors(IBehaviorModuleBuilder behaviors);
}
