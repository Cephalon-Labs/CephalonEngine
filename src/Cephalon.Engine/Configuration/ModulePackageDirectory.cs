namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes a directory that should be scanned for Cephalon package manifests.
/// </summary>
public sealed class ModulePackageDirectory
{
    /// <summary>
    /// Gets the default manifest file name expected inside package directories.
    /// </summary>
    public const string DefaultManifestFileName = "cephalon.package.json";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModulePackageDirectory" /> class.
    /// </summary>
    /// <param name="path">The directory path to scan.</param>
    /// <param name="manifestFileName">The manifest file name to look for inside the directory.</param>
    /// <param name="includeSubdirectories">Whether nested directories should also be scanned.</param>
    public ModulePackageDirectory(
        string path,
        string? manifestFileName = null,
        bool includeSubdirectories = true)
    {
        Path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Package directory path is required.", nameof(path))
            : path.Trim();
        ManifestFileName = string.IsNullOrWhiteSpace(manifestFileName)
            ? DefaultManifestFileName
            : manifestFileName.Trim();
        IncludeSubdirectories = includeSubdirectories;
    }

    /// <summary>
    /// Gets the directory path to scan.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the manifest file name to look for inside the directory.
    /// </summary>
    public string ManifestFileName { get; }

    /// <summary>
    /// Gets a value indicating whether nested directories should also be scanned.
    /// </summary>
    public bool IncludeSubdirectories { get; }
}
