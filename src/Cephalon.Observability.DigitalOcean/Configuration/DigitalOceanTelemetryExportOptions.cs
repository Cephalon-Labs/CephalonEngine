using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.DigitalOcean.Configuration;

/// <summary>
/// Configures DigitalOcean observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class DigitalOceanTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DigitalOceanTelemetryExportOptions" /> class.
    /// </summary>
    public DigitalOceanTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the DigitalOcean deployment target whose hosted defaults should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>droplet</c>, <c>doks</c>, and <c>app-platform</c>. The package maps them to
    /// package-specific <c>cloud.platform</c> values so the collector-first DigitalOcean slice stays explicit
    /// without pretending those values are part of the engine core.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Gets or sets the DigitalOcean region slug to stamp onto exported resources.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes cluster name to stamp onto exported resources for DOKS deployments.
    /// </summary>
    public string? ClusterName { get; set; }

    /// <summary>
    /// Gets or sets the workload namespace to stamp onto exported resources for DOKS deployments.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the current pod namespace through the <c>POD_NAMESPACE</c>
    /// environment variable when it is available.
    /// </remarks>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package should target an in-cluster collector service for
    /// DOKS deployments when no shared endpoint is configured.
    /// </summary>
    public bool UseInClusterCollectorService { get; set; }

    /// <summary>
    /// Gets or sets the collector service name used to build the in-cluster DOKS collector endpoint.
    /// </summary>
    public string? CollectorServiceName { get; set; }

    /// <summary>
    /// Gets or sets the collector namespace used to build the in-cluster DOKS collector endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <see cref="Namespace" /> and then to <c>POD_NAMESPACE</c>
    /// when the host runs inside a pod.
    /// </remarks>
    public string? CollectorNamespace { get; set; }

    /// <summary>
    /// Gets or sets the URI scheme used for the in-cluster DOKS collector endpoint.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>http</c> and <c>https</c>. The default is <c>http</c>.
    /// </remarks>
    public string? CollectorScheme { get; set; }

    /// <summary>
    /// Gets or sets the collector port used for the in-cluster DOKS collector endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to <c>4317</c> for <c>otlp</c> / <c>otlp/grpc</c> and
    /// <c>4318</c> for <c>otlp/http</c>.
    /// </remarks>
    public int? CollectorPort { get; set; }

    /// <summary>
    /// Gets or sets the filesystem path to a trusted CA bundle used for HTTPS OTLP/HTTP traces and metrics
    /// against shared or in-cluster DigitalOcean collectors.
    /// </summary>
    /// <remarks>
    /// Because the current OTLP logging exporter does not support custom <c>HttpClient</c> wiring for HTTP,
    /// the package rejects configurations that require this custom CA path for logs. OTLP/gRPC custom CA
    /// wiring is also left explicit and unsupported for now.
    /// </remarks>
    public string? TrustedCaCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the raw OTLP headers string added to collector requests.
    /// </summary>
    /// <remarks>
    /// Use the standard OTLP <c>key=value</c> comma-separated format when a collector, gateway, or route
    /// expects explicit headers.
    /// </remarks>
    public string? Headers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package should query the Droplet metadata service to fill
    /// in best-effort <c>host.id</c>, <c>host.name</c>, and <c>cloud.region</c> values when they are missing.
    /// </summary>
    public bool UseDropletMetadataDefaults { get; set; }

    /// <summary>
    /// Gets or sets the timeout, in milliseconds, used for Droplet metadata-service lookups.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to a short one-second timeout so non-Droplet hosts do not stall
    /// during best-effort metadata detection.
    /// </remarks>
    public int? MetadataTimeoutMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the base URI of the Droplet metadata-service endpoint.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the standard DigitalOcean metadata-service base URI.
    /// This override exists mainly for proxies, alternative routing, or automated testing.
    /// </remarks>
    public string? DropletMetadataEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the Droplet identifier to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted and <see cref="UseDropletMetadataDefaults" /> is enabled, the package attempts to read
    /// the identifier from the Droplet metadata service.
    /// </remarks>
    public string? DropletId { get; set; }

    /// <summary>
    /// Gets or sets the App Platform application identifier to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the <c>APP_ID</c> or <c>DIGITALOCEAN_APP_ID</c> environment
    /// variable when it is available.
    /// </remarks>
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets the App Platform public URL to stamp onto exported resources.
    /// </summary>
    /// <remarks>
    /// When omitted, the package falls back to the <c>APP_URL</c> or <c>DIGITALOCEAN_APP_URL</c>
    /// environment variable when it is available.
    /// </remarks>
    public string? AppUrl { get; set; }

    /// <summary>
    /// Binds DigitalOcean telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound DigitalOcean telemetry export options.</returns>
    public static DigitalOceanTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("DigitalOcean");

        return FromSection(section);
    }

    internal static DigitalOceanTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new DigitalOceanTelemetryExportOptions
        {
            HostedPlatform = Normalize(section["HostedPlatform"]),
            Region = Normalize(section["Region"]),
            ClusterName = Normalize(section["ClusterName"]),
            Namespace = Normalize(section["Namespace"]),
            UseInClusterCollectorService = GetBoolean(section["UseInClusterCollectorService"], defaultValue: false),
            CollectorServiceName = Normalize(section["CollectorServiceName"]),
            CollectorNamespace = Normalize(section["CollectorNamespace"]),
            CollectorScheme = Normalize(section["CollectorScheme"]),
            CollectorPort = GetNullableInt32(section["CollectorPort"]),
            TrustedCaCertificatePath = Normalize(section["TrustedCaCertificatePath"]),
            Headers = Normalize(section["Headers"]),
            UseDropletMetadataDefaults = GetBoolean(section["UseDropletMetadataDefaults"], defaultValue: false),
            MetadataTimeoutMilliseconds = GetNullableInt32(section["MetadataTimeoutMilliseconds"]),
            DropletMetadataEndpoint = Normalize(section["DropletMetadataEndpoint"]),
            DropletId = Normalize(section["DropletId"]),
            AppId = Normalize(section["AppId"]),
            AppUrl = Normalize(section["AppUrl"])
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
