namespace Cephalon.Abstractions.Patterns;

public sealed class PatternDescriptor
{
    public PatternDescriptor(
        string id,
        string displayName,
        string description,
        PatternKind kind,
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
        Tags = Normalize(tags);
        Requires = Normalize(requires);
        ConflictsWith = Normalize(conflictsWith);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public PatternKind Kind { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Requires { get; }

    public IReadOnlyList<string> ConflictsWith { get; }

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
