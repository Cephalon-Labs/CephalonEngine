namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes provider-neutral schema registry availability for event serializers.
/// </summary>
/// <remarks>
/// The descriptor is intentionally metadata-only. It lets hosts and modules expose schema registry
/// availability without putting schema lookup, payload serialization, or compatibility validation
/// on the publication hot path.
/// </remarks>
public sealed class EventSchemaRegistryDescriptor
{
    /// <summary>
    /// Creates a new event schema registry descriptor.
    /// </summary>
    /// <param name="id">The stable schema registry identifier used by serializers.</param>
    /// <param name="displayName">The operator-facing schema registry name.</param>
    /// <param name="description">The human-readable schema registry description.</param>
    /// <param name="provider">The provider or product family for the registry.</param>
    /// <param name="endpointKind">The endpoint kind, such as <c>managed</c>, <c>embedded</c>, or <c>external</c>.</param>
    /// <param name="runtimeKind">The runtime implementation kind, such as <c>code-first</c> or <c>provider-managed</c>.</param>
    /// <param name="canReadSchemas">Whether the runtime can read schemas from the registry.</param>
    /// <param name="canWriteSchemas">Whether the runtime can write schemas to the registry.</param>
    /// <param name="validatesCompatibility">Whether the runtime validates schema compatibility.</param>
    /// <param name="supportedFormats">Optional serialization formats supported by the registry.</param>
    /// <param name="tags">Optional tags that classify the registry.</param>
    /// <param name="metadata">Optional schema registry metadata.</param>
    public EventSchemaRegistryDescriptor(
        string id,
        string displayName,
        string description,
        string provider,
        string endpointKind,
        string runtimeKind = "code-first",
        bool canReadSchemas = true,
        bool canWriteSchemas = false,
        bool validatesCompatibility = false,
        IReadOnlyList<string>? supportedFormats = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Event schema registry id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Event schema registry display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Event schema registry description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Event schema registry provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(endpointKind))
        {
            throw new ArgumentException("Event schema registry endpoint kind is required.", nameof(endpointKind));
        }

        if (string.IsNullOrWhiteSpace(runtimeKind))
        {
            throw new ArgumentException("Event schema registry runtime kind is required.", nameof(runtimeKind));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Provider = provider.Trim();
        EndpointKind = endpointKind.Trim();
        RuntimeKind = runtimeKind.Trim();
        CanReadSchemas = canReadSchemas;
        CanWriteSchemas = canWriteSchemas;
        ValidatesCompatibility = validatesCompatibility;
        SupportedFormats = Normalize(supportedFormats);
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
    /// Gets the stable schema registry identifier used by serializers.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the registry.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable registry description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the provider or product family for the registry.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the endpoint kind exposed by the registry.
    /// </summary>
    public string EndpointKind { get; }

    /// <summary>
    /// Gets the registry runtime implementation kind.
    /// </summary>
    public string RuntimeKind { get; }

    /// <summary>
    /// Gets a value indicating whether schemas can be read from the registry.
    /// </summary>
    public bool CanReadSchemas { get; }

    /// <summary>
    /// Gets a value indicating whether schemas can be written to the registry.
    /// </summary>
    public bool CanWriteSchemas { get; }

    /// <summary>
    /// Gets a value indicating whether the registry runtime validates compatibility.
    /// </summary>
    public bool ValidatesCompatibility { get; }

    /// <summary>
    /// Gets the normalized serialization formats supported by the registry.
    /// </summary>
    public IReadOnlyList<string> SupportedFormats { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the registry.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets normalized metadata associated with the registry.
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
