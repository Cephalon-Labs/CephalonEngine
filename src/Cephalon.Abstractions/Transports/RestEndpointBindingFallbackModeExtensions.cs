namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointBindingFallbackMode"/>.
/// </summary>
public static class RestEndpointBindingFallbackModeExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization and compatibility metadata for the fallback mode.
    /// </summary>
    /// <param name="mode">The fallback mode.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointBindingFallbackMode mode)
    {
        return mode switch
        {
            RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback => "preserve-source-implicit-fallback",
            RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback => "preserve-remaining-body-fallback",
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "A supported REST endpoint binding fallback mode is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and compatibility metadata into a fallback mode.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="mode">The parsed fallback mode when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported fallback mode; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointBindingFallbackMode mode)
    {
        switch (value?.Trim())
        {
            case "preserve-source-implicit-fallback":
                mode = RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback;
                return true;
            case "preserve-remaining-body-fallback":
                mode = RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback;
                return true;
            default:
                mode = default;
                return false;
        }
    }
}
