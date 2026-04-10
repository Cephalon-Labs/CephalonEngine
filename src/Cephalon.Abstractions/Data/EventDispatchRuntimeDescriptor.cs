namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one operator-facing durable event-dispatch runtime available to the active Cephalon runtime.
/// </summary>
public sealed class EventDispatchRuntimeDescriptor
{
    /// <summary>
    /// Creates a new event-dispatch runtime descriptor.
    /// </summary>
    /// <param name="id">The stable dispatch-runtime identifier.</param>
    /// <param name="displayName">The operator-facing dispatch-runtime name.</param>
    /// <param name="description">The human-readable dispatch-runtime description.</param>
    /// <param name="metadata">Optional operator-facing metadata for the dispatch runtime.</param>
    /// <param name="outboxIds">
    /// Optional outbox identifiers explicitly owned by the dispatch runtime when execution ownership is bounded to specific outboxes.
    /// </param>
    public EventDispatchRuntimeDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<string>? outboxIds = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Dispatch runtime id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Dispatch runtime display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Dispatch runtime description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        OutboxIds = Normalize(outboxIds);
    }

    /// <summary>
    /// Gets the stable dispatch-runtime identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing dispatch-runtime name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable dispatch-runtime description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets operator-facing metadata for the dispatch runtime.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the outbox identifiers explicitly owned by the dispatch runtime.
    /// </summary>
    public IReadOnlyList<string> OutboxIds { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
