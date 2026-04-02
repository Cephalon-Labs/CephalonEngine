namespace Cephalon.Engine.Runtime;

/// <summary>
/// Represents the overall health state of the runtime.
/// </summary>
public enum RuntimeHealthState
{
    /// <summary>
    /// The runtime and its dependencies are healthy.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// The runtime is available, but one or more dependencies need attention.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The runtime is not healthy enough to serve traffic.
    /// </summary>
    Unhealthy = 2
}
