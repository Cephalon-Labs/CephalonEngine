namespace Cephalon.Engine.Runtime;

/// <summary>
/// Identifies which runtime surface produced one operator-facing lifecycle event.
/// </summary>
public enum RuntimeLifecycleEventScope
{
    /// <summary>
    /// The event belongs to the overall runtime lifecycle.
    /// </summary>
    Runtime = 0,

    /// <summary>
    /// The event belongs to one module lifecycle transition.
    /// </summary>
    Module = 1,

    /// <summary>
    /// The event belongs to package loading and package-origin visibility.
    /// </summary>
    Package = 2
}
