namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorAuthoringPathConventions
{
    internal static string DeriveRouteGroupPrefixFromBehaviorIdPrefix(string behaviorIdPrefix)
    {
        var segments = NormalizeSegments(
            behaviorIdPrefix,
            nameof(behaviorIdPrefix),
            "Behavior id prefix");
        return "/" + string.Join("/", segments);
    }

    internal static string DeriveBehaviorIdPrefixFromBehaviorId(string behaviorId)
    {
        var segments = NormalizeSegments(
            behaviorId,
            nameof(behaviorId),
            "Behavior id");
        if (segments.Length < 2)
        {
            throw new ArgumentException(
                $"Behavior id '{behaviorId}' must use at least two dot-separated segments so Cephalon can derive a deterministic parent behavior-id prefix.",
                nameof(behaviorId));
        }

        return string.Join(".", segments[..^1]);
    }

    private static string[] NormalizeSegments(
        string value,
        string paramName,
        string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalizedValue = value.Trim();
        var segments = normalizedValue
            .Split('.', StringSplitOptions.None | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || segments.Any(static segment => string.IsNullOrWhiteSpace(segment)))
        {
            throw new ArgumentException(
                $"{label} '{value}' must use non-empty dot-separated segments so Cephalon can derive a deterministic route-group prefix.",
                paramName);
        }

        var invalidSegment = segments.FirstOrDefault(static segment =>
            segment.Contains('/') ||
            segment.Contains('{') ||
            segment.Contains('}'));
        if (invalidSegment is not null)
        {
            throw new ArgumentException(
                $"{label} '{value}' contains unsupported segment '{invalidSegment}'. Derived route-group prefixes do not allow '/', '{{', or '}}'.",
                paramName);
        }

        return segments;
    }
}
