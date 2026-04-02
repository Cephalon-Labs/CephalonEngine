using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.AlibabaCloud.Configuration;

/// <summary>
/// Configures Alibaba Cloud observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class AlibabaCloudTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlibabaCloudTelemetryExportOptions" /> class.
    /// </summary>
    public AlibabaCloudTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the hosted Alibaba Cloud platform whose default resource attributes should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>ecs</c>, <c>fc</c> / <c>functioncompute</c>, and <c>openshift</c>.
    /// The package maps them to the current OpenTelemetry <c>cloud.platform</c> attribute values.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Gets or sets the Alibaba Cloud region to stamp onto exported resources when one should be explicit.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package should use Alibaba Cloud Managed Service for
    /// OpenTelemetry when no shared collector endpoint is configured.
    /// </summary>
    /// <remarks>
    /// This direct managed path is intentionally limited to traces and metrics. Logs stay on the shared
    /// collector path, SLS, or another runtime-specific route instead of being redirected implicitly.
    /// </remarks>
    public bool UseManagedOpenTelemetryIngestion { get; set; }

    /// <summary>
    /// Gets or sets the Alibaba Cloud Managed Service for OpenTelemetry OTLP/gRPC endpoint used for direct
    /// managed traces and metrics ingestion.
    /// </summary>
    /// <remarks>
    /// This endpoint is only used when <see cref="UseManagedOpenTelemetryIngestion" /> is enabled, the
    /// shared <c>Engine:Observability:Telemetry:Endpoint</c> setting is omitted, and the protocol remains on
    /// <c>otlp</c> or <c>otlp/grpc</c>.
    /// </remarks>
    public string? ManagedGrpcEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the Alibaba Cloud Managed Service for OpenTelemetry OTLP/HTTP traces endpoint used for
    /// direct managed ingestion.
    /// </summary>
    /// <remarks>
    /// This endpoint is only used when <see cref="UseManagedOpenTelemetryIngestion" /> is enabled, the
    /// shared endpoint is omitted, and the protocol stays on <c>otlp/http</c>. The value should already be
    /// the full Alibaba Cloud-managed trace URL, including any tokenized path shape required by the
    /// provider.
    /// </remarks>
    public string? ManagedHttpTracesEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the Alibaba Cloud Managed Service for OpenTelemetry OTLP/HTTP metrics endpoint used for
    /// direct managed ingestion.
    /// </summary>
    /// <remarks>
    /// This endpoint is only used when <see cref="UseManagedOpenTelemetryIngestion" /> is enabled, the
    /// shared endpoint is omitted, and the protocol stays on <c>otlp/http</c>. The value should already be
    /// the full Alibaba Cloud-managed metrics URL, including any tokenized path shape required by the
    /// provider.
    /// </remarks>
    public string? ManagedHttpMetricsEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the authentication token written to the Alibaba Cloud <c>Authentication</c> header for
    /// OTLP/gRPC direct managed ingestion.
    /// </summary>
    /// <remarks>
    /// This value is only used when <see cref="UseManagedOpenTelemetryIngestion" /> is enabled and the
    /// protocol stays on <c>otlp</c> or <c>otlp/grpc</c>. OTLP/HTTP managed ingestion expects the supplied
    /// signal-specific endpoints to already follow the provider's documented tokenized URL shape instead.
    /// </remarks>
    public string? AuthenticationToken { get; set; }

    /// <summary>
    /// Binds Alibaba Cloud telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Alibaba Cloud telemetry export options.</returns>
    public static AlibabaCloudTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("AlibabaCloud");

        return FromSection(section);
    }

    internal static AlibabaCloudTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new AlibabaCloudTelemetryExportOptions
        {
            HostedPlatform = string.IsNullOrWhiteSpace(section["HostedPlatform"])
                ? null
                : section["HostedPlatform"]!.Trim(),
            Region = string.IsNullOrWhiteSpace(section["Region"])
                ? null
                : section["Region"]!.Trim(),
            UseManagedOpenTelemetryIngestion = GetBoolean(section["UseManagedOpenTelemetryIngestion"], defaultValue: false),
            ManagedGrpcEndpoint = string.IsNullOrWhiteSpace(section["ManagedGrpcEndpoint"])
                ? null
                : section["ManagedGrpcEndpoint"]!.Trim(),
            ManagedHttpTracesEndpoint = string.IsNullOrWhiteSpace(section["ManagedHttpTracesEndpoint"])
                ? null
                : section["ManagedHttpTracesEndpoint"]!.Trim(),
            ManagedHttpMetricsEndpoint = string.IsNullOrWhiteSpace(section["ManagedHttpMetricsEndpoint"])
                ? null
                : section["ManagedHttpMetricsEndpoint"]!.Trim(),
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
