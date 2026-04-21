namespace Cephalon.Edge.KubernetesGateway.Configuration;

/// <summary>
/// Defines the stable observation modes supported by the Kubernetes Gateway traffic materializer.
/// </summary>
public static class KubernetesGatewayTrafficObservationModes
{
    /// <summary>
    /// Publishes configured Kubernetes Gateway API intent without reading live control-plane status.
    /// </summary>
    public const string ConfiguredIntent = "configured-intent";

    /// <summary>
    /// Reads live Kubernetes Gateway API status and projects the observed posture back into the shared runtime catalog.
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
                $"Kubernetes Gateway traffic observation mode '{mode}' is not supported.")
        };
    }
}
