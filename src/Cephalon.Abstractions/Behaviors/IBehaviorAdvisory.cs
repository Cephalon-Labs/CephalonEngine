namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Represents a runtime advisory that describes a recommendation or observation about behavior topology.
/// Advisories are informational — they do not block dispatch.
/// </summary>
public interface IBehaviorAdvisory
{
    /// <summary>Gets the stable advisory identifier.</summary>
    string Id { get; }

    /// <summary>Gets the display name shown in runtime surfaces.</summary>
    string DisplayName { get; }

    /// <summary>Gets the advisory description.</summary>
    string Description { get; }

    /// <summary>Gets the severity of this advisory.</summary>
    BehaviorAdvisorySeverity Severity { get; }

    /// <summary>Gets the behavior identifier this advisory applies to, or <see langword="null"/> if global.</summary>
    string? BehaviorId { get; }
}
