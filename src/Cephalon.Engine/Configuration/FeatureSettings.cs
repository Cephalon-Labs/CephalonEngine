using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven feature-flag settings for a Cephalon app.
/// </summary>
public sealed class FeatureSettings
{
    /// <summary>
    /// Gets an empty feature settings instance.
    /// </summary>
    public static FeatureSettings Empty { get; } = new();

    /// <summary>
    /// Creates feature settings.
    /// </summary>
    /// <param name="flags">The feature flags configured for the app.</param>
    public FeatureSettings(IReadOnlyList<FeatureFlagSettings>? flags = null)
    {
        Flags = flags?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the feature flags configured for the app.
    /// </summary>
    public IReadOnlyList<FeatureFlagSettings> Flags { get; }

    /// <summary>
    /// Gets a value indicating whether any feature settings were explicitly supplied.
    /// </summary>
    public bool HasValues => Flags.Count > 0;

    /// <summary>
    /// Reads feature settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed feature settings.</returns>
    public static FeatureSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Features");
        if (!section.Exists())
        {
            return Empty;
        }

        var flags = section
            .GetSection("Flags")
            .GetChildren()
            .Where(static child => child.Exists())
            .Select(FeatureFlagSettings.FromSection)
            .ToArray();

        return new FeatureSettings(flags);
    }
}
