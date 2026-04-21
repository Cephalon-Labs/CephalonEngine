namespace Cephalon.Edge.Traefik.Configuration;

/// <summary>
/// Configures how the Traefik traffic materializer reads live Kubernetes resources.
/// </summary>
public sealed class TraefikTrafficObservationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TraefikTrafficObservationOptions" /> class.
    /// </summary>
    public TraefikTrafficObservationOptions()
    {
    }

    /// <summary>
    /// Gets or sets the control-plane mode used by the Traefik materializer.
    /// </summary>
    /// <remarks>
    /// The default value keeps the pack in configured-intent mode so projected-intent behavior remains additive without
    /// claiming a live apply. Set this to <c>observe-only</c> when the pack should read live Traefik Kubernetes CRD
    /// resources and project the observed posture back into the shared runtime catalog, or to
    /// <c>apply-and-reconcile</c> when the pack should write owned <c>IngressRoute</c> resources before observing live
    /// Traefik posture back into that same shared runtime catalog.
    /// </remarks>
    public string Mode { get; set; } = TraefikTrafficObservationModes.ConfiguredIntent;

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
    /// Gets or sets the polling interval, in seconds, used for recurring live observation after startup materialization.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the freshness window, in seconds, that observed status should advertise to operators.
    /// </summary>
    public int StaleAfterSeconds { get; set; } = 90;
}
