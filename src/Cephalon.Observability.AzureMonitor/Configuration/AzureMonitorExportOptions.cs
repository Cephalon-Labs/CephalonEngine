using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.AzureMonitor.Configuration;

/// <summary>
/// Configures Azure Monitor exporter wiring on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class AzureMonitorExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureMonitorExportOptions" /> class.
    /// </summary>
    public AzureMonitorExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the Azure Monitor / Application Insights connection string used by the exporter.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the exporter should authenticate with <c>DefaultAzureCredential</c>.
    /// </summary>
    /// <remarks>
    /// The connection string still determines the Azure Monitor resource endpoint. This flag only adds
    /// Azure Active Directory authentication on top of that endpoint selection.
    /// </remarks>
    public bool UseDefaultAzureCredential { get; set; }

    /// <summary>
    /// Gets or sets the hosted Azure platform whose default resource attributes should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>appservice</c>, <c>functions</c>, <c>aks</c>, <c>containerapps</c>, and <c>vm</c>.
    /// The package maps them to the current OpenTelemetry <c>cloud.platform</c> attribute values.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Binds Azure Monitor export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound Azure Monitor export options.</returns>
    public static AzureMonitorExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("AzureMonitor");

        return FromSection(section);
    }

    internal static AzureMonitorExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new AzureMonitorExportOptions
        {
            ConnectionString = string.IsNullOrWhiteSpace(section["ConnectionString"])
                ? null
                : section["ConnectionString"]!.Trim(),
            UseDefaultAzureCredential = GetBoolean(section["UseDefaultAzureCredential"], defaultValue: false),
            HostedPlatform = string.IsNullOrWhiteSpace(section["HostedPlatform"])
                ? null
                : section["HostedPlatform"]!.Trim()
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
