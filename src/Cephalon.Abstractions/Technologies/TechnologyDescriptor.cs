namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one technology profile that can be activated for an app.
/// </summary>
public sealed class TechnologyDescriptor
{
    /// <summary>
    /// Creates a technology descriptor.
    /// </summary>
    /// <param name="id">The stable technology identifier.</param>
    /// <param name="displayName">The human-readable technology name.</param>
    /// <param name="description">The technology description.</param>
    /// <param name="kind">The category of the technology.</param>
    /// <param name="aliases">Optional aliases that can resolve to the same technology.</param>
    /// <param name="tags">The tags associated with the technology.</param>
    /// <param name="requiresPatterns">The pattern identifiers required by the technology.</param>
    /// <param name="requiresTransports">The transport identifiers required by the technology.</param>
    /// <param name="requiresTechnologies">The technology identifiers required by the technology.</param>
    /// <param name="conflictsWith">The technology identifiers that conflict with the technology.</param>
    /// <param name="packageHints">The companion-package hints associated with the technology.</param>
    /// <param name="guidance">The guidance entries associated with the technology.</param>
    /// <param name="metadata">Optional technology metadata.</param>
    public TechnologyDescriptor(
        string id,
        string displayName,
        string description,
        TechnologyKind kind,
        IReadOnlyList<string>? aliases = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? requiresPatterns = null,
        IReadOnlyList<string>? requiresTransports = null,
        IReadOnlyList<string>? requiresTechnologies = null,
        IReadOnlyList<string>? conflictsWith = null,
        IReadOnlyList<string>? packageHints = null,
        IReadOnlyList<string>? guidance = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Technology id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Technology display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Technology description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Kind = kind;
        Aliases = Normalize(aliases);
        Tags = Normalize(tags);
        RequiresPatterns = Normalize(requiresPatterns);
        RequiresTransports = Normalize(requiresTransports);
        RequiresTechnologies = Normalize(requiresTechnologies);
        ConflictsWith = Normalize(conflictsWith);
        PackageHints = Normalize(packageHints);
        Guidance = Normalize(guidance);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable technology identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable technology name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the technology description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the category of the technology.
    /// </summary>
    public TechnologyKind Kind { get; }

    /// <summary>
    /// Gets optional aliases that can resolve to the same technology.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Gets the tags associated with the technology.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the pattern identifiers required by the technology.
    /// </summary>
    public IReadOnlyList<string> RequiresPatterns { get; }

    /// <summary>
    /// Gets the transport identifiers required by the technology.
    /// </summary>
    public IReadOnlyList<string> RequiresTransports { get; }

    /// <summary>
    /// Gets the technology identifiers required by the technology.
    /// </summary>
    public IReadOnlyList<string> RequiresTechnologies { get; }

    /// <summary>
    /// Gets the technology identifiers that conflict with the technology.
    /// </summary>
    public IReadOnlyList<string> ConflictsWith { get; }

    /// <summary>
    /// Gets the companion-package hints associated with the technology.
    /// </summary>
    public IReadOnlyList<string> PackageHints { get; }

    /// <summary>
    /// Gets the guidance entries associated with the technology.
    /// </summary>
    public IReadOnlyList<string> Guidance { get; }

    /// <summary>
    /// Gets optional technology metadata.
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
