namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes provider-neutral event upcaster availability for one event type version transition.
/// </summary>
/// <remarks>
/// The descriptor is intentionally metadata-only. It lets hosts and modules expose version
/// transition ownership without putting payload deserialization, schema lookup, or upcaster
/// execution on the publication or subscription hot path.
/// </remarks>
public sealed class EventUpcasterDescriptor
{
    /// <summary>
    /// Creates a new event upcaster descriptor.
    /// </summary>
    /// <param name="id">The stable upcaster identifier.</param>
    /// <param name="eventType">The logical event type identifier handled by the upcaster.</param>
    /// <param name="displayName">The operator-facing upcaster name.</param>
    /// <param name="description">The human-readable upcaster description.</param>
    /// <param name="fromVersion">The source event contract version.</param>
    /// <param name="toVersion">The target event contract version.</param>
    /// <param name="runtimeKind">The runtime implementation kind, such as <c>code-first</c> or <c>provider-managed</c>.</param>
    /// <param name="canUpcast">Whether the runtime declares that this transition can be upcast.</param>
    /// <param name="tags">Optional tags that classify the upcaster.</param>
    /// <param name="metadata">Optional upcaster metadata.</param>
    public EventUpcasterDescriptor(
        string id,
        string eventType,
        string displayName,
        string description,
        string fromVersion,
        string toVersion,
        string runtimeKind = "code-first",
        bool canUpcast = true,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Event upcaster id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event upcaster event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Event upcaster display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Event upcaster description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(fromVersion))
        {
            throw new ArgumentException("Event upcaster source version is required.", nameof(fromVersion));
        }

        if (string.IsNullOrWhiteSpace(toVersion))
        {
            throw new ArgumentException("Event upcaster target version is required.", nameof(toVersion));
        }

        if (string.IsNullOrWhiteSpace(runtimeKind))
        {
            throw new ArgumentException("Event upcaster runtime kind is required.", nameof(runtimeKind));
        }

        Id = id.Trim();
        EventType = eventType.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        FromVersion = fromVersion.Trim();
        ToVersion = toVersion.Trim();
        RuntimeKind = runtimeKind.Trim();
        CanUpcast = canUpcast;
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : metadata
                .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
                .GroupBy(static pair => pair.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.Last().Value?.Trim() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable upcaster identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the logical event type identifier handled by the upcaster.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the operator-facing display name for the upcaster.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable upcaster description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the source event contract version.
    /// </summary>
    public string FromVersion { get; }

    /// <summary>
    /// Gets the target event contract version.
    /// </summary>
    public string ToVersion { get; }

    /// <summary>
    /// Gets the upcaster runtime implementation kind.
    /// </summary>
    public string RuntimeKind { get; }

    /// <summary>
    /// Gets a value indicating whether the runtime declares that this transition can be upcast.
    /// </summary>
    public bool CanUpcast { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the upcaster.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets normalized metadata associated with the upcaster.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

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
