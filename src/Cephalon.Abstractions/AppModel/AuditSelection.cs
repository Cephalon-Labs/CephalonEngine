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
    [JsonConstructor]
    public AuditSelection(bool? enabled = null)
    {
        Enabled = enabled;
    }

    /// <summary>
    /// Gets a value indicating whether audit support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether any audit-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues => Enabled.HasValue;
}
