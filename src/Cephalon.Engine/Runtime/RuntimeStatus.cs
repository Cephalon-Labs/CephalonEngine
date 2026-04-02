namespace Cephalon.Engine.Runtime;

/// <summary>
/// Represents the current lifecycle phase of the runtime.
/// </summary>
public enum RuntimeStatus
{
    /// <summary>
    /// The runtime has been created but not initialized.
    /// </summary>
    Created = 0,

    /// <summary>
    /// The runtime is initializing modules.
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// The runtime finished initialization but has not started.
    /// </summary>
    Initialized = 2,

    /// <summary>
    /// The runtime is starting modules.
    /// </summary>
    Starting = 3,

    /// <summary>
    /// The runtime is fully started.
    /// </summary>
    Started = 4,

    /// <summary>
    /// The runtime is stopping started modules.
    /// </summary>
    Stopping = 5,

    /// <summary>
    /// The runtime is stopped.
    /// </summary>
    Stopped = 6,

    /// <summary>
    /// The runtime captured a lifecycle failure.
    /// </summary>
    Failed = 7
}
