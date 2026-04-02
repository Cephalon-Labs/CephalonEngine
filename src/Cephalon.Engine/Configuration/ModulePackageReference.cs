namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes a package input that can be loaded into the engine runtime.
/// </summary>
public sealed class ModulePackageReference
{
    /// <summary>
    /// Identifies a package input that points directly to an assembly path.
    /// </summary>
    public const string AssemblyPathKind = "assembly-path";

    /// <summary>
    /// Identifies a package input that points directly to a manifest file.
    /// </summary>
    public const string ManifestFileKind = "manifest-file";

    /// <summary>
    /// Identifies a package input that was discovered from a directory manifest scan.
    /// </summary>
    public const string DirectoryManifestKind = "directory-manifest";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModulePackageReference" /> class.
    /// </summary>
    /// <param name="path">The assembly or manifest path.</param>
    /// <param name="id">The optional package identifier override.</param>
    /// <param name="kind">The package input kind.</param>
    public ModulePackageReference(string path, string? id = null, string? kind = null)
    {
        Path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Package path is required.", nameof(path))
            : path.Trim();
        Id = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        Kind = NormalizeKind(kind);
    }

    /// <summary>
    /// Gets the optional package identifier override.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Gets the assembly or manifest path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the normalized package input kind.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets a value indicating whether this reference points directly to an assembly path.
    /// </summary>
    public bool IsAssemblyPath => string.Equals(Kind, AssemblyPathKind, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this reference points to a manifest-backed package input.
    /// </summary>
    public bool IsManifestFile =>
        string.Equals(Kind, ManifestFileKind, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Kind, DirectoryManifestKind, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a manifest-backed package reference.
    /// </summary>
    /// <param name="manifestPath">The manifest path to load.</param>
    /// <param name="id">The optional package identifier override.</param>
    /// <returns>A manifest-backed package reference.</returns>
    public static ModulePackageReference FromManifest(string manifestPath, string? id = null)
    {
        return new ModulePackageReference(manifestPath, id, ManifestFileKind);
    }

    internal static string NormalizeKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return AssemblyPathKind;
        }

        return kind.Trim().ToLowerInvariant() switch
        {
            "assembly-path" or "assemblypath" or "path" or "dll" => AssemblyPathKind,
            "manifest-file" or "manifestfile" or "manifest" => ManifestFileKind,
            "directory-manifest" or "directorymanifest" => DirectoryManifestKind,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Package kind must be assembly-path or manifest-file.")
        };
    }
}
