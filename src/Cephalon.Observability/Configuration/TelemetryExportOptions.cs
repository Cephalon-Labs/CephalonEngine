using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Configuration;

/// <summary>
/// Describes how operators intend host telemetry to be exported.
/// </summary>
/// <remarks>
/// These settings let Cephalon packages and hosts agree on provider, protocol, endpoint, and enabled
/// signals without forcing exporter dependencies into the engine core. Companion packages such as
/// <c>Cephalon.Observability.OpenTelemetry</c>, <c>Cephalon.Observability.Aws</c>, or
/// <c>Cephalon.Observability.AzureMonitor</c> can
/// interpret the same contract when a host wants a supported export path, including the explicit
/// self-hosted collector defaults that remain outside <c>Cephalon.Engine</c>.
/// </remarks>
public sealed class TelemetryExportOptions
{
    /// <summary>
    /// Creates telemetry export options with the default guidance values.
    /// </summary>
    public TelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the telemetry provider name, such as <c>OpenTelemetry</c>.
    /// </summary>
    public string Provider { get; set; } = "OpenTelemetry";

    /// <summary>
    /// Gets or sets the telemetry transport protocol, such as <c>otlp</c>, <c>otlp/grpc</c>, or <c>otlp/http</c>.
    /// </summary>
    public string Protocol { get; set; } = "otlp";

    /// <summary>
    /// Gets or sets the target export endpoint, if one is configured.
    /// Companion packages interpret this as the base collector endpoint for the selected export protocol.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether companion packages should apply the supported
    /// self-hosted collector and runtime defaults when the export endpoint is omitted.
    /// </summary>
    /// <remarks>
    /// The shipped OpenTelemetry companion interprets this flag as an explicit self-hosted path on
    /// top of the shared OTLP baseline, using the standard local collector ports and host-managed
    /// runtime resource defaults instead of vendor-specific wiring.
    /// </remarks>
    public bool UseSelfHostedDefaults { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether logs should be exported.
    /// </summary>
    public bool ExportLogs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics should be exported.
    /// </summary>
    public bool ExportMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether traces should be exported.
    /// </summary>
    public bool ExportTraces { get; set; } = true;

    internal static TelemetryExportOptions FromConfiguration(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new TelemetryExportOptions
        {
            Provider = string.IsNullOrWhiteSpace(section["Provider"])
                ? "OpenTelemetry"
                : section["Provider"]!.Trim(),
            Protocol = string.IsNullOrWhiteSpace(section["Protocol"])
                ? "otlp"
                : section["Protocol"]!.Trim(),
            Endpoint = string.IsNullOrWhiteSpace(section["Endpoint"])
                ? null
                : section["Endpoint"]!.Trim(),
            UseSelfHostedDefaults = GetBoolean(section["UseSelfHostedDefaults"], defaultValue: false),
            ExportLogs = GetBoolean(section["ExportLogs"], defaultValue: true),
            ExportMetrics = GetBoolean(section["ExportMetrics"], defaultValue: true),
            ExportTraces = GetBoolean(section["ExportTraces"], defaultValue: true)
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
