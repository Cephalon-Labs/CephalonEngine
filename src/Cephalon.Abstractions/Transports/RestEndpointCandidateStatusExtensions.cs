using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointCandidateStatus"/>.
/// </summary>
public static class RestEndpointCandidateStatusExtensions
{
    private static readonly Dictionary<RestEndpointCandidateStatus, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointCandidateStatus> StatusesByWireName = CreateStatusesByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the candidate status.
    /// </summary>
    /// <param name="status">The candidate status.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointCandidateStatus status)
    {
        if (WireNames.TryGetValue(status, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(status),
            status,
            "A supported REST endpoint candidate status is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into a candidate status.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="status">The parsed candidate status when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported candidate status; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointCandidateStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = default;
            return false;
        }

        return StatusesByWireName.TryGetValue(value.Trim(), out status);
    }

    private static Dictionary<RestEndpointCandidateStatus, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointCandidateStatus, string>();
        foreach (var value in Enum.GetValues<RestEndpointCandidateStatus>())
        {
            var field = typeof(RestEndpointCandidateStatus).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointCandidateStatus> CreateStatusesByWireName(
        IReadOnlyDictionary<RestEndpointCandidateStatus, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointCandidateStatus>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
