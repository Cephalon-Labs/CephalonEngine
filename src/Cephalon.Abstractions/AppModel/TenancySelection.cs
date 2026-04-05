using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active multi-tenancy inputs resolved for a Cephalon app.
/// </summary>
public sealed class TenancySelection
{
    /// <summary>
    /// Gets an empty tenancy-selection instance.
    /// </summary>
    public static TenancySelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancySelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether multi-tenancy was explicitly enabled.</param>
    /// <param name="mode">The selected tenancy mode.</param>
    [JsonConstructor]
    public TenancySelection(
        bool? enabled = null,
        string? mode = null)
    {
        Enabled = enabled;
        Mode = string.IsNullOrWhiteSpace(mode) ? null : mode.Trim();
    }

    /// <summary>
    /// Gets a value indicating whether multi-tenancy was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the selected tenancy mode.
    /// </summary>
    public string? Mode { get; }

    /// <summary>
    /// Gets a value indicating whether any tenancy-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        Mode is not null;
}
