using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Behaviors.Http.Abstractions;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="BehaviorRestMethod"/>.
/// </summary>
public static class BehaviorRestMethodExtensions
{
    private static readonly Dictionary<BehaviorRestMethod, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, BehaviorRestMethod> MethodsByWireName = CreateMethodsByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization for the REST method.
    /// </summary>
    /// <param name="method">The REST method.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this BehaviorRestMethod method)
    {
        if (WireNames.TryGetValue(method, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(method),
            method,
            "A supported behavior REST method is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization into a REST method.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="method">The parsed REST method when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported REST method; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out BehaviorRestMethod method)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            method = default;
            return false;
        }

        return MethodsByWireName.TryGetValue(value.Trim(), out method);
    }

    private static Dictionary<BehaviorRestMethod, string> CreateWireNames()
    {
        var result = new Dictionary<BehaviorRestMethod, string>();
        foreach (var value in Enum.GetValues<BehaviorRestMethod>())
        {
            var field = typeof(BehaviorRestMethod).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, BehaviorRestMethod> CreateMethodsByWireName(
        IReadOnlyDictionary<BehaviorRestMethod, string> wireNames)
    {
        var result = new Dictionary<string, BehaviorRestMethod>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
