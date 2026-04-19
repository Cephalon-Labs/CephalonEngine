using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven cell-based architecture settings for a Cephalon app.
/// </summary>
public sealed class CellSettings
{
    /// <summary>
    /// Gets an empty cell settings instance.
    /// </summary>
    public static CellSettings Empty { get; } = new();

    /// <summary>
    /// Creates cell settings.
    /// </summary>
    /// <param name="trafficAutomation">The configuration-driven traffic-automation settings for governed cell routes.</param>
    public CellSettings(CellTrafficAutomationSettings? trafficAutomation = null)
    {
        TrafficAutomation = trafficAutomation ?? CellTrafficAutomationSettings.Empty;
    }

    /// <summary>
    /// Gets the configuration-driven traffic-automation settings for governed cell routes.
    /// </summary>
    public CellTrafficAutomationSettings TrafficAutomation { get; }

    /// <summary>
    /// Gets a value indicating whether any cell settings were explicitly supplied.
    /// </summary>
    public bool HasValues => TrafficAutomation.HasValues;

    /// <summary>
    /// Reads cell settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed cell settings.</returns>
    public static CellSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Cells");
        if (!section.Exists())
        {
            return Empty;
        }

        return new CellSettings(
            trafficAutomation: CellTrafficAutomationSettings.FromSection(section.GetSection("TrafficAutomation")));
    }
}
