using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Compatibility;

/// <summary>
/// ABT-003: A <c>process-manager</c> behavior must have inbox deduplication enabled
/// to guarantee exactly-once processing across checkpoints.
/// </summary>
public sealed class Abt003ProcessManagerRequiresInboxRule : IBehaviorCompatibilityRule
{
    /// <summary>
    /// Gets the rule identifier <c>ABT-003</c>.
    /// </summary>
    public string RuleId => "ABT-003";

    /// <summary>
    /// Gets a human-readable description of the rule.
    /// </summary>
    public string Description =>
        "A process-manager behavior must have InboxEnabled=true to guarantee exactly-once processing across checkpoints.";

    /// <summary>
    /// Returns a violation when a <c>process-manager</c> behavior does not have inbox enabled.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to evaluate.</param>
    /// <returns>
    /// A <see cref="BehaviorCompatibilityViolation" /> with <see cref="CompatibilitySeverity.Error" />
    /// when the rule is violated, or <see langword="null" /> when the descriptor is compliant.
    /// </returns>
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "process-manager", StringComparison.OrdinalIgnoreCase))
            return null;

        if (descriptor.InboxEnabled)
            return null;

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Error,
            message: $"Behavior '{descriptor.Id}' is a process-manager but does not have InboxEnabled. " +
                     $"Process managers require inbox deduplication for exactly-once checkpoint guarantees.");
    }
}
