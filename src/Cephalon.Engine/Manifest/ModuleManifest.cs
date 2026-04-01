namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes a single module that participates in the built runtime.
/// </summary>
public sealed class ModuleManifest
{
    /// <summary>
    /// Creates a new module manifest entry.
    /// </summary>
    /// <param name="id">The stable module identifier.</param>
    /// <param name="displayName">The operator-facing module name.</param>
    /// <param name="description">A human-readable description of the module's role.</param>
    /// <param name="version">The effective module version.</param>
    /// <param name="assemblyName">The assembly that contains the module implementation.</param>
    /// <param name="typeName">The fully qualified CLR type name for the module implementation.</param>
    /// <param name="dependsOn">The identifiers of modules this module depends on.</param>
    /// <param name="tags">The descriptive tags published by the module descriptor.</param>
    /// <param name="metadata">Additional descriptor metadata published by the module.</param>
    /// <param name="packageId">The package identifier that supplied the module, if it was package-loaded.</param>
    /// <param name="isTrusted">Whether the module is currently considered trusted under the active trust policy.</param>
    public ModuleManifest(
        string id,
        string displayName,
        string description,
        string version,
        string assemblyName,
        string typeName,
        IReadOnlyList<string> dependsOn,
        IReadOnlyList<string> tags,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? packageId = null,
        bool isTrusted = true)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        DependsOn = dependsOn ?? throw new ArgumentNullException(nameof(dependsOn));
        Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        PackageId = string.IsNullOrWhiteSpace(packageId) ? null : packageId.Trim();
        IsTrusted = isTrusted;
    }

    /// <summary>
    /// Gets the stable module identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the module.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the module.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the effective version reported for the module.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the assembly name that contains the module implementation.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the CLR type name that implements the module.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the identifiers of modules this module depends on.
    /// </summary>
    public IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Gets the descriptor tags published by the module.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets arbitrary descriptor metadata published by the module.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the supplying package identifier when the module came from a package load.
    /// </summary>
    public string? PackageId { get; }

    /// <summary>
    /// Gets a value indicating whether the module is trusted by the current trust policy.
    /// </summary>
    public bool IsTrusted { get; }
}
