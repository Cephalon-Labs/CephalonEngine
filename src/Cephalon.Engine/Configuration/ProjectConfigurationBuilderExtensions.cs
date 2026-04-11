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
/// <c>{Environment}.json</c> file found under the folder tree. These sources are inserted ahead
/// of standard host overrides such as <c>appsettings.json</c>, <c>appsettings.{Environment}.json</c>,
/// user secrets, environment variables, and command-line arguments so projects can keep Cephalon
/// defaults grouped by concern without losing the normal ASP.NET Core and generic-host override path.
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
        var insertionIndex = ResolveInsertionIndex(configuration);
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

            var insertedSource = configuration.Sources[^1];
            configuration.Sources.RemoveAt(configuration.Sources.Count - 1);
            configuration.Sources.Insert(insertionIndex, insertedSource);
            insertionIndex++;
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

    private static int ResolveInsertionIndex(IConfigurationBuilder configuration)
    {
        for (var index = 0; index < configuration.Sources.Count; index++)
        {
            if (IsStandardHostOverrideSource(configuration.Sources[index]))
            {
                return index;
            }
        }

        return configuration.Sources.Count;
    }

    private static bool IsStandardHostOverrideSource(IConfigurationSource source)
    {
        if (source is JsonConfigurationSource jsonSource && IsAppSettingsPath(jsonSource.Path))
        {
            return true;
        }

        var sourceTypeName = source.GetType().Name;
        return sourceTypeName.Contains("UserSecrets", StringComparison.OrdinalIgnoreCase) ||
            sourceTypeName.Contains("EnvironmentVariables", StringComparison.OrdinalIgnoreCase) ||
            sourceTypeName.Contains("CommandLine", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAppSettingsPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fileName = Path.GetFileName(path.Replace('\\', '/'));
        if (string.Equals(fileName, "appsettings.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName.StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Replace('\\', '/');
    }
}
