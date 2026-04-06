using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Rules;

/// <summary>ABT-004: A CQRS behavior with more than one transport may produce unexpected routing ambiguity.</summary>
public sealed class Abt004CqrsMultipleTransportsRule : IBehaviorCompatibilityRule
{
    /// <inheritdoc />
    public string RuleId => "ABT-004";

    /// <inheritdoc />
    public string Description => "A CQRS behavior with more than one transport may produce routing ambiguity. Consider limiting CQRS behaviors to a single primary transport.";

    /// <inheritdoc />
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "cqrs", StringComparison.OrdinalIgnoreCase))
            return null;

        if (descriptor.TransportIds.Count <= 1)
            return null;

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Advisory,
            message: $"Behavior '{descriptor.Id}' is cqrs and has {descriptor.TransportIds.Count} transports. Consider limiting to a single transport to avoid routing ambiguity.");
    }
}
