using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointOverrideActionKind" />.
/// </summary>
public static class RestEndpointOverrideActionKindExtensions
{
    private static readonly Dictionary<RestEndpointOverrideActionKind, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointOverrideActionKind> ActionKindsByWireName = CreateActionKindsByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the override action kind.
    /// </summary>
    /// <param name="actionKind">The override action kind.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointOverrideActionKind actionKind)
    {
        if (WireNames.TryGetValue(actionKind, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(actionKind),
            actionKind,
            "A supported REST endpoint override action kind is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into an override action kind.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="actionKind">The parsed override action kind when the wire name is recognized.</param>
    /// <returns>
    /// <see langword="true" /> when the wire name maps to a supported override action kind;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryParseWireName(string? value, out RestEndpointOverrideActionKind actionKind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            actionKind = default;
            return false;
        }

        return ActionKindsByWireName.TryGetValue(value.Trim(), out actionKind);
    }

    private static Dictionary<RestEndpointOverrideActionKind, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointOverrideActionKind, string>();
        foreach (var value in Enum.GetValues<RestEndpointOverrideActionKind>())
        {
            var field = typeof(RestEndpointOverrideActionKind).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointOverrideActionKind> CreateActionKindsByWireName(
        IReadOnlyDictionary<RestEndpointOverrideActionKind, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointOverrideActionKind>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
