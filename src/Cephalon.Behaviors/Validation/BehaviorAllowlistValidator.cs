using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Validation;

/// <summary>
/// Validates that a behavior's resolved interaction pattern is permitted by its
/// <see cref="BehaviorAllowedPatternsAttribute" />, when one is present.
/// When the attribute is absent, no restriction is applied.
/// </summary>
public sealed class BehaviorAllowlistValidator
{
    /// <summary>
    /// Validates the resolved topology against the allowlist declared on the behavior type.
    /// </summary>
    /// <param name="descriptor">The resolved behavior topology descriptor.</param>
    /// <param name="behaviorType">
    /// The concrete behavior type to inspect for <see cref="BehaviorAllowedPatternsAttribute" />
    /// and <see cref="BehaviorAllowedTransportsAttribute" />.
    /// When <see langword="null" />, no attribute check is performed.
    /// </param>
    /// <exception cref="BehaviorSecurityException">
    /// Thrown when the resolved pattern or a transport is not in the declared allowlist.
    /// </exception>
    public static void Validate(BehaviorTopologyDescriptor descriptor, Type? behaviorType)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (behaviorType is null)
        {
            return;
        }

        ValidatePatternAllowlist(descriptor, behaviorType);
        ValidateTransportAllowlist(descriptor, behaviorType);
    }

    private static void ValidatePatternAllowlist(BehaviorTopologyDescriptor descriptor, Type behaviorType)
    {
        var allowlistAttr = (BehaviorAllowedPatternsAttribute?)
            Attribute.GetCustomAttribute(behaviorType, typeof(BehaviorAllowedPatternsAttribute));

        if (allowlistAttr is null)
        {
            return;
        }

        var allowed = allowlistAttr.Patterns;
        if (allowed.Count == 0)
        {
            return;
        }

        var isAllowed = allowed.Any(p =>
            string.Equals(p, descriptor.Pattern, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            var allowedList = string.Join(", ", allowed.Select(p => $"'{p}'"));
            throw new BehaviorSecurityException(
                descriptor.Id,
                $"Behavior '{descriptor.Id}' resolved to pattern '{descriptor.Pattern}', " +
                $"which is not in its declared allowlist: [{allowedList}].");
        }
    }

    private static void ValidateTransportAllowlist(BehaviorTopologyDescriptor descriptor, Type behaviorType)
    {
        var attr = (BehaviorAllowedTransportsAttribute?)
            Attribute.GetCustomAttribute(behaviorType, typeof(BehaviorAllowedTransportsAttribute));

        if (attr is null)
        {
            return;
        }

        var allowed = new HashSet<string>(
            BehaviorTransportIdNormalizer.NormalizeMany(attr.Transports),
            StringComparer.OrdinalIgnoreCase);
        foreach (var transport in descriptor.TransportIds)
        {
            var normalizedTransport = BehaviorTransportIdNormalizer.Normalize(transport);
            if (!allowed.Contains(normalizedTransport))
            {
                throw new BehaviorSecurityException(
                    descriptor.Id,
                    $"Behavior '{descriptor.Id}' has transport '{transport}' which is not in the allowed " +
                    $"transports list [{string.Join(", ", attr.Transports)}] declared on '{behaviorType.Name}'.");
            }
        }
    }
}
