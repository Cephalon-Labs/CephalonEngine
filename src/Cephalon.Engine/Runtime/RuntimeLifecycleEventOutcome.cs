namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes whether one lifecycle event completed successfully or failed.
/// </summary>
public enum RuntimeLifecycleEventOutcome
{
    /// <summary>
    /// The lifecycle event completed successfully.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// The lifecycle event failed.
    /// </summary>
    Failed = 1
}
