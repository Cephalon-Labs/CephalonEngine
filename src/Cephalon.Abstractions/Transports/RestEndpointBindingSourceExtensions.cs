using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointBindingSource"/>.
/// </summary>
public static class RestEndpointBindingSourceExtensions
{
    private static readonly Dictionary<RestEndpointBindingSource, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointBindingSource> SourcesByWireName = CreateSourcesByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization and REST governance config for the binding source.
    /// </summary>
    /// <param name="source">The binding source.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointBindingSource source)
    {
        if (WireNames.TryGetValue(source, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(source),
            source,
            "A supported REST endpoint binding source is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and REST governance config into a binding source.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="source">The parsed binding source when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported binding source; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointBindingSource source)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            source = default;
            return false;
        }

        return SourcesByWireName.TryGetValue(value.Trim(), out source);
    }

    private static Dictionary<RestEndpointBindingSource, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointBindingSource, string>();
        foreach (var value in Enum.GetValues<RestEndpointBindingSource>())
        {
            var field = typeof(RestEndpointBindingSource).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointBindingSource> CreateSourcesByWireName(
        IReadOnlyDictionary<RestEndpointBindingSource, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointBindingSource>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
