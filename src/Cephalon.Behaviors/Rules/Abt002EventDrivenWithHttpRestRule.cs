using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Rules;

/// <summary>ABT-002: An event-driven behavior using http.rest may produce unexpected synchronous semantics.</summary>
public sealed class Abt002EventDrivenWithHttpRestRule : IBehaviorCompatibilityRule
{
    /// <inheritdoc />
    public string RuleId => "ABT-002";

    /// <inheritdoc />
    public string Description => "Event-driven behaviors bound to http.rest may produce unexpected synchronous call semantics. Prefer a message-bus transport for event-driven patterns.";

    /// <inheritdoc />
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "event-driven", StringComparison.OrdinalIgnoreCase))
            return null;

        var hasHttpRest = descriptor.TransportIds.Any(
            static t => string.Equals(t, "http.rest", StringComparison.OrdinalIgnoreCase));

        if (!hasHttpRest)
            return null;

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Warning,
            message: $"Behavior '{descriptor.Id}' is event-driven and uses http.rest. This may produce synchronous semantics that conflict with fire-and-forget intent.");
    }
}
