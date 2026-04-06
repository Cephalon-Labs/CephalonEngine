namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Contributes behavior advisories to the active runtime's advisory catalog.
/// Implementations are collected via dependency injection enumeration.
/// </summary>
public interface IBehaviorAdvisoryContributor
{
    /// <summary>Contributes advisories for the current runtime state.</summary>
    /// <returns>The advisories contributed by this instance.</returns>
    IReadOnlyList<IBehaviorAdvisory> Contribute();
}
