using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Aggregates advisories from all registered <see cref="IBehaviorAdvisoryContributor"/> implementations.
/// </summary>
public sealed class BehaviorAdvisoryCatalog : IBehaviorAdvisoryCatalog
{
    private readonly IReadOnlyList<IBehaviorAdvisory> _all;

    /// <summary>Initializes the catalog from the provided contributors.</summary>
    public BehaviorAdvisoryCatalog(IEnumerable<IBehaviorAdvisoryContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        _all = contributors.SelectMany(c => c.Contribute()).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IBehaviorAdvisory> All => _all;

    /// <inheritdoc />
    public IReadOnlyList<IBehaviorAdvisory> GetByBehavior(string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        return _all.Where(a => string.Equals(a.BehaviorId, behaviorId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IBehaviorAdvisory> GetBySeverity(BehaviorAdvisorySeverity minimumSeverity)
    {
        return _all.Where(a => a.Severity >= minimumSeverity).ToList();
    }
}
