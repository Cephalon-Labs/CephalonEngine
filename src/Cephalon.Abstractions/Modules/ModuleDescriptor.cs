namespace Cephalon.Abstractions.Modules;

public sealed class ModuleDescriptor
{
    public ModuleDescriptor(
        string id,
        string displayName,
        string description,
        IEnumerable<Type>? dependsOn = null,
        IEnumerable<string>? tags = null,
        string? version = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Module id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Module display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Module description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        DependsOn = dependsOn?.Distinct().ToArray() ?? [];
        Tags = tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Version = string.IsNullOrWhiteSpace(version) ? null : version.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public IReadOnlyList<Type> DependsOn { get; }

    public IReadOnlyList<string> Tags { get; }

    public string? Version { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
