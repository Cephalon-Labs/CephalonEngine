namespace Cephalon.Edge.KubernetesGateway.Configuration;

/// <summary>
/// Defines the stable control-plane modes supported by the Kubernetes Gateway traffic materializer.
/// </summary>
public static class KubernetesGatewayTrafficObservationModes
{
    /// <summary>
    /// Publishes configured Kubernetes Gateway API intent without reading or writing live control-plane resources.
    /// </summary>
    public const string ConfiguredIntent = "configured-intent";

    /// <summary>
    /// Reads live Kubernetes Gateway API status and projects the observed posture back into the shared runtime catalog.
    /// </summary>
    public const string ObserveOnly = "observe-only";

    /// <summary>
    /// Applies owned HTTPRoute resources and then reconciles the observed Gateway API status back into the shared runtime catalog.
    /// </summary>
    public const string ApplyAndReconcile = "apply-and-reconcile";

    internal static string Normalize(string? mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode)
            ? ConfiguredIntent
            : mode.Trim().ToLowerInvariant();

        return normalized switch
        {
            ConfiguredIntent => ConfiguredIntent,
            ObserveOnly => ObserveOnly,
            ApplyAndReconcile => ApplyAndReconcile,
            _ => throw new InvalidOperationException(
                $"Kubernetes Gateway traffic observation mode '{mode}' is not supported.")
        };
    }
}
