using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes how the engine discovers modules from assemblies, package references, and package directories.
/// </summary>
public sealed class ModuleDiscoverySettings
{
    /// <summary>
    /// Gets an empty module discovery settings instance.
    /// </summary>
    public static ModuleDiscoverySettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleDiscoverySettings" /> class.
    /// </summary>
    /// <param name="assemblies">Assembly names or paths to scan for modules.</param>
    /// <param name="packages">Explicit package references to load.</param>
    /// <param name="packageDirectories">Package directories to scan for manifests.</param>
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

    /// <summary>
    /// Gets assembly names or paths to scan for modules.
    /// </summary>
    public IReadOnlyList<string> Assemblies { get; }

    /// <summary>
    /// Gets explicit package references to load.
    /// </summary>
    public IReadOnlyList<ModulePackageReference> Packages { get; }

    /// <summary>
    /// Gets package directories to scan for manifests.
    /// </summary>
    public IReadOnlyList<ModulePackageDirectory> PackageDirectories { get; }

    /// <summary>
    /// Gets a value indicating whether any discovery inputs were explicitly supplied.
    /// </summary>
    public bool HasValues => Assemblies.Count > 0 || Packages.Count > 0 || PackageDirectories.Count > 0;

    /// <summary>
    /// Reads module discovery settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed module discovery settings.</returns>
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
