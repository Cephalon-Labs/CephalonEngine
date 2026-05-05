namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointOverrideBindingMode" />.
/// </summary>
public static class RestEndpointOverrideBindingModeExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization and compatibility metadata for the override binding mode.
    /// </summary>
    /// <param name="bindingMode">The override binding mode.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointOverrideBindingMode bindingMode)
    {
        return bindingMode switch
        {
            RestEndpointOverrideBindingMode.Unspecified => "unspecified",
            RestEndpointOverrideBindingMode.ReplaceExplicit => "replace-explicit",
            RestEndpointOverrideBindingMode.MergeExplicit => "merge-explicit",
            _ => throw new ArgumentOutOfRangeException(
                nameof(bindingMode),
                bindingMode,
                "A supported REST endpoint override binding mode is required.")
        };
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
        switch (value?.Trim())
        {
            case "unspecified":
                bindingMode = RestEndpointOverrideBindingMode.Unspecified;
                return true;
            case "replace-explicit":
                bindingMode = RestEndpointOverrideBindingMode.ReplaceExplicit;
                return true;
            case "merge-explicit":
                bindingMode = RestEndpointOverrideBindingMode.MergeExplicit;
                return true;
            default:
                bindingMode = default;
                return false;
        }
    }
}
