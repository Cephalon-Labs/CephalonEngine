namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes one pattern that can shape a Cephalon app.
/// </summary>
public sealed class PatternDescriptor
{
    /// <summary>
    /// Creates a pattern descriptor.
    /// </summary>
    /// <param name="id">The stable pattern identifier.</param>
    /// <param name="displayName">The human-readable pattern name.</param>
    /// <param name="description">The pattern description.</param>
    /// <param name="kind">The category of the pattern.</param>
    /// <param name="aliases">Optional aliases that can resolve to the same pattern.</param>
    /// <param name="tags">The tags associated with the pattern.</param>
    /// <param name="requires">The pattern identifiers required by this pattern.</param>
    /// <param name="conflictsWith">The pattern identifiers that conflict with this pattern.</param>
    /// <param name="metadata">Optional pattern metadata.</param>
    public PatternDescriptor(
        string id,
        string displayName,
        string description,
        PatternKind kind,
        IReadOnlyList<string>? aliases = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? requires = null,
        IReadOnlyList<string>? conflictsWith = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Pattern id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Pattern display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Pattern description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Kind = kind;
        Aliases = Normalize(aliases);
        Tags = Normalize(tags);
        Requires = Normalize(requires);
        ConflictsWith = Normalize(conflictsWith);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable pattern identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable pattern name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the pattern description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the category of the pattern.
    /// </summary>
    public PatternKind Kind { get; }

    /// <summary>
    /// Gets optional aliases that can resolve to the same pattern.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Gets the tags associated with the pattern.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the pattern identifiers required by this pattern.
    /// </summary>
    public IReadOnlyList<string> Requires { get; }

    /// <summary>
    /// Gets the pattern identifiers that conflict with this pattern.
    /// </summary>
    public IReadOnlyList<string> ConflictsWith { get; }

    /// <summary>
    /// Gets optional pattern metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
