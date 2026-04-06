using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Rules;

/// <summary>ABT-003: A process-manager behavior must have inbox deduplication enabled to guarantee exactly-once processing.</summary>
public sealed class Abt003ProcessManagerRequiresInboxRule : IBehaviorCompatibilityRule
{
    /// <inheritdoc />
    public string RuleId => "ABT-003";

    /// <inheritdoc />
    public string Description => "A process-manager behavior must have InboxEnabled=true to guarantee exactly-once processing across checkpoints.";

    /// <inheritdoc />
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
            message: $"Behavior '{descriptor.Id}' is a process-manager but does not have InboxEnabled. Process managers require inbox deduplication for exactly-once checkpoint guarantees.");
    }
}
