using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven multi-tenancy settings for a Cephalon app.
/// </summary>
public sealed class TenancySettings
{
    /// <summary>
    /// Gets an empty tenancy-settings instance.
    /// </summary>
    public static TenancySettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancySettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether multi-tenancy was explicitly enabled.</param>
    /// <param name="mode">The selected tenancy mode.</param>
    public TenancySettings(
        bool? enabled = null,
        string? mode = null)
    {
        Enabled = enabled;
        Mode = string.IsNullOrWhiteSpace(mode) ? null : mode.Trim();
    }

    /// <summary>
    /// Gets a value indicating whether multi-tenancy was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the selected tenancy mode.
    /// </summary>
    public string? Mode { get; }

    /// <summary>
    /// Gets a value indicating whether any tenancy settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        Mode is not null;

    /// <summary>
    /// Reads tenancy settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed tenancy settings.</returns>
    public static TenancySettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Tenancy");

        return new TenancySettings(
            enabled: TryParseBoolean(section["Enabled"]),
            mode: section["Mode"]);
    }

    private static bool? TryParseBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return bool.TryParse(value.Trim(), out var parsed)
            ? parsed
            : null;
    }
}
