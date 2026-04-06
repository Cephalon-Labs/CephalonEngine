namespace Cephalon.Abstractions.Behaviors;

/// <summary>Validates a behavior topology descriptor against a compatibility constraint.</summary>
public interface IBehaviorCompatibilityRule
{
    /// <summary>Gets the unique rule identifier (e.g. "ABT-001").</summary>
    string RuleId { get; }

    /// <summary>Gets a human-readable description of the rule.</summary>
    string Description { get; }

    /// <summary>Checks the descriptor and returns a violation if the rule is violated, or <see langword="null"/> if valid.</summary>
    BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor);
}
