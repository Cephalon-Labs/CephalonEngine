using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the durable audit-history retention inputs resolved for a Cephalon app.
/// </summary>
public sealed class AuditHistoryRetentionSelection
{
    /// <summary>
    /// Gets an empty audit-history retention selection instance.
    /// </summary>
    public static AuditHistoryRetentionSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditHistoryRetentionSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether retention was explicitly enabled.</param>
    /// <param name="maxAgeDays">The maximum age, in days, to retain durable audit rows.</param>
    /// <param name="deleteBatchSize">The maximum number of rows deleted per retention batch.</param>
    /// <param name="applyOnStartup">Whether one retention pass should run during host startup.</param>
    /// <param name="runIntervalMinutes">The optional recurring retention interval in minutes.</param>
    [JsonConstructor]
    public AuditHistoryRetentionSelection(
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
    /// Gets a value indicating whether any durable audit-history retention inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxAgeDays.HasValue ||
        DeleteBatchSize.HasValue ||
        ApplyOnStartup.HasValue ||
        RunIntervalMinutes.HasValue;

    private static int? NormalizePositiveInt(int? value)
    {
        return value is > 0 ? value : null;
    }
}
