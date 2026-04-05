using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active messaging inputs resolved for a Cephalon app.
/// </summary>
public sealed class MessagingSelection
{
    /// <summary>
    /// Gets an empty messaging-selection instance.
    /// </summary>
    public static MessagingSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingSelection" /> class.
    /// </summary>
    /// <param name="provider">The selected messaging provider or runtime adapter.</param>
    [JsonConstructor]
    public MessagingSelection(string? provider = null)
    {
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
    }

    /// <summary>
    /// Gets the selected messaging provider or runtime adapter.
    /// </summary>
    public string? Provider { get; }

    /// <summary>
    /// Gets a value indicating whether any messaging-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues => Provider is not null;
}
