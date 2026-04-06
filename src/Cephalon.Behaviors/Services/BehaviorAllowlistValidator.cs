using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Exceptions;

namespace Cephalon.Behaviors.Services;

/// <summary>Validates resolved topology against <c>[BehaviorAllowedPatterns]</c> and <c>[BehaviorAllowedTransports]</c> attribute allowlists.</summary>
public sealed class BehaviorAllowlistValidator
{
    /// <summary>
    /// Validates the resolved <paramref name="descriptor"/> against the allowlists declared on <paramref name="behaviorType"/>.
    /// Throws <see cref="BehaviorSecurityException"/> if any allowlist is violated.
    /// </summary>
    public static void Validate(Type behaviorType, BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(descriptor);

        ValidatePatternAllowlist(behaviorType, descriptor);
        ValidateTransportAllowlist(behaviorType, descriptor);
    }

    private static void ValidatePatternAllowlist(Type behaviorType, BehaviorTopologyDescriptor descriptor)
    {
        var attr = (BehaviorAllowedPatternsAttribute?)Attribute.GetCustomAttribute(
            behaviorType, typeof(BehaviorAllowedPatternsAttribute));

        if (attr is null)
            return;

        var allowed = new HashSet<string>(attr.Patterns, StringComparer.OrdinalIgnoreCase);
        if (!allowed.Contains(descriptor.Pattern))
        {
            throw new BehaviorSecurityException(
                descriptor.Id,
                $"Behavior '{descriptor.Id}' has pattern '{descriptor.Pattern}' which is not in the allowed patterns list [{string.Join(", ", attr.Patterns)}] declared on '{behaviorType.Name}'.");
        }
    }

    private static void ValidateTransportAllowlist(Type behaviorType, BehaviorTopologyDescriptor descriptor)
    {
        var attr = (BehaviorAllowedTransportsAttribute?)Attribute.GetCustomAttribute(
            behaviorType, typeof(BehaviorAllowedTransportsAttribute));

        if (attr is null)
            return;

        var allowed = new HashSet<string>(attr.Transports, StringComparer.OrdinalIgnoreCase);
        foreach (var transport in descriptor.TransportIds)
        {
            if (!allowed.Contains(transport))
            {
                throw new BehaviorSecurityException(
                    descriptor.Id,
                    $"Behavior '{descriptor.Id}' has transport '{transport}' which is not in the allowed transports list [{string.Join(", ", attr.Transports)}] declared on '{behaviorType.Name}'.");
            }
        }
    }
}
