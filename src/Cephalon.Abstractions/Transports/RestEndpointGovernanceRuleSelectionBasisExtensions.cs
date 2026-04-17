using System.Reflection;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointGovernanceRuleSelectionBasis"/>.
/// </summary>
public static class RestEndpointGovernanceRuleSelectionBasisExtensions
{
    private static readonly Dictionary<RestEndpointGovernanceRuleSelectionBasis, string> WireNames = CreateWireNames();
    private static readonly Dictionary<string, RestEndpointGovernanceRuleSelectionBasis> BasesByWireName = CreateBasesByWireName(WireNames);

    /// <summary>
    /// Gets the stable wire name used by JSON serialization and runtime introspection for the
    /// selection basis.
    /// </summary>
    /// <param name="basis">The selection basis.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointGovernanceRuleSelectionBasis basis)
    {
        if (WireNames.TryGetValue(basis, out var wireName))
        {
            return wireName;
        }

        throw new ArgumentOutOfRangeException(
            nameof(basis),
            basis,
            "A supported REST endpoint governance rule selection basis is required.");
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and runtime introspection into
    /// a selection basis.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="basis">The parsed selection basis when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported selection basis; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointGovernanceRuleSelectionBasis basis)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            basis = default;
            return false;
        }

        return BasesByWireName.TryGetValue(value.Trim(), out basis);
    }

    private static Dictionary<RestEndpointGovernanceRuleSelectionBasis, string> CreateWireNames()
    {
        var result = new Dictionary<RestEndpointGovernanceRuleSelectionBasis, string>();
        foreach (var value in Enum.GetValues<RestEndpointGovernanceRuleSelectionBasis>())
        {
            var field = typeof(RestEndpointGovernanceRuleSelectionBasis).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            ArgumentNullException.ThrowIfNull(field);

            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
            result[value] = string.IsNullOrWhiteSpace(wireName)
                ? field.Name
                : wireName.Trim();
        }

        return result;
    }

    private static Dictionary<string, RestEndpointGovernanceRuleSelectionBasis> CreateBasesByWireName(
        IReadOnlyDictionary<RestEndpointGovernanceRuleSelectionBasis, string> wireNames)
    {
        var result = new Dictionary<string, RestEndpointGovernanceRuleSelectionBasis>(StringComparer.Ordinal);
        foreach (var pair in wireNames)
        {
            result[pair.Value] = pair.Key;
        }

        return result;
    }
}
