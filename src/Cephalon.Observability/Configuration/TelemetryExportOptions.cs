using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Configuration;

/// <summary>
/// Describes how operators intend host telemetry to be exported.
/// </summary>
/// <remarks>
/// These settings are intentionally guidance-oriented. They let Cephalon packages and hosts agree on
/// provider, protocol, endpoint, and enabled signals without forcing a specific exporter implementation.
/// </remarks>
public sealed class TelemetryExportOptions
{
    /// <summary>
    /// Gets or sets the telemetry provider name, such as <c>OpenTelemetry</c>.
    /// </summary>
    public string Provider { get; set; } = "OpenTelemetry";

    /// <summary>
    /// Gets or sets the telemetry transport protocol, such as <c>otlp</c>.
    /// </summary>
    public string Protocol { get; set; } = "otlp";

    /// <summary>
    /// Gets or sets the target export endpoint, if one is configured.
    /// </summary>
    public string? Endpoint { get; set; }

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
