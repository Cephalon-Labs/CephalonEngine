namespace Cephalon.Engine.Configuration;

public sealed class ModulePackageReference
{
    public const string AssemblyPathKind = "assembly-path";
    public const string ManifestFileKind = "manifest-file";
    public const string DirectoryManifestKind = "directory-manifest";

    public ModulePackageReference(string path, string? id = null, string? kind = null)
    {
        Path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Package path is required.", nameof(path))
            : path.Trim();
        Id = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        Kind = NormalizeKind(kind);
    }

    public string? Id { get; }

    public string Path { get; }

    public string Kind { get; }

    public bool IsAssemblyPath => string.Equals(Kind, AssemblyPathKind, StringComparison.OrdinalIgnoreCase);

    public bool IsManifestFile =>
        string.Equals(Kind, ManifestFileKind, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Kind, DirectoryManifestKind, StringComparison.OrdinalIgnoreCase);

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
