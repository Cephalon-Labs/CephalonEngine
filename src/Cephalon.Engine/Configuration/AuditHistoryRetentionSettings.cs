using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven retention settings for durable audit history.
/// </summary>
public sealed class AuditHistoryRetentionSettings
{
    /// <summary>
    /// Gets the default delete-batch size used by the engine-owned retention baseline.
    /// </summary>
    public const int DefaultDeleteBatchSize = 500;

    /// <summary>
    /// Gets an empty audit-history retention settings instance.
    /// </summary>
    public static AuditHistoryRetentionSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditHistoryRetentionSettings" /> class.
    /// </summary>
    /// <param name="enabled">Whether retention was explicitly enabled.</param>
    /// <param name="maxAgeDays">The maximum age, in days, to retain durable audit rows.</param>
    /// <param name="deleteBatchSize">The maximum number of rows deleted per retention batch.</param>
    /// <param name="applyOnStartup">Whether one retention pass should run during host startup.</param>
    /// <param name="runIntervalMinutes">The optional recurring retention interval in minutes.</param>
    public AuditHistoryRetentionSettings(
        bool? enabled = null,
        int? maxAgeDays = null,
        int? deleteBatchSize = null,
        bool? applyOnStartup = null,
        int? runIntervalMinutes = null)
    {
        Enabled = enabled;
        MaxAgeDays = NormalizePositiveInt(maxAgeDays);
        DeleteBatchSize = NormalizePositiveInt(deleteBatchSize);
        ApplyOnStartup = applyOnStartup;
        RunIntervalMinutes = NormalizePositiveInt(runIntervalMinutes);
    }

    /// <summary>
    /// Gets a value indicating whether retention was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the maximum age, in days, to retain durable audit rows.
    /// </summary>
    public int? MaxAgeDays { get; }

    /// <summary>
    /// Gets the maximum number of rows deleted per retention batch.
    /// </summary>
    public int? DeleteBatchSize { get; }

    /// <summary>
    /// Gets a value indicating whether one retention pass should run during host startup.
    /// </summary>
    public bool? ApplyOnStartup { get; }

    /// <summary>
    /// Gets the optional recurring retention interval in minutes.
    /// </summary>
    public int? RunIntervalMinutes { get; }

    /// <summary>
    /// Gets a value indicating whether any retention settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxAgeDays.HasValue ||
        DeleteBatchSize.HasValue ||
        ApplyOnStartup.HasValue ||
        RunIntervalMinutes.HasValue;

    /// <summary>
    /// Reads retention settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The retention configuration section to read.</param>
    /// <returns>The parsed retention settings.</returns>
    public static AuditHistoryRetentionSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        return new AuditHistoryRetentionSettings(
            enabled: TryParseBoolean(section["Enabled"]),
            maxAgeDays: TryParsePositiveInt(section["MaxAgeDays"]),
            deleteBatchSize: TryParsePositiveInt(section["DeleteBatchSize"]),
            applyOnStartup: TryParseBoolean(section["ApplyOnStartup"]),
            runIntervalMinutes: TryParsePositiveInt(section["RunIntervalMinutes"]));
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

    private static int? TryParsePositiveInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value.Trim(), out var parsed) && parsed > 0
            ? parsed
            : null;
    }

    private static int? NormalizePositiveInt(int? value)
    {
        return value is > 0 ? value : null;
    }
}
