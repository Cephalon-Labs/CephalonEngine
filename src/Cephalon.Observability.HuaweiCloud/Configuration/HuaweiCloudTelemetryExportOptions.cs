using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.HuaweiCloud.Configuration;

/// <summary>
/// Configures Huawei Cloud observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class HuaweiCloudTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HuaweiCloudTelemetryExportOptions" /> class.
    /// </summary>
    public HuaweiCloudTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the hosted Huawei Cloud platform whose default resource attributes should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>ecs</c>, <c>cce</c>, and <c>functiongraph</c>.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Gets or sets the Huawei Cloud region to stamp onto exported resources when one should be explicit.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package should send traces directly to Huawei Cloud APM
    /// when no shared OTLP endpoint is configured.
    /// </summary>
    /// <remarks>
    /// This direct path is intentionally trace-only. Logs and metrics stay on the shared collector path or on
    /// another runtime-specific route instead of being redirected implicitly.
    /// </remarks>
    public bool UseApmManagedTraceIngestion { get; set; }

    /// <summary>
    /// Gets or sets the Huawei Cloud APM OTLP endpoint used for direct managed trace ingestion.
    /// </summary>
    /// <remarks>
    /// This endpoint is only used when <see cref="UseApmManagedTraceIngestion" /> is enabled and the shared
    /// <c>Engine:Observability:Telemetry:Endpoint</c> setting is omitted.
    /// </remarks>
    public string? ApmEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the authentication token written to the Huawei Cloud <c>Authentication</c> header for
    /// direct managed trace ingestion.
    /// </summary>
    public string? AuthenticationToken { get; set; }

    /// <summary>
    /// Binds Huawei Cloud telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Huawei Cloud telemetry export options.</returns>
    public static HuaweiCloudTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("HuaweiCloud");

        return FromSection(section);
    }

    internal static HuaweiCloudTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new HuaweiCloudTelemetryExportOptions
        {
            HostedPlatform = string.IsNullOrWhiteSpace(section["HostedPlatform"])
                ? null
                : section["HostedPlatform"]!.Trim(),
            Region = string.IsNullOrWhiteSpace(section["Region"])
                ? null
                : section["Region"]!.Trim(),
            UseApmManagedTraceIngestion = GetBoolean(section["UseApmManagedTraceIngestion"], defaultValue: false),
            ApmEndpoint = string.IsNullOrWhiteSpace(section["ApmEndpoint"])
                ? null
                : section["ApmEndpoint"]!.Trim(),
            AuthenticationToken = string.IsNullOrWhiteSpace(section["AuthenticationToken"])
                ? null
                : section["AuthenticationToken"]!.Trim()
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
