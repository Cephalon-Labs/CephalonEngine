using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the durable audit-history inputs resolved for a Cephalon app.
/// </summary>
public sealed class AuditHistorySelection
{
    /// <summary>
    /// Gets an empty audit-history selection instance.
    /// </summary>
    public static AuditHistorySelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditHistorySelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether durable audit history was explicitly enabled.</param>
    /// <param name="provider">The selected durable history provider identifier.</param>
    /// <param name="databaseRole">The selected database role used by the durable history path.</param>
    /// <param name="export">The resolved export inputs for durable audit history.</param>
    /// <param name="retention">The resolved retention inputs for durable audit history.</param>
    [JsonConstructor]
    public AuditHistorySelection(
        bool? enabled = null,
        string? provider = null,
        string? databaseRole = null,
        AuditHistoryExportSelection? export = null,
        AuditHistoryRetentionSelection? retention = null)
    {
        Enabled = enabled;
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        DatabaseRole = string.IsNullOrWhiteSpace(databaseRole) ? null : databaseRole.Trim();
        Export = export ?? AuditHistoryExportSelection.Empty;
        Retention = retention ?? AuditHistoryRetentionSelection.Empty;
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
    /// Gets the selected database role used by the durable history path.
    /// </summary>
    public string? DatabaseRole { get; }

    /// <summary>
    /// Gets the resolved export inputs for durable audit history.
    /// </summary>
    public AuditHistoryExportSelection Export { get; }

    /// <summary>
    /// Gets the resolved retention inputs for durable audit history.
    /// </summary>
    public AuditHistoryRetentionSelection Retention { get; }

    /// <summary>
    /// Gets a value indicating whether any durable audit-history inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        Provider is not null ||
        DatabaseRole is not null ||
        Export.HasValues ||
        Retention.HasValues;
}
