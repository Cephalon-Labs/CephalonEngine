using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven audit settings for a Cephalon app.
/// </summary>
public sealed class AuditSettings
{
    /// <summary>
    /// Gets an empty audit-settings instance.
    /// </summary>
    public static AuditSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether audit support was explicitly enabled.</param>
    /// <param name="history">The durable audit-history settings resolved for the app.</param>
    public AuditSettings(
        bool? enabled = null,
        AuditHistorySettings? history = null)
    {
        Enabled = enabled;
        History = history ?? AuditHistorySettings.Empty;
    }

    /// <summary>
    /// Gets a value indicating whether audit support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the durable audit-history settings resolved for the app.
    /// </summary>
    public AuditHistorySettings History { get; }

    /// <summary>
    /// Gets a value indicating whether any audit settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        History.HasValues;

    /// <summary>
    /// Reads audit settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed audit settings.</returns>
    public static AuditSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Audit");

        return new AuditSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            history: AuditHistorySettings.FromSection(section.GetSection("History")));
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
