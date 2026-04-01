namespace Cephalon.Abstractions.Health;

public sealed record DependencyHealthReport(
    string Id,
    string DisplayName,
    HealthState State,
    string Description,
    bool Required,
    string Source);
