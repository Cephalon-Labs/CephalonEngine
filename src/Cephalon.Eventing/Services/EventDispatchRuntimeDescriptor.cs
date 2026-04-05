namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes one operator-facing durable dispatch runtime available to the active eventing technology.
/// </summary>
/// <param name="id">The stable dispatch-runtime identifier.</param>
/// <param name="displayName">The operator-facing dispatch-runtime name.</param>
/// <param name="description">The human-readable dispatch-runtime description.</param>
/// <param name="metadata">Optional operator-facing metadata for the dispatch runtime.</param>
public sealed class EventDispatchRuntimeDescriptor(
    string id,
    string displayName,
    string description,
    IReadOnlyDictionary<string, string>? metadata = null)
{
    /// <summary>
    /// Gets the stable dispatch-runtime identifier.
    /// </summary>
    public string Id { get; } = string.IsNullOrWhiteSpace(id)
        ? throw new ArgumentException("Dispatch runtime id is required.", nameof(id))
        : id.Trim();

    /// <summary>
    /// Gets the operator-facing dispatch-runtime name.
    /// </summary>
    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("Dispatch runtime display name is required.", nameof(displayName))
        : displayName.Trim();

    /// <summary>
    /// Gets the human-readable dispatch-runtime description.
    /// </summary>
    public string Description { get; } = string.IsNullOrWhiteSpace(description)
        ? throw new ArgumentException("Dispatch runtime description is required.", nameof(description))
        : description.Trim();

    /// <summary>
    /// Gets operator-facing metadata for the dispatch runtime.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata is null
        ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
}
