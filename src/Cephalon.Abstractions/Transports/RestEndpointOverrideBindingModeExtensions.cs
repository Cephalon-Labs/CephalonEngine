using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointOverrideBindingMode" />.
/// </summary>
public static class RestEndpointOverrideBindingModeExtensions
{
    private static readonly Dictionary<RestEndpointOverrideBindingMode, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointOverrideBindingMode> ModesByWireName = CreateModesByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization and compatibility metadata for the override binding mode.
    /// </summary>
    /// <param name="bindingMode">The override binding mode.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointOverrideBindingMode bindingMode)
    {
        if (WireNames.TryGetValue(bindingMode, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(bindingMode),
            bindingMode,
            "A supported REST endpoint override binding mode is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and compatibility metadata into an override binding mode.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="bindingMode">The parsed override binding mode when the wire name is recognized.</param>
    /// <returns>
    /// <see langword="true" /> when the wire name maps to a supported override binding mode;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryParseWireName(string? value, out RestEndpointOverrideBindingMode bindingMode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            bindingMode = default;
            return false;
        }

        return ModesByWireName.TryGetValue(value.Trim(), out bindingMode);
    }

    private static Dictionary<RestEndpointOverrideBindingMode, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointOverrideBindingMode, string>();
        foreach (var value in Enum.GetValues<RestEndpointOverrideBindingMode>())
        {
            var field = typeof(RestEndpointOverrideBindingMode).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointOverrideBindingMode> CreateModesByWireName(
        IReadOnlyDictionary<RestEndpointOverrideBindingMode, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointOverrideBindingMode>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
