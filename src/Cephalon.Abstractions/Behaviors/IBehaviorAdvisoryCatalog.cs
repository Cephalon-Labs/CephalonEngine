namespace Cephalon.Abstractions.Behaviors;

/// <summary>Provides read access to all active behavior advisories.</summary>
public interface IBehaviorAdvisoryCatalog
{
    /// <summary>Gets all active advisories.</summary>
    IReadOnlyList<IBehaviorAdvisory> All { get; }

    /// <summary>Gets advisories for a specific behavior identifier.</summary>
    IReadOnlyList<IBehaviorAdvisory> GetByBehavior(string behaviorId);

    /// <summary>Gets advisories at or above the specified severity.</summary>
    IReadOnlyList<IBehaviorAdvisory> GetBySeverity(BehaviorAdvisorySeverity minimumSeverity);
}
