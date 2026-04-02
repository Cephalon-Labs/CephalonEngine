namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Describes a module for discovery, ordering, manifest generation, and diagnostics.
/// </summary>
public sealed class ModuleDescriptor
{
    /// <summary>
    /// Creates a module descriptor.
    /// </summary>
    /// <param name="id">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="dependsOn">The module types this module depends on.</param>
    /// <param name="tags">The tags associated with the module.</param>
    /// <param name="version">The declared module version.</param>
    /// <param name="metadata">Optional module metadata.</param>
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

    /// <summary>
    /// Gets the stable module identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable module name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the module description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the module types this module depends on.
    /// </summary>
    public IReadOnlyList<Type> DependsOn { get; }

    /// <summary>
    /// Gets the tags associated with the module.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the declared module version, when one is available.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Gets optional module metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
