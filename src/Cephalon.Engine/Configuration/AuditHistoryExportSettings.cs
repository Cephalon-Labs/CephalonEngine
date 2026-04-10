using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven audit-history export settings for a Cephalon app.
/// </summary>
public sealed class AuditHistoryExportSettings
{
    /// <summary>
    /// Gets the default maximum number of entries that one audit-history export may stream.
    /// </summary>
    public const int DefaultMaxEntries = 1000;

    /// <summary>
    /// Gets an empty audit-history export-settings instance.
    /// </summary>
    public static AuditHistoryExportSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditHistoryExportSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether audit-history export was explicitly enabled.</param>
    /// <param name="maxEntries">The configured maximum number of entries that one export may stream.</param>
    public AuditHistoryExportSettings(
        bool? enabled = null,
        int? maxEntries = null)
    {
        Enabled = enabled;
        MaxEntries = maxEntries;
    }

    /// <summary>
    /// Gets a value indicating whether audit-history export was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the configured maximum number of entries that one export may stream.
    /// </summary>
    public int? MaxEntries { get; }

    /// <summary>
    /// Gets a value indicating whether any audit-history export settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxEntries.HasValue;

    /// <summary>
    /// Reads audit-history export settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The audit-history export configuration section to read.</param>
    /// <returns>The parsed audit-history export settings.</returns>
    public static AuditHistoryExportSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        return new AuditHistoryExportSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            maxEntries: TryParseInteger(section["MaxEntries"]));
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

    private static int? TryParseInteger(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value.Trim(), out var parsed)
            ? parsed
            : null;
    }
}
