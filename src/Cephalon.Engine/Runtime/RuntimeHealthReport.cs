using Cephalon.Abstractions.Health;

namespace Cephalon.Engine.Runtime;

public sealed record RuntimeHealthReport(
    string Probe,
    RuntimeHealthState State,
    string Description,
    RuntimeStatus RuntimeStatus,
    int RestartCount,
    RuntimeFailureInfo? LastFailure,
    IReadOnlyList<DependencyHealthReport> Dependencies)
{
    public bool IsHealthy => State == RuntimeHealthState.Healthy;
}
