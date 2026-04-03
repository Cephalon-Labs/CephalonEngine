using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Kubernetes.Configuration;

/// <summary>
/// Configures Kubernetes observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class KubernetesTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesTelemetryExportOptions" /> class.
    /// </summary>
    public KubernetesTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the Kubernetes cluster name to stamp onto exported resources.
    /// </summary>
    public string? ClusterName { get; set; }

    /// <summary>
    /// Gets or sets the workload namespace to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the current pod namespace through the <c>POD_NAMESPACE</c>
    /// environment variable when it is available.
    /// </remarks>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the pod name to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <c>POD_NAME</c> and then <c>HOSTNAME</c> when those environment
    /// variables are available.
    /// </remarks>
    public string? PodName { get; set; }

    /// <summary>
    /// Gets or sets the pod UID to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the <c>POD_UID</c> environment variable when it is available.
    /// </remarks>
    public string? PodUid { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes node name to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the <c>NODE_NAME</c> environment variable when it is available.
    /// </remarks>
    public string? NodeName { get; set; }

    /// <summary>
    /// Gets or sets the container name to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the <c>CONTAINER_NAME</c> environment variable when it is available.
    /// </remarks>
    public string? ContainerName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package should target an in-cluster collector service when no
    /// shared endpoint is configured.
    /// </summary>
    public bool UseInClusterCollectorService { get; set; }

    /// <summary>
    /// Gets or sets the collector service name used to build the in-cluster Kubernetes collector endpoint.
    /// </summary>
    public string? CollectorServiceName { get; set; }

    /// <summary>
    /// Gets or sets the collector namespace used to build the in-cluster Kubernetes collector endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <see cref="Namespace" /> and then to <c>POD_NAMESPACE</c> when
    /// the host runs inside a pod.
    /// </remarks>
    public string? CollectorNamespace { get; set; }

    /// <summary>
    /// Gets or sets the URI scheme used for the in-cluster Kubernetes collector endpoint.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>http</c> and <c>https</c>. The default is <c>http</c>.
    /// </remarks>
    public string? CollectorScheme { get; set; }

    /// <summary>
    /// Gets or sets the collector port used for the in-cluster Kubernetes collector endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <c>4317</c> for <c>otlp</c> / <c>otlp/grpc</c> and
    /// <c>4318</c> for <c>otlp/http</c>.
    /// </remarks>
    public int? CollectorPort { get; set; }

    /// <summary>
    /// Gets or sets the service DNS suffix used to build the in-cluster Kubernetes collector endpoint.
    /// </summary>
    /// <remarks>
    /// The default is <c>svc.cluster.local</c>. Set this when a cluster uses a non-default service DNS suffix.
    /// </remarks>
    public string? ServiceDnsSuffix { get; set; }

    /// <summary>
    /// Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics
    /// against in-cluster or shared Kubernetes collectors.
    /// </summary>
    /// <remarks>
    /// Because the current OTLP logging exporter does not support custom <c>HttpClient</c> wiring for HTTP,
    /// the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA
    /// wiring is also left explicit and unsupported for now.
    /// </remarks>
    public string? TrustedCaCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the raw OTLP headers string added to Kubernetes collector requests.
    /// </summary>
    /// <remarks>
    /// Use the standard OTLP <c>key=value</c> comma-separated format when a collector, gateway, or route
    /// expects explicit headers.
    /// </remarks>
    public string? Headers { get; set; }

    /// <summary>
    /// Binds Kubernetes telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Kubernetes telemetry export options.</returns>
    public static KubernetesTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("Kubernetes");

        return FromSection(section);
    }

    internal static KubernetesTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new KubernetesTelemetryExportOptions
        {
            ClusterName = Normalize(section["ClusterName"]),
            Namespace = Normalize(section["Namespace"]),
            PodName = Normalize(section["PodName"]),
            PodUid = Normalize(section["PodUid"]),
            NodeName = Normalize(section["NodeName"]),
            ContainerName = Normalize(section["ContainerName"]),
            UseInClusterCollectorService = GetBoolean(section["UseInClusterCollectorService"], defaultValue: false),
            CollectorServiceName = Normalize(section["CollectorServiceName"]),
            CollectorNamespace = Normalize(section["CollectorNamespace"]),
            CollectorScheme = Normalize(section["CollectorScheme"]),
            CollectorPort = GetNullableInt32(section["CollectorPort"]),
            ServiceDnsSuffix = NormalizeDnsSuffix(section["ServiceDnsSuffix"]),
            TrustedCaCertificatePath = Normalize(section["TrustedCaCertificatePath"]),
            Headers = Normalize(section["Headers"])
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeDnsSuffix(string? value)
    {
        var normalized = Normalize(value);
        return normalized?.Trim('.').Replace('/', '.');
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int? GetNullableInt32(string? value)
    {
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }
}
