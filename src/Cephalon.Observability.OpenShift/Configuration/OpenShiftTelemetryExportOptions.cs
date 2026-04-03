using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.OpenShift.Configuration;

/// <summary>
/// Configures Red Hat OpenShift observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class OpenShiftTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenShiftTelemetryExportOptions" /> class.
    /// </summary>
    public OpenShiftTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the OpenShift deployment target whose hosted defaults should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>openshift</c>, <c>aro</c>, and <c>rosa</c>.
    /// </remarks>
    public string? HostedPlatform { get; set; }

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
    /// Gets or sets a value indicating whether the package should target an in-cluster OpenShift collector
    /// service when no shared endpoint is configured.
    /// </summary>
    public bool UseInClusterCollectorService { get; set; }

    /// <summary>
    /// Gets or sets the collector service name used to build the in-cluster OpenShift collector endpoint.
    /// </summary>
    public string? CollectorServiceName { get; set; }

    /// <summary>
    /// Gets or sets the collector namespace used to build the in-cluster OpenShift collector endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <see cref="Namespace" /> and then to
    /// <c>POD_NAMESPACE</c> when the host runs inside a pod.
    /// </remarks>
    public string? CollectorNamespace { get; set; }

    /// <summary>
    /// Gets or sets the URI scheme used for the in-cluster OpenShift collector endpoint.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>http</c> and <c>https</c>. The default is <c>http</c>.
    /// </remarks>
    public string? CollectorScheme { get; set; }

    /// <summary>
    /// Gets or sets the collector port used for the in-cluster OpenShift collector endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <c>4317</c> for <c>otlp</c> / <c>otlp/grpc</c> and
    /// <c>4318</c> for <c>otlp/http</c>.
    /// </remarks>
    public int? CollectorPort { get; set; }

    /// <summary>
    /// Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics
    /// against in-cluster or shared OpenShift collectors.
    /// </summary>
    /// <remarks>
    /// This setting is intended for OpenShift service CAs or other cluster-local trust roots. Because the
    /// current OTLP logging exporter does not support custom <c>HttpClient</c> wiring for HTTP, the package
    /// rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA wiring is also
    /// left explicit and unsupported for now.
    /// </remarks>
    public string? TrustedCaCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the raw OTLP headers string added to OpenShift collector requests.
    /// </summary>
    /// <remarks>
    /// Use the standard OTLP <c>key=value</c> comma-separated format when a collector, gateway, or route
    /// expects explicit headers.
    /// </remarks>
    public string? Headers { get; set; }

    /// <summary>
    /// Binds OpenShift telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound OpenShift telemetry export options.</returns>
    public static OpenShiftTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("OpenShift");

        return FromSection(section);
    }

    internal static OpenShiftTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new OpenShiftTelemetryExportOptions
        {
            HostedPlatform = Normalize(section["HostedPlatform"]),
            ClusterName = Normalize(section["ClusterName"]),
            Namespace = Normalize(section["Namespace"]),
            UseInClusterCollectorService = GetBoolean(section["UseInClusterCollectorService"], defaultValue: false),
            CollectorServiceName = Normalize(section["CollectorServiceName"]),
            CollectorNamespace = Normalize(section["CollectorNamespace"]),
            CollectorScheme = Normalize(section["CollectorScheme"]),
            CollectorPort = GetNullableInt32(section["CollectorPort"]),
            TrustedCaCertificatePath = Normalize(section["TrustedCaCertificatePath"]),
            Headers = Normalize(section["Headers"])
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
