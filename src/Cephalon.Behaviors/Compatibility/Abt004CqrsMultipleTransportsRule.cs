using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Compatibility;

/// <summary>
/// ABT-004: A CQRS behavior with more than one transport may produce unexpected routing ambiguity.
/// </summary>
public sealed class Abt004CqrsMultipleTransportsRule : IBehaviorCompatibilityRule
{
    /// <summary>
    /// Gets the rule identifier <c>ABT-004</c>.
    /// </summary>
    public string RuleId => "ABT-004";

    /// <summary>
    /// Gets a human-readable description of the rule.
    /// </summary>
    public string Description =>
        "A CQRS behavior with more than one transport may produce routing ambiguity. " +
        "Consider limiting CQRS behaviors to a single primary transport.";

    /// <summary>
    /// Returns an advisory when a <c>cqrs</c> behavior is configured with more than one transport.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to evaluate.</param>
    /// <returns>
    /// A <see cref="BehaviorCompatibilityViolation" /> with <see cref="CompatibilitySeverity.Advisory" />
    /// when the rule is violated, or <see langword="null" /> when the descriptor is compliant.
    /// </returns>
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
            message: $"Behavior '{descriptor.Id}' is cqrs and has {descriptor.TransportIds.Count} transports. " +
                     $"Consider limiting to a single transport to avoid routing ambiguity.");
    }
}
