using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Compatibility;

/// <summary>
/// ABT-005: A <c>saga-choreography</c> behavior should enable outbox staging
/// so choreography publications can survive transient failures and retries safely.
/// </summary>
public sealed class Abt005SagaChoreographyOutboxRule : IBehaviorCompatibilityRule
{
    /// <summary>Gets the rule identifier <c>ABT-005</c>.</summary>
    public string RuleId => "ABT-005";

    /// <summary>Gets a human-readable description of the rule.</summary>
    public string Description =>
        "A saga-choreography behavior should have OutboxEnabled=true so event publications stay durable across retries and restarts.";

    /// <summary>
    /// Returns an advisory when a <c>saga-choreography</c> behavior does not have outbox enabled.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to evaluate.</param>
    /// <returns>
    /// A <see cref="BehaviorCompatibilityViolation" /> with <see cref="CompatibilitySeverity.Advisory" />
    /// when the rule is violated, or <see langword="null" /> when the descriptor is compliant.
    /// </returns>
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "saga-choreography", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (descriptor.OutboxEnabled)
        {
            return null;
        }

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Advisory,
            message: $"Behavior '{descriptor.Id}' is a saga-choreography step but does not have OutboxEnabled. " +
                     "Enable the outbox when choreography publications must survive retries and restarts.");
    }
}
