namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointAuthoringPolicySuppressionKind"/>.
/// </summary>
public static class RestEndpointAuthoringPolicySuppressionKindExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization and runtime introspection for the suppression kind.
    /// </summary>
    /// <param name="kind">The suppression kind.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointAuthoringPolicySuppressionKind kind)
    {
        return kind switch
        {
            RestEndpointAuthoringPolicySuppressionKind.Unspecified => "Unspecified",
            RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle => "disallowed-authoring-style",
            RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle => "not-allowed-authoring-style",
            RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected => "preferred-authoring-style-selected",
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "A supported REST endpoint authoring-policy suppression kind is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and runtime introspection into a suppression kind.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="kind">The parsed suppression kind when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported suppression kind; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointAuthoringPolicySuppressionKind kind)
    {
        switch (value?.Trim())
        {
            case "Unspecified":
                kind = RestEndpointAuthoringPolicySuppressionKind.Unspecified;
                return true;
            case "disallowed-authoring-style":
                kind = RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle;
                return true;
            case "not-allowed-authoring-style":
                kind = RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle;
                return true;
            case "preferred-authoring-style-selected":
                kind = RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected;
                return true;
            default:
                kind = default;
                return false;
        }
    }
}
