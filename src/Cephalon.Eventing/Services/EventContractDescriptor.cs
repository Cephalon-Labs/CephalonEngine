namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes the provider-neutral contract metadata for one event type and version.
/// </summary>
/// <remarks>
/// The descriptor is intentionally code-first and runtime-neutral. Modules, source generators,
/// and hosts can register event contract truth without putting serializer or handler lookup on
/// the publication hot path.
/// </remarks>
public sealed class EventContractDescriptor
{
    /// <summary>
    /// Creates a new event contract descriptor.
    /// </summary>
    /// <param name="id">The stable contract identifier.</param>
    /// <param name="eventType">The logical event type identifier.</param>
    /// <param name="displayName">The operator-facing event contract name.</param>
    /// <param name="description">The human-readable event contract description.</param>
    /// <param name="version">The event contract version.</param>
    /// <param name="contentType">The wire content type expected for the event payload.</param>
    /// <param name="serializerId">The provider-neutral serializer identifier selected by the contract.</param>
    /// <param name="envelopeSchema">The event envelope schema identifier used by the contract.</param>
    /// <param name="compatibilityPolicy">The compatibility policy declared for the contract.</param>
    /// <param name="tags">Optional tags that classify the contract.</param>
    /// <param name="metadata">Optional contract metadata.</param>
    public EventContractDescriptor(
        string id,
        string eventType,
        string displayName,
        string description,
        string version,
        string contentType,
        string serializerId,
        string envelopeSchema = "cephalon.event-envelope.v1",
        string compatibilityPolicy = "backward-compatible",
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Event contract id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event contract event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Event contract display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Event contract description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Event contract version is required.", nameof(version));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Event contract content type is required.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(serializerId))
        {
            throw new ArgumentException("Event contract serializer id is required.", nameof(serializerId));
        }

        if (string.IsNullOrWhiteSpace(envelopeSchema))
        {
            throw new ArgumentException("Event contract envelope schema is required.", nameof(envelopeSchema));
        }

        if (string.IsNullOrWhiteSpace(compatibilityPolicy))
        {
            throw new ArgumentException("Event contract compatibility policy is required.", nameof(compatibilityPolicy));
        }

        Id = id.Trim();
        EventType = eventType.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Version = version.Trim();
        ContentType = contentType.Trim();
        SerializerId = serializerId.Trim();
        EnvelopeSchema = envelopeSchema.Trim();
        CompatibilityPolicy = compatibilityPolicy.Trim();
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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
    /// Gets the stable contract identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the logical event type identifier.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the operator-facing display name for the contract.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable contract description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the event contract version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the wire content type expected for the event payload.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the provider-neutral serializer identifier selected by the contract.
    /// </summary>
    public string SerializerId { get; }

    /// <summary>
    /// Gets the event envelope schema identifier used by the contract.
    /// </summary>
    public string EnvelopeSchema { get; }

    /// <summary>
    /// Gets the declared compatibility policy for the event contract.
    /// </summary>
    public string CompatibilityPolicy { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the contract.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets normalized metadata associated with the contract.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
