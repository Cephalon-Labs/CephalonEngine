using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven identity and authorization settings for a Cephalon app.
/// </summary>
public sealed class IdentitySettings
{
    /// <summary>
    /// Gets an empty identity-settings instance.
    /// </summary>
    public static IdentitySettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentitySettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether identity and authorization support was explicitly enabled.</param>
    /// <param name="authorizationModes">The selected authorization modes.</param>
    public IdentitySettings(
        bool? enabled = null,
        IReadOnlyList<string>? authorizationModes = null)
    {
        Enabled = enabled;
        AuthorizationModes = authorizationModes?
            .Where(mode => !string.IsNullOrWhiteSpace(mode))
            .Select(mode => mode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether identity and authorization support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the selected authorization modes.
    /// </summary>
    public IReadOnlyList<string> AuthorizationModes { get; }

    /// <summary>
    /// Gets a value indicating whether any identity settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        AuthorizationModes.Count > 0;

    /// <summary>
    /// Reads identity settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed identity settings.</returns>
    public static IdentitySettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Identity");

        var authorizationModes = section.GetSection("AuthorizationModes")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        return new IdentitySettings(
            enabled: TryParseBoolean(section["Enabled"]),
            authorizationModes: authorizationModes);
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
