using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven migration settings for a Cephalon app.
/// </summary>
public sealed class MigrationSettings
{
    /// <summary>
    /// Gets an empty migration-settings instance.
    /// </summary>
    public static MigrationSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationSettings" /> class.
    /// </summary>
    /// <param name="stranglerFig">The strangler-fig migration settings resolved for the app.</param>
    public MigrationSettings(StranglerFigMigrationSettings? stranglerFig = null)
    {
        StranglerFig = stranglerFig ?? StranglerFigMigrationSettings.Empty;
    }

    /// <summary>
    /// Gets the strangler-fig migration settings resolved for the app.
    /// </summary>
    public StranglerFigMigrationSettings StranglerFig { get; }

    /// <summary>
    /// Gets a value indicating whether any migration settings were explicitly supplied.
    /// </summary>
    public bool HasValues => StranglerFig.HasValues;

    /// <summary>
    /// Reads migration settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed migration settings.</returns>
    public static MigrationSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Migration");

        if (!section.Exists())
        {
            return Empty;
        }

        return new MigrationSettings(
            stranglerFig: StranglerFigMigrationSettings.FromSection(section.GetSection("StranglerFig")));
    }
}
