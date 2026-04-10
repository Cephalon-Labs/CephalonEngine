using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active audit and history inputs resolved for a Cephalon app.
/// </summary>
public sealed class AuditSelection
{
    /// <summary>
    /// Gets an empty audit-selection instance.
    /// </summary>
    public static AuditSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditSelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether audit support was explicitly enabled.</param>
    /// <param name="history">The durable audit-history inputs resolved for the app.</param>
    [JsonConstructor]
    public AuditSelection(
        bool? enabled = null,
        AuditHistorySelection? history = null)
    {
        Enabled = enabled;
        History = history ?? AuditHistorySelection.Empty;
    }

    /// <summary>
    /// Gets a value indicating whether audit support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the durable audit-history inputs resolved for the app.
    /// </summary>
    public AuditHistorySelection History { get; }

    /// <summary>
    /// Gets a value indicating whether any audit-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        History.HasValues;
}
