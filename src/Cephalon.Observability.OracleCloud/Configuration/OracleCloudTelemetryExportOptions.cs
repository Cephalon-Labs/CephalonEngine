using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.OracleCloud.Configuration;

/// <summary>
/// Configures Oracle Cloud observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class OracleCloudTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleCloudTelemetryExportOptions" /> class.
    /// </summary>
    public OracleCloudTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the hosted Oracle Cloud platform whose default resource attributes should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>compute</c>, <c>oke</c>, and <c>functions</c>.
    /// The package maps them to the current OpenTelemetry <c>cloud.platform</c> attribute values.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Gets or sets the Oracle Cloud region to stamp onto exported resources when one should be explicit.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package should use Oracle Cloud APM managed
    /// OpenTelemetry ingestion when no shared collector endpoint is configured.
    /// </summary>
    /// <remarks>
    /// This direct managed path is intentionally limited to traces and metrics over OTLP/HTTP.
    /// Logs stay on the shared collector path, Oracle Log Analytics, or another runtime-specific route
    /// instead of being redirected implicitly.
    /// </remarks>
    public bool UseManagedOpenTelemetryIngestion { get; set; }

    /// <summary>
    /// Gets or sets the Oracle Cloud APM data upload endpoint used to build direct managed
    /// OTLP/HTTP ingestion URLs.
    /// </summary>
    /// <remarks>
    /// This value should be the Oracle Cloud APM <c>DataUploadEndpoint</c> for the target domain.
    /// The package appends the documented OpenTelemetry signal paths when direct managed ingestion is enabled.
    /// </remarks>
    public string? DataUploadEndpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether trace ingestion should use the public Oracle Cloud
    /// APM data key path instead of the private data key path.
    /// </summary>
    /// <remarks>
    /// Metrics always use the private-key path in Oracle Cloud APM.
    /// </remarks>
    public bool UsePublicTraceDataKey { get; set; }

    /// <summary>
    /// Gets or sets the Oracle Cloud APM trace data key used for direct managed trace ingestion.
    /// </summary>
    /// <remarks>
    /// When <see cref="UsePublicTraceDataKey" /> is <see langword="true" />, this should be the public
    /// trace key. Otherwise it should be the private trace key.
    /// </remarks>
    public string? TraceDataKey { get; set; }

    /// <summary>
    /// Gets or sets the Oracle Cloud APM private metrics data key used for direct managed metrics ingestion.
    /// </summary>
    public string? MetricsDataKey { get; set; }

    /// <summary>
    /// Binds Oracle Cloud telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Oracle Cloud telemetry export options.</returns>
    public static OracleCloudTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("OracleCloud");

        return FromSection(section);
    }

    internal static OracleCloudTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new OracleCloudTelemetryExportOptions
        {
            HostedPlatform = Normalize(section["HostedPlatform"]),
            Region = Normalize(section["Region"]),
            UseManagedOpenTelemetryIngestion = GetBoolean(section["UseManagedOpenTelemetryIngestion"], defaultValue: false),
            DataUploadEndpoint = Normalize(section["DataUploadEndpoint"]),
            UsePublicTraceDataKey = GetBoolean(section["UsePublicTraceDataKey"], defaultValue: false),
            TraceDataKey = Normalize(section["TraceDataKey"]),
            MetricsDataKey = Normalize(section["MetricsDataKey"])
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
}
