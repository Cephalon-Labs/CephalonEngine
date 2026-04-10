using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the durable audit-history export inputs resolved for a Cephalon app.
/// </summary>
public sealed class AuditHistoryExportSelection
{
    /// <summary>
    /// Gets an empty audit-history export-selection instance.
    /// </summary>
    public static AuditHistoryExportSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditHistoryExportSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether audit-history export was explicitly enabled.</param>
    /// <param name="maxEntries">The configured maximum number of entries that one export may stream.</param>
    [JsonConstructor]
    public AuditHistoryExportSelection(
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
    /// Gets a value indicating whether any audit-history export inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        MaxEntries.HasValue;
}
