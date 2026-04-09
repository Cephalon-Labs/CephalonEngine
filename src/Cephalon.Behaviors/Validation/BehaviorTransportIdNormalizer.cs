namespace Cephalon.Behaviors.Validation;

internal static class BehaviorTransportIdNormalizer
{
    internal static string Normalize(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var normalized = transportId.Trim();
        return normalized.Equals("http.grpc", StringComparison.OrdinalIgnoreCase)
            ? "grpc"
            : normalized;
    }

    internal static string[] NormalizeMany(IReadOnlyList<string> transportIds)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        return transportIds
            .Where(static transportId => !string.IsNullOrWhiteSpace(transportId))
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static transportId => transportId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
