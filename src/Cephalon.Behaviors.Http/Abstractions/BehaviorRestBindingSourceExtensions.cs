using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="BehaviorRestBindingSource"/>.
/// </summary>
public static class BehaviorRestBindingSourceExtensions
{
    private static readonly Dictionary<BehaviorRestBindingSource, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, BehaviorRestBindingSource> SourcesByWireName = CreateSourcesByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the binding source.
    /// </summary>
    /// <param name="source">The binding source.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this BehaviorRestBindingSource source)
    {
        if (WireNames.TryGetValue(source, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(source),
            source,
            "A supported behavior REST binding source is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into a binding source.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="source">The parsed binding source when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported binding source; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out BehaviorRestBindingSource source)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            source = default;
            return false;
        }

        return SourcesByWireName.TryGetValue(value.Trim(), out source);
    }

    private static Dictionary<BehaviorRestBindingSource, string> CreateWireNames()
    {
        var result = new Dictionary<BehaviorRestBindingSource, string>();
        foreach (var value in Enum.GetValues<BehaviorRestBindingSource>())
        {
            var field = typeof(BehaviorRestBindingSource).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, BehaviorRestBindingSource> CreateSourcesByWireName(
        IReadOnlyDictionary<BehaviorRestBindingSource, string> wireNames)
    {
        var result = new Dictionary<string, BehaviorRestBindingSource>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
