using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Tanzu.Configuration;

/// <summary>
/// Configures VMware Tanzu observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class TanzuTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TanzuTelemetryExportOptions" /> class.
    /// </summary>
    public TanzuTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the VMware Tanzu deployment target whose hosted defaults should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>tkg</c>, <c>tkgi</c>, and <c>tap</c>.
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
    /// Gets or sets a value indicating whether the package should target an in-cluster Tanzu proxy service
    /// for trace handoff when no shared endpoint is configured.
    /// </summary>
    /// <remarks>
    /// This trace-handoff path intentionally stays explicit because the current Tanzu and Wavefront
    /// documentation centers proxy-mediated OpenTelemetry traces, not one generic managed OTLP backend for
    /// every signal.
    /// </remarks>
    public bool UseInClusterProxyService { get; set; }

    /// <summary>
    /// Gets or sets the proxy service name used to build the in-cluster Tanzu proxy endpoint.
    /// </summary>
    public string? ProxyServiceName { get; set; }

    /// <summary>
    /// Gets or sets the proxy namespace used to build the in-cluster Tanzu proxy endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <see cref="Namespace" /> and then to
    /// <c>POD_NAMESPACE</c> when the host runs inside a pod.
    /// </remarks>
    public string? ProxyNamespace { get; set; }

    /// <summary>
    /// Gets or sets the URI scheme used for the in-cluster Tanzu proxy endpoint.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>http</c> and <c>https</c>. The default is <c>http</c>.
    /// </remarks>
    public string? ProxyScheme { get; set; }

    /// <summary>
    /// Gets or sets the proxy port used for the in-cluster Tanzu proxy endpoint.
    /// </summary>
    /// <remarks>
    /// This value stays required when <see cref="UseInClusterProxyService" /> is enabled so the package does
    /// not pretend the current Tanzu proxy path has one generic vendor-wide OTLP port for every deployment.
    /// </remarks>
    public int? ProxyPort { get; set; }

    /// <summary>
    /// Gets or sets the optional base path used for the in-cluster Tanzu proxy endpoint.
    /// </summary>
    /// <remarks>
    /// When <c>otlp/http</c> is selected, the package still appends the standard OTLP signal path beneath
    /// this base path unless the full signal path was already provided.
    /// </remarks>
    public string? ProxyPath { get; set; }

    /// <summary>
    /// Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics
    /// against shared collectors or Tanzu proxy endpoints.
    /// </summary>
    /// <remarks>
    /// Because the current OTLP logging exporter does not support custom <c>HttpClient</c> wiring for HTTP,
    /// the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA
    /// wiring is also left explicit and unsupported for now.
    /// </remarks>
    public string? TrustedCaCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the raw OTLP headers string added to Tanzu proxy or shared collector requests.
    /// </summary>
    /// <remarks>
    /// Use the standard OTLP <c>key=value</c> comma-separated format when a proxy, route, or gateway expects
    /// explicit headers.
    /// </remarks>
    public string? Headers { get; set; }

    /// <summary>
    /// Binds Tanzu telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Tanzu telemetry export options.</returns>
    public static TanzuTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("Tanzu");

        return FromSection(section);
    }

    internal static TanzuTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new TanzuTelemetryExportOptions
        {
            HostedPlatform = Normalize(section["HostedPlatform"]),
            ClusterName = Normalize(section["ClusterName"]),
            Namespace = Normalize(section["Namespace"]),
            UseInClusterProxyService = GetBoolean(section["UseInClusterProxyService"], defaultValue: false),
            ProxyServiceName = Normalize(section["ProxyServiceName"]),
            ProxyNamespace = Normalize(section["ProxyNamespace"]),
            ProxyScheme = Normalize(section["ProxyScheme"]),
            ProxyPort = GetNullableInt32(section["ProxyPort"]),
            ProxyPath = NormalizePath(section["ProxyPath"]),
            TrustedCaCertificatePath = Normalize(section["TrustedCaCertificatePath"]),
            Headers = Normalize(section["Headers"])
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizePath(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return null;
        }

        return normalized.Length > 0 && normalized[0] == '/'
            ? normalized
            : "/" + normalized;
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
