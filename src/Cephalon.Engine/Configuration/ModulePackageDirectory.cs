namespace Cephalon.Engine.Configuration;

public sealed class ModulePackageDirectory
{
    public const string DefaultManifestFileName = "cephalon.package.json";

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

    public string Path { get; }

    public string ManifestFileName { get; }

    public bool IncludeSubdirectories { get; }
}
