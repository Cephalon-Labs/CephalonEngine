namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes a provider-neutral serializer runtime that can be selected by event contracts.
/// </summary>
/// <remarks>
/// The descriptor is intentionally code-first. It lets modules, source generators, or hosts declare
/// serializer availability without forcing publish or subscription handler selection through string configuration.
/// </remarks>
public sealed class EventSerializerDescriptor
{
    /// <summary>
    /// Creates a new event serializer descriptor.
    /// </summary>
    /// <param name="id">The stable serializer identifier used by event contracts.</param>
    /// <param name="displayName">The operator-facing serializer name.</param>
    /// <param name="description">The human-readable serializer description.</param>
    /// <param name="contentType">The primary wire content type produced or consumed by the serializer.</param>
    /// <param name="format">The provider-neutral serialization format, such as <c>json</c>, <c>protobuf</c>, or <c>avro</c>.</param>
    /// <param name="runtimeKind">The runtime implementation kind, such as <c>source-generated</c> or <c>custom</c>.</param>
    /// <param name="canRead">Whether the serializer can deserialize payloads.</param>
    /// <param name="canWrite">Whether the serializer can serialize payloads.</param>
    /// <param name="requiresSchemaRegistry">Whether the serializer requires a schema registry before it can be used safely.</param>
    /// <param name="schemaRegistryId">The optional schema registry identifier required by the serializer.</param>
    /// <param name="tags">Optional tags that classify the serializer.</param>
    /// <param name="metadata">Optional serializer metadata.</param>
    public EventSerializerDescriptor(
        string id,
        string displayName,
        string description,
        string contentType,
        string format,
        string runtimeKind = "code-first",
        bool canRead = true,
        bool canWrite = true,
        bool requiresSchemaRegistry = false,
        string schemaRegistryId = "",
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Event serializer id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Event serializer display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Event serializer description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Event serializer content type is required.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Event serializer format is required.", nameof(format));
        }

        if (string.IsNullOrWhiteSpace(runtimeKind))
        {
            throw new ArgumentException("Event serializer runtime kind is required.", nameof(runtimeKind));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        ContentType = contentType.Trim();
        Format = format.Trim();
        RuntimeKind = runtimeKind.Trim();
        CanRead = canRead;
        CanWrite = canWrite;
        RequiresSchemaRegistry = requiresSchemaRegistry;
        SchemaRegistryId = schemaRegistryId?.Trim() ?? string.Empty;
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
    /// Gets the stable serializer identifier used by event contracts.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the serializer.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable serializer description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the primary wire content type produced or consumed by the serializer.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the provider-neutral serialization format.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets the serializer runtime implementation kind.
    /// </summary>
    public string RuntimeKind { get; }

    /// <summary>
    /// Gets a value indicating whether the serializer can deserialize payloads.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Gets a value indicating whether the serializer can serialize payloads.
    /// </summary>
    public bool CanWrite { get; }

    /// <summary>
    /// Gets a value indicating whether the serializer needs schema-registry support before use.
    /// </summary>
    public bool RequiresSchemaRegistry { get; }

    /// <summary>
    /// Gets the optional schema registry identifier required by the serializer.
    /// </summary>
    public string SchemaRegistryId { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the serializer.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets normalized metadata associated with the serializer.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
