using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Configuration;

/// <summary>
/// Configures the built-in Cephalon observability package.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>
    /// Creates observability options with the default startup diagnostics behavior.
    /// </summary>
    public ObservabilityOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether a manifest summary should be written at host startup.
    /// </summary>
    public bool LogManifestSummary { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a module summary should be written at host startup.
    /// </summary>
    public bool LogModuleSummary { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a capability summary should be written at host startup.
    /// </summary>
    public bool LogCapabilitySummary { get; set; } = true;

    /// <summary>
    /// Gets or sets the telemetry export guidance associated with the host.
    /// </summary>
    public TelemetryExportOptions Telemetry { get; set; } = new();

    /// <summary>
    /// Binds observability options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound observability options.</returns>
    public static ObservabilityOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability");

        return new ObservabilityOptions
        {
            LogManifestSummary = GetBoolean(section["LogManifestSummary"], defaultValue: true),
            LogModuleSummary = GetBoolean(section["LogModuleSummary"], defaultValue: true),
            LogCapabilitySummary = GetBoolean(section["LogCapabilitySummary"], defaultValue: true),
            Telemetry = TelemetryExportOptions.FromConfiguration(section.GetSection("Telemetry"))
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;
}
