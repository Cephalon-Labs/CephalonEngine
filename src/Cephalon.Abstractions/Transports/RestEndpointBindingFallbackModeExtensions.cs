using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointBindingFallbackMode"/>.
/// </summary>
public static class RestEndpointBindingFallbackModeExtensions
{
    private static readonly Dictionary<RestEndpointBindingFallbackMode, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointBindingFallbackMode> ModesByWireName = CreateModesByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization and compatibility metadata for the fallback mode.
    /// </summary>
    /// <param name="mode">The fallback mode.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointBindingFallbackMode mode)
    {
        if (WireNames.TryGetValue(mode, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(mode),
            mode,
            "A supported REST endpoint binding fallback mode is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and compatibility metadata into a fallback mode.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="mode">The parsed fallback mode when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported fallback mode; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointBindingFallbackMode mode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            mode = default;
            return false;
        }

        return ModesByWireName.TryGetValue(value.Trim(), out mode);
    }

    private static Dictionary<RestEndpointBindingFallbackMode, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointBindingFallbackMode, string>();
        foreach (var value in Enum.GetValues<RestEndpointBindingFallbackMode>())
        {
            var field = typeof(RestEndpointBindingFallbackMode).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointBindingFallbackMode> CreateModesByWireName(
        IReadOnlyDictionary<RestEndpointBindingFallbackMode, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointBindingFallbackMode>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
