using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Rules;

/// <summary>ABT-001: A saga-step behavior must use at least one stateful transport (rabbitmq, kafka, or in-memory).</summary>
public sealed class Abt001SagaRequiresStatefulTransportRule : IBehaviorCompatibilityRule
{
    private static readonly HashSet<string> StatefulTransports = new(StringComparer.OrdinalIgnoreCase)
    {
        "rabbitmq",
        "kafka",
        "in-memory",
    };

    /// <inheritdoc />
    public string RuleId => "ABT-001";

    /// <inheritdoc />
    public string Description => "A saga-step behavior must be wired to at least one stateful transport (rabbitmq, kafka, or in-memory).";

    /// <inheritdoc />
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "saga-step", StringComparison.OrdinalIgnoreCase))
            return null;

        var hasStateful = descriptor.TransportIds.Any(t => StatefulTransports.Contains(t));
        if (hasStateful)
            return null;

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Error,
            message: $"Behavior '{descriptor.Id}' is a saga-step but has no stateful transport (rabbitmq, kafka, in-memory). Saga compensation requires a reliable message bus.");
    }
}
