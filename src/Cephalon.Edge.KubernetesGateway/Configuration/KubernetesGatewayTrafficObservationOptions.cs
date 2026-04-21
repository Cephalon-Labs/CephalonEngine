namespace Cephalon.Edge.KubernetesGateway.Configuration;

/// <summary>
/// Configures how the Kubernetes Gateway traffic materializer observes live Gateway API status.
/// </summary>
public sealed class KubernetesGatewayTrafficObservationOptions
{
    /// <summary>
    /// Gets or sets the observation mode used by the Kubernetes Gateway materializer.
    /// </summary>
    /// <remarks>
    /// The default value keeps the pack in configured-intent mode so existing projected-intent behavior remains additive.
    /// Set this to <c>observe-only</c> when the pack should read live Gateway API resources and project observed status back
    /// into the shared cell traffic automation runtime surfaces.
    /// </remarks>
    public string Mode { get; set; } = KubernetesGatewayTrafficObservationModes.ConfiguredIntent;

    /// <summary>
    /// Gets or sets a value indicating whether in-cluster Kubernetes configuration should be used when the pack creates its own client.
    /// </summary>
    public bool UseInClusterConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the explicit kubeconfig path used when the pack creates its own client outside the cluster.
    /// </summary>
    public string? KubeConfigPath { get; set; }

    /// <summary>
    /// Gets or sets the optional kubeconfig context override used when the pack creates its own client.
    /// </summary>
    public string? KubeContext { get; set; }

    /// <summary>
    /// Gets or sets the optional API-server override used when the pack creates its own client from kubeconfig.
    /// </summary>
    public string? MasterUrl { get; set; }

    /// <summary>
    /// Gets or sets the polling interval, in seconds, used for recurring live observation after startup reconciliation.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the freshness window, in seconds, that observed status should advertise to operators.
    /// </summary>
    public int StaleAfterSeconds { get; set; } = 90;
}
