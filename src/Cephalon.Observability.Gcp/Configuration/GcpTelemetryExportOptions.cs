using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Gcp.Configuration;

/// <summary>
/// Configures GCP-hosted observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class GcpTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GcpTelemetryExportOptions" /> class.
    /// </summary>
    public GcpTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the package should use Google-managed OTLP ingestion for traces
    /// and metrics when no shared collector endpoint is configured.
    /// </summary>
    /// <remarks>
    /// When this flag is enabled, the package targets <c>https://telemetry.googleapis.com</c> for traces and
    /// metrics by using OTLP/HTTP plus Application Default Credentials. Logs stay on the shared collector or
    /// platform logging path instead of being redirected through the Google-managed ingestion endpoint.
    /// </remarks>
    public bool UseGoogleManagedIngestion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Google-managed ingestion should authenticate by using
    /// Application Default Credentials.
    /// </summary>
    /// <remarks>
    /// This setting only applies when <see cref="UseGoogleManagedIngestion" /> is enabled and no shared
    /// collector endpoint is configured.
    /// </remarks>
    public bool UseApplicationDefaultCredentials { get; set; } = true;

    /// <summary>
    /// Gets or sets the hosted GCP platform whose default resource attributes should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>gce</c>, <c>gke</c>, <c>cloudrun</c>, <c>appengine</c>, and <c>functions</c>.
    /// The package maps them to the current OpenTelemetry <c>cloud.platform</c> attribute values.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Gets or sets the GCP location to stamp onto exported resources when one should be made explicit.
    /// </summary>
    /// <remarks>
    /// When the value looks like a zonal location such as <c>asia-southeast1-b</c>, the package also derives
    /// the matching <c>cloud.region</c>. When omitted, the package falls back to common GCP runtime
    /// environment variables when they are available.
    /// </remarks>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the optional quota project header used for Google-managed ingestion requests.
    /// </summary>
    /// <remarks>
    /// This value is written to the <c>x-goog-user-project</c> header only for Google-managed ingestion.
    /// Shared collector or self-hosted OTLP paths ignore it.
    /// </remarks>
    public string? QuotaProjectId { get; set; }

    /// <summary>
    /// Binds GCP telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound GCP telemetry export options.</returns>
    public static GcpTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("Gcp");

        return FromSection(section);
    }

    internal static GcpTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new GcpTelemetryExportOptions
        {
            UseGoogleManagedIngestion = GetBoolean(section["UseGoogleManagedIngestion"], defaultValue: false),
            UseApplicationDefaultCredentials = GetBoolean(section["UseApplicationDefaultCredentials"], defaultValue: true),
            HostedPlatform = string.IsNullOrWhiteSpace(section["HostedPlatform"])
                ? null
                : section["HostedPlatform"]!.Trim(),
            Location = string.IsNullOrWhiteSpace(section["Location"])
                ? null
                : section["Location"]!.Trim(),
            QuotaProjectId = string.IsNullOrWhiteSpace(section["QuotaProjectId"])
                ? null
                : section["QuotaProjectId"]!.Trim()
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
