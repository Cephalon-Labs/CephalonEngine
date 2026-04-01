using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

public sealed class ModuleDiscoverySettings
{
    public static ModuleDiscoverySettings Empty { get; } = new();

    public ModuleDiscoverySettings(
        IReadOnlyList<string>? assemblies = null,
        IReadOnlyList<ModulePackageReference>? packages = null,
        IReadOnlyList<ModulePackageDirectory>? packageDirectories = null)
    {
        Assemblies = assemblies?
            .Where(assembly => !string.IsNullOrWhiteSpace(assembly))
            .Select(assembly => assembly.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Packages = packages?
            .Where(static package => package is not null)
            .ToArray() ?? [];
        PackageDirectories = packageDirectories?
            .Where(static directory => directory is not null)
            .ToArray() ?? [];
    }

    public IReadOnlyList<string> Assemblies { get; }

    public IReadOnlyList<ModulePackageReference> Packages { get; }

    public IReadOnlyList<ModulePackageDirectory> PackageDirectories { get; }

    public bool HasValues => Assemblies.Count > 0 || Packages.Count > 0 || PackageDirectories.Count > 0;

    public static ModuleDiscoverySettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var assemblies = configuration
            .GetSection(sectionPath)
            .GetSection("Discovery")
            .GetSection("Assemblies")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
        var packages = configuration
            .GetSection(sectionPath)
            .GetSection("Discovery")
            .GetSection("Packages")
            .GetChildren()
            .Select(child =>
            {
                var kind = child["Kind"];
                var manifestPath = child["ManifestPath"];
                var path = manifestPath ?? child["Path"] ?? child.Value;
                return string.IsNullOrWhiteSpace(path)
                    ? null
                    : new ModulePackageReference(path, child["Id"], manifestPath is null ? kind : ModulePackageReference.ManifestFileKind);
            })
            .Where(static package => package is not null)
            .Cast<ModulePackageReference>()
            .ToArray();
        var packageDirectories = configuration
            .GetSection(sectionPath)
            .GetSection("Discovery")
            .GetSection("PackageDirectories")
            .GetChildren()
            .Select(child =>
            {
                var path = child["Path"] ?? child.Value;
                return string.IsNullOrWhiteSpace(path)
                    ? null
                    : new ModulePackageDirectory(
                        path,
                        child["ManifestFileName"],
                        ParseIncludeSubdirectories(child["IncludeSubdirectories"]));
            })
            .Where(static directory => directory is not null)
            .Cast<ModulePackageDirectory>()
            .ToArray();

        return new ModuleDiscoverySettings(assemblies, packages, packageDirectories);
    }

    private static bool ParseIncludeSubdirectories(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var includeSubdirectories)
            ? true
            : includeSubdirectories;
    }
}
