namespace Cephalon.Abstractions.Health;

/// <summary>
/// Describes the runtime health state of a dependency or probe.
/// </summary>
public enum HealthState
{
    /// <summary>
    /// Indicates the dependency is healthy.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Indicates the dependency is degraded but still available.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Indicates the dependency is unhealthy.
    /// </summary>
    Unhealthy = 2
}
