namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorAuthoringPathConventions
{
    internal static string DeriveRouteGroupPrefixFromBehaviorIdPrefix(string behaviorIdPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);

        var normalizedPrefix = behaviorIdPrefix.Trim();
        var segments = normalizedPrefix
            .Split('.', StringSplitOptions.None | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || segments.Any(static segment => string.IsNullOrWhiteSpace(segment)))
        {
            throw new ArgumentException(
                $"Behavior id prefix '{behaviorIdPrefix}' must use non-empty dot-separated segments so Cephalon can derive a deterministic route-group prefix.",
                nameof(behaviorIdPrefix));
        }

        var invalidSegment = segments.FirstOrDefault(static segment =>
            segment.Contains('/') ||
            segment.Contains('{') ||
            segment.Contains('}'));
        if (invalidSegment is not null)
        {
            throw new ArgumentException(
                $"Behavior id prefix '{behaviorIdPrefix}' contains unsupported segment '{invalidSegment}'. Derived route-group prefixes do not allow '/', '{{', or '}}'.",
                nameof(behaviorIdPrefix));
        }

        return "/" + string.Join("/", segments);
    }
}
