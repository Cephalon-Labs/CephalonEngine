using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven durable audit-history settings for a Cephalon app.
/// </summary>
public sealed class AuditHistorySettings
{
    /// <summary>
    /// Gets the default logical database role used by the durable audit-history path.
    /// </summary>
    public const string DefaultDatabaseRole = "history";

    /// <summary>
    /// Gets an empty audit-history settings instance.
    /// </summary>
    public static AuditHistorySettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditHistorySettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether durable audit history was explicitly enabled.</param>
    /// <param name="provider">The selected durable history provider identifier.</param>
    /// <param name="databaseRole">The selected logical database role for durable history.</param>
    /// <param name="export">The configured export settings for durable history.</param>
    /// <param name="retention">The selected retention settings for durable history.</param>
    public AuditHistorySettings(
        bool? enabled = null,
        string? provider = null,
        string? databaseRole = null,
        AuditHistoryExportSettings? export = null,
        AuditHistoryRetentionSettings? retention = null)
    {
        Enabled = enabled;
        Provider = Normalize(provider);
        DatabaseRole = Normalize(databaseRole);
        Export = export ?? AuditHistoryExportSettings.Empty;
        Retention = retention ?? AuditHistoryRetentionSettings.Empty;
    }

    /// <summary>
    /// Gets a value indicating whether durable audit history was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the selected durable history provider identifier.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets the selected logical database role used by durable history.
    /// </summary>
    public string? DatabaseRole { get; }

    /// <summary>
    /// Gets the configured export settings for durable history.
    /// </summary>
    public AuditHistoryExportSettings Export { get; }

    /// <summary>
    /// Gets the configured retention settings for durable history.
    /// </summary>
    public AuditHistoryRetentionSettings Retention { get; }

    /// <summary>
    /// Gets a value indicating whether any durable audit-history settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        Provider is not null ||
        DatabaseRole is not null ||
        Export.HasValues ||
        Retention.HasValues;

    /// <summary>
    /// Reads durable audit-history settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The audit-history configuration section to read.</param>
    /// <returns>The parsed durable audit-history settings.</returns>
    public static AuditHistorySettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        return new AuditHistorySettings(
            enabled: TryParseBoolean(section["Enabled"]),
            provider: section["Provider"],
            databaseRole: section["DatabaseRole"],
            export: AuditHistoryExportSettings.FromSection(section.GetSection("Export")),
            retention: AuditHistoryRetentionSettings.FromSection(section.GetSection("Retention")));
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
