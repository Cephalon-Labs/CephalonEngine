using Cephalon.Abstractions.Health;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Captures the health result for a runtime liveness or readiness probe.
/// </summary>
/// <param name="Probe">The probe name that produced the report.</param>
/// <param name="State">The evaluated runtime health state.</param>
/// <param name="Description">A human-readable description of the evaluated state.</param>
/// <param name="RuntimeStatus">The runtime lifecycle status at the time of evaluation.</param>
/// <param name="RestartCount">The number of completed manual restarts.</param>
/// <param name="LastFailure">The last runtime failure when one is available.</param>
/// <param name="Dependencies">The dependency-health reports visible during evaluation.</param>
public sealed record RuntimeHealthReport(
    string Probe,
    RuntimeHealthState State,
    string Description,
    RuntimeStatus RuntimeStatus,
    int RestartCount,
    RuntimeFailureInfo? LastFailure,
    IReadOnlyList<DependencyHealthReport> Dependencies)
{
    /// <summary>
    /// Gets a value indicating whether the report represents a healthy state.
    /// </summary>
    public bool IsHealthy => State == RuntimeHealthState.Healthy;
}
