using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Exceptions;

/// <summary>Thrown when Error-severity compatibility rule violations are found during startup.</summary>
public sealed class BehaviorCompatibilityException : Exception
{
    /// <summary>Initializes a new instance of <see cref="BehaviorCompatibilityException"/>.</summary>
    public BehaviorCompatibilityException(IReadOnlyList<BehaviorCompatibilityViolation> violations)
        : base(BuildMessage(violations))
    {
        Violations = violations;
    }

    /// <summary>Gets the list of Error-severity violations that caused this exception.</summary>
    public IReadOnlyList<BehaviorCompatibilityViolation> Violations { get; }

    private static string BuildMessage(IReadOnlyList<BehaviorCompatibilityViolation> violations)
    {
        var lines = violations.Select(v => $"  [{v.RuleId}] {v.BehaviorId}: {v.Message}");
        return $"Behavior topology compatibility check failed with {violations.Count} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}
