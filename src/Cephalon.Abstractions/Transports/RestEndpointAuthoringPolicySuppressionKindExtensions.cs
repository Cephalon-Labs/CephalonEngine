using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointAuthoringPolicySuppressionKind"/>.
/// </summary>
public static class RestEndpointAuthoringPolicySuppressionKindExtensions
{
    private static readonly Dictionary<RestEndpointAuthoringPolicySuppressionKind, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointAuthoringPolicySuppressionKind> KindsByWireName = CreateKindsByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization and runtime introspection for the suppression kind.
    /// </summary>
    /// <param name="kind">The suppression kind.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointAuthoringPolicySuppressionKind kind)
    {
        if (WireNames.TryGetValue(kind, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(kind),
            kind,
            "A supported REST endpoint authoring-policy suppression kind is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and runtime introspection into a suppression kind.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="kind">The parsed suppression kind when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported suppression kind; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointAuthoringPolicySuppressionKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            kind = default;
            return false;
        }

        return KindsByWireName.TryGetValue(value.Trim(), out kind);
    }

    private static Dictionary<RestEndpointAuthoringPolicySuppressionKind, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointAuthoringPolicySuppressionKind, string>();
        foreach (var value in Enum.GetValues<RestEndpointAuthoringPolicySuppressionKind>())
        {
            var field = typeof(RestEndpointAuthoringPolicySuppressionKind).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointAuthoringPolicySuppressionKind> CreateKindsByWireName(
        IReadOnlyDictionary<RestEndpointAuthoringPolicySuppressionKind, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointAuthoringPolicySuppressionKind>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
