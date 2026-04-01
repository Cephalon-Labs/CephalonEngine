using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Adds Cephalon project-configuration conventions to a configuration builder.
/// </summary>
/// <remarks>
/// <para>
/// Cephalon keeps configuration-driven features friendly to large projects by supporting a
/// split-file convention under a project's <c>Configurations</c> folder.
/// </para>
/// <para>
/// The current convention loads root-level <c>Add*.json</c> files first, then loads every
/// <c>{Environment}.json</c> file found under the folder tree. This allows teams to keep
/// concerns such as engine settings, OpenAPI settings, or CORS settings in separate folders
/// without forcing everything into one large <c>appsettings.json</c> file.
/// </para>
/// </remarks>
public static class ProjectConfigurationBuilderExtensions
{
    /// <summary>
    /// Gets the default root folder name used for split project configuration files.
    /// </summary>
    public const string DefaultRootFolderName = "Configurations";

    /// <summary>
    /// Adds Cephalon project-configuration conventions to the supplied configuration builder.
    /// </summary>
    /// <param name="configuration">The configuration builder to extend.</param>
    /// <param name="contentRootPath">The project content root that owns the <c>Configurations</c> folder.</param>
    /// <param name="environmentName">The current host environment name, such as <c>Development</c> or <c>Local</c>.</param>
    /// <param name="rootFolderName">
    /// The split-configuration root folder name. The default value is
    /// <see cref="DefaultRootFolderName" />.
    /// </param>
    /// <returns>The same configuration builder for fluent composition.</returns>
    /// <remarks>
    /// The convention currently loads:
    /// <list type="bullet">
    /// <item><description><c>Configurations/Add*.json</c></description></item>
    /// <item><description><c>Configurations/**/{Environment}.json</c></description></item>
    /// </list>
    /// Existing JSON configuration sources are not duplicated when this method is called more than once.
    /// </remarks>
    public static IConfigurationBuilder AddCephalonProjectConfigurations(
        this IConfigurationBuilder configuration,
        string contentRootPath,
        string environmentName,
        string rootFolderName = DefaultRootFolderName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolderName);

        var configurationRootPath = Path.Combine(contentRootPath, rootFolderName);
        if (!Directory.Exists(configurationRootPath))
        {
            return configuration;
        }

        var fileProvider = new PhysicalFileProvider(contentRootPath);
        foreach (var relativePath in EnumerateConventionFiles(contentRootPath, configurationRootPath, environmentName))
        {
            if (HasJsonSource(configuration, relativePath))
            {
                continue;
            }

            configuration.AddJsonFile(
                provider: fileProvider,
                path: relativePath,
                optional: true,
                reloadOnChange: true);
        }

        return configuration;
    }

    private static IEnumerable<string> EnumerateConventionFiles(
        string contentRootPath,
        string configurationRootPath,
        string environmentName)
    {
        var addFiles = Directory
            .EnumerateFiles(configurationRootPath, "Add*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);

        var environmentFiles = Directory
            .EnumerateFiles(configurationRootPath, $"{environmentName}.json", SearchOption.AllDirectories)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var path in addFiles.Concat(environmentFiles))
        {
            yield return NormalizePath(Path.GetRelativePath(contentRootPath, path));
        }
    }

    private static bool HasJsonSource(IConfigurationBuilder configuration, string relativePath)
    {
        return configuration.Sources
            .OfType<JsonConfigurationSource>()
            .Any(source => string.Equals(
                NormalizePath(source.Path),
                NormalizePath(relativePath),
                StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Replace('\\', '/');
    }
}
