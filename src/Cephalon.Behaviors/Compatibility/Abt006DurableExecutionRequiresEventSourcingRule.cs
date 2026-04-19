using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Compatibility;

/// <summary>
/// ABT-006: A <c>durable-execution</c> behavior must declare event sourcing
/// so the runtime and operator surfaces stay aligned with replay semantics.
/// </summary>
public sealed class Abt006DurableExecutionRequiresEventSourcingRule : IBehaviorCompatibilityRule
{
    /// <summary>Gets the rule identifier <c>ABT-006</c>.</summary>
    public string RuleId => "ABT-006";

    /// <summary>Gets a human-readable description of the rule.</summary>
    public string Description =>
        "A durable-execution behavior must declare EventSourcingEnabled=true so replay semantics stay truthful in runtime metadata.";

    /// <summary>
    /// Returns an error when a <c>durable-execution</c> behavior does not declare event sourcing.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to evaluate.</param>
    /// <returns>
    /// A <see cref="BehaviorCompatibilityViolation" /> with <see cref="CompatibilitySeverity.Error" />
    /// when the rule is violated, or <see langword="null" /> when the descriptor is compliant.
    /// </returns>
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "durable-execution", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (descriptor.EventSourcingEnabled)
        {
            return null;
        }

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Error,
            message: $"Behavior '{descriptor.Id}' uses durable execution but does not declare EventSourcingEnabled. " +
                     "Enable event sourcing so replay semantics stay truthful in runtime metadata and behavior contexts.");
    }
}
