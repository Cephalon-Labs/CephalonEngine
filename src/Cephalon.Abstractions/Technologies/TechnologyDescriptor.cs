namespace Cephalon.Abstractions.Technologies;

public sealed class TechnologyDescriptor
{
    public TechnologyDescriptor(
        string id,
        string displayName,
        string description,
        TechnologyKind kind,
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

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public TechnologyKind Kind { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> RequiresPatterns { get; }

    public IReadOnlyList<string> RequiresTransports { get; }

    public IReadOnlyList<string> RequiresTechnologies { get; }

    public IReadOnlyList<string> ConflictsWith { get; }

    public IReadOnlyList<string> PackageHints { get; }

    public IReadOnlyList<string> Guidance { get; }

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
