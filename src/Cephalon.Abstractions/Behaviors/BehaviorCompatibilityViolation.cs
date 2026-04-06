namespace Cephalon.Abstractions.Behaviors;

/// <summary>Represents a compatibility rule violation for a behavior topology.</summary>
public sealed class BehaviorCompatibilityViolation
{
    /// <summary>Initializes a new instance of <see cref="BehaviorCompatibilityViolation"/>.</summary>
    public BehaviorCompatibilityViolation(string ruleId, string behaviorId, CompatibilitySeverity severity, string message)
    {
        RuleId = ruleId;
        BehaviorId = behaviorId;
        Severity = severity;
        Message = message;
    }

    /// <summary>Gets the rule identifier that was violated.</summary>
    public string RuleId { get; }

    /// <summary>Gets the behavior identifier that triggered the violation.</summary>
    public string BehaviorId { get; }

    /// <summary>Gets the violation severity.</summary>
    public CompatibilitySeverity Severity { get; }

    /// <summary>Gets the violation message.</summary>
    public string Message { get; }
}

/// <summary>Severity level of a behavior compatibility rule violation.</summary>
public enum CompatibilitySeverity
{
    /// <summary>The violation is informational only.</summary>
    Advisory,
    /// <summary>The violation may cause runtime issues but does not prevent startup.</summary>
    Warning,
    /// <summary>The violation prevents application startup.</summary>
    Error,
}
