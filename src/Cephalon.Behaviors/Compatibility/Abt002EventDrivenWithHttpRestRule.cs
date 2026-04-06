using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Compatibility;

/// <summary>
/// ABT-002: An <c>event-driven</c> behavior exposed over <c>http.rest</c> is inadvisable.
/// REST is a synchronous request-response transport and does not align with event-driven semantics.
/// Consider using <c>http.sse</c>, <c>http.graphql-sse</c>, <c>http.graphql-ws</c>,
/// <c>http.ws</c>, or a message-queue transport instead.
/// </summary>
public sealed class Abt002EventDrivenWithHttpRestRule : IBehaviorCompatibilityRule
{
    /// <summary>
    /// Gets the rule identifier <c>ABT-002</c>.
    /// </summary>
    public string RuleId => "ABT-002";

    /// <summary>
    /// Gets a human-readable description of the rule.
    /// </summary>
    public string Description =>
        "An event-driven behavior should not use http.rest (a synchronous transport). " +
        "Prefer http.sse, http.ws, http.graphql-sse, http.graphql-ws, rabbitmq, kafka, or in-memory.";

    /// <summary>
    /// Returns a warning when an <c>event-driven</c> behavior is exposed over <c>http.rest</c>.
    /// </summary>
    /// <param name="descriptor">The behavior topology descriptor to evaluate.</param>
    /// <returns>
    /// A <see cref="BehaviorCompatibilityViolation" /> with <see cref="CompatibilitySeverity.Warning" />
    /// when the rule is violated, or <see langword="null" /> when the descriptor is compliant.
    /// </returns>
    public BehaviorCompatibilityViolation? Check(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!string.Equals(descriptor.Pattern, "event-driven", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var hasHttpRest = descriptor.TransportIds
            .Any(static t => string.Equals(t, "http.rest", StringComparison.OrdinalIgnoreCase));

        if (!hasHttpRest)
        {
            return null;
        }

        return new BehaviorCompatibilityViolation(
            ruleId: RuleId,
            behaviorId: descriptor.Id,
            severity: CompatibilitySeverity.Warning,
            message: $"Behavior '{descriptor.Id}' uses the 'event-driven' pattern with 'http.rest', which is " +
                     $"a synchronous transport. Consider using http.sse, http.ws, http.graphql-sse, " +
                     $"http.graphql-ws, rabbitmq, kafka, or in-memory instead.");
    }
}
