namespace Cephalon.Edge.Traefik.Configuration;

/// <summary>
/// Defines the stable control-plane modes supported by the Traefik traffic materializer.
/// </summary>
public static class TraefikTrafficObservationModes
{
    /// <summary>
    /// Publishes configured Traefik IngressRoute intent without reading live control-plane resources.
    /// </summary>
    public const string ConfiguredIntent = "configured-intent";

    /// <summary>
    /// Reads live Traefik Kubernetes CRD resources and projects the observed posture back into the shared runtime catalog.
    /// </summary>
    public const string ObserveOnly = "observe-only";

    internal static string Normalize(string? mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode)
            ? ConfiguredIntent
            : mode.Trim().ToLowerInvariant();

        return normalized switch
        {
            ConfiguredIntent => ConfiguredIntent,
            ObserveOnly => ObserveOnly,
            _ => throw new InvalidOperationException(
                $"Traefik traffic observation mode '{mode}' is not supported.")
        };
    }
}
