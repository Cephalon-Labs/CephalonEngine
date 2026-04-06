using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Compatibility;

/// <summary>
/// ABT-001: A <c>saga-step</c> behavior must be exposed through at least one stateful transport
/// (<c>rabbitmq</c>, <c>kafka</c>, or <c>in-memory</c>).
/// Stateful transports provide the durable messaging guarantees required for saga coordination.
/// </summary>
public sealed class Abt001SagaStepStatefulTransportRule : IBehaviorCompatibilityRule
{
    private static readonly HashSet<string> StatefulTransports =
        new(StringComparer.OrdinalIgnoreCase) { "rabbitmq", "kafka", "in-memory" };

    /// <summary>
    /// Gets the rule identifier <c>ABT-001</c>.
    /// </summary>
    public string RuleId => "ABT-001";

    /// <summary>
    /// Gets a human-readable description of the rule.
    /// </summary>
    public string Description =>
        "A saga-step behavior must be wired to at least one stateful transport (rabbitmq, kafka, or in-memory).";

    /// <summary>
    /// Returns a violation when a <c>saga-step</c> behavior has no stateful transport assigned.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to evaluate.</param>
    /// <returns>
    /// A <see cref="BehaviorCompatibilityViolation" /> with <see cref="CompatibilitySeverity.Error" />
    /// when the rule is violated, or <see langword="null" /> when the descriptor is compliant.
    /// </returns>
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "saga-step", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var hasStatefulTransport = descriptor.TransportIds
            .Any(t => StatefulTransports.Contains(t));

        if (hasStatefulTransport)
        {
            return null;
        }

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Error,
            message: $"Behavior '{descriptor.Id}' is a saga-step but has no stateful transport " +
                     $"(rabbitmq, kafka, in-memory). Saga compensation requires a reliable message bus.");
    }
}
