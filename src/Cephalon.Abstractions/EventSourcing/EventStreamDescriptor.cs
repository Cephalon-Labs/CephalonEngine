namespace Cephalon.Abstractions.EventSourcing;

/// <summary>
/// Describes one logical event stream visible to the current runtime.
/// </summary>
public sealed record class EventStreamDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamDescriptor" /> class.
    /// </summary>
    /// <param name="id">The stable event-stream identifier.</param>
    /// <param name="displayName">The operator-facing event-stream name.</param>
    /// <param name="description">The human-readable event-stream description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the event stream.</param>
    /// <param name="provider">The provider identifier that persists the stream.</param>
    /// <param name="mode">The stream persistence mode. The default is <c>append-only</c>.</param>
    /// <param name="tags">The descriptive tags associated with the stream.</param>
    /// <param name="metadata">The provider-specific metadata associated with the stream.</param>
    public EventStreamDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string provider,
        string mode = "append-only",
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = NormalizeRequired(id, nameof(id));
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        Description = NormalizeRequired(description, nameof(description));
        SourceModuleId = NormalizeRequired(sourceModuleId, nameof(sourceModuleId));
        Provider = NormalizeRequired(provider, nameof(provider));
        Mode = NormalizeRequired(mode, nameof(mode));
        Tags = NormalizeTags(tags);
        Metadata = NormalizeMetadata(metadata);
    }

    /// <summary>
    /// Gets the normalized event-stream identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the normalized operator-facing name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the normalized human-readable description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the normalized source module identifier.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the normalized provider identifier.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the normalized stream persistence mode.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets the normalized descriptive tags associated with the stream.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the normalized provider-specific metadata associated with the stream.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"A value is required for {parameterName}.", parameterName);
        }

        return value.Trim();
    }

    private static string[] NormalizeTags(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static Dictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            normalized[pair.Key.Trim()] = pair.Value.Trim();
        }

        return normalized;
    }
}
