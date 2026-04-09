using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Validation;

/// <summary>
/// Normalizes and validates behavior topology inferred from allowlist attributes and explicit topology declarations.
/// </summary>
internal static class BehaviorAttributeTopologyResolver
{
    private const string RestTransportId = "http.rest";

    internal static BehaviorTopologyDescriptor? Resolve(
        string behaviorId,
        Type behaviorType,
        BehaviorTopologyDescriptor? descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);

        var declaredPatterns = ReadDeclaredPatterns(behaviorType);
        var declaredTransports = ReadDeclaredTransports(behaviorType);
        ThrowIfRestDeclared(behaviorId, declaredTransports, descriptor);

        if (descriptor is null)
        {
            descriptor = BuildDescriptorFromAttributes(behaviorId, declaredPatterns, declaredTransports);
            if (descriptor is null)
            {
                return null;
            }
        }

        BehaviorAllowlistValidator.Validate(descriptor, behaviorType);
        return descriptor;
    }

    private static BehaviorTopologyDescriptor? BuildDescriptorFromAttributes(
        string behaviorId,
        string[] declaredPatterns,
        string[] declaredTransports)
    {
        if (declaredPatterns.Length > 1)
        {
            var declaredPatternList = string.Join(", ", declaredPatterns.Select(static pattern => $"'{pattern}'"));
            throw new BehaviorSecurityException(
                behaviorId,
                $"Behavior '{behaviorId}' declares multiple allowed patterns [{declaredPatternList}] but no explicit topology selected one. " +
                "Add ConfigureTopology(...), runtime config, or reduce the attribute to a single pattern.");
        }

        if (declaredPatterns.Length == 0 && declaredTransports.Length == 0)
        {
            return null;
        }

        var resolvedPattern = declaredPatterns.Length == 1
            ? declaredPatterns[0]
            : "direct";

        return new BehaviorTopologyDescriptor(
            behaviorId,
            resolvedPattern,
            declaredTransports);
    }

    private static string[] ReadDeclaredPatterns(Type behaviorType)
    {
        var attribute = (BehaviorAllowedPatternsAttribute?)Attribute.GetCustomAttribute(
            behaviorType,
            typeof(BehaviorAllowedPatternsAttribute));

        if (attribute?.Patterns.Count is not > 0)
        {
            return [];
        }

        return attribute.Patterns
            .Where(static pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(static pattern => pattern.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] ReadDeclaredTransports(Type behaviorType)
    {
        var attribute = (BehaviorAllowedTransportsAttribute?)Attribute.GetCustomAttribute(
            behaviorType,
            typeof(BehaviorAllowedTransportsAttribute));

        if (attribute?.Transports.Count is not > 0)
        {
            return [];
        }

        return BehaviorTransportIdNormalizer.NormalizeMany(attribute.Transports);
    }

    private static void ThrowIfRestDeclared(
        string behaviorId,
        IReadOnlyList<string> declaredTransports,
        BehaviorTopologyDescriptor? descriptor)
    {
        var annotationDeclaresRest = declaredTransports.Any(static transportId =>
            string.Equals(transportId, RestTransportId, StringComparison.OrdinalIgnoreCase));
        var topologyDeclaresRest = descriptor?.TransportIds.Any(static transportId =>
            string.Equals(transportId, RestTransportId, StringComparison.OrdinalIgnoreCase)) == true;

        if (!annotationDeclaresRest && !topologyDeclaresRest)
        {
            return;
        }

        throw new BehaviorSecurityException(
            behaviorId,
            $"Behavior '{behaviorId}' declares '{RestTransportId}', but Cephalon now keeps REST module-owned only. " +
            "Remove 'http.rest' from behavior annotations/topology and map REST endpoints in a module with MapEndpoints(...) plus MapBehaviorRestGroup(...).");
    }
}
