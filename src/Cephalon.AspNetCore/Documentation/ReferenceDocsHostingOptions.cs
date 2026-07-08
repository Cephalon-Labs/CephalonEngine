using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Documentation;

/// <summary>
/// Configures how an ASP.NET Core host serves generated Cephalon reference documentation.
/// </summary>
/// <remarks>
/// These options belong to the host layer rather than the engine core because they describe
/// how already-generated static documentation should be exposed over HTTP.
/// </remarks>
public sealed class ReferenceDocsHostingOptions
{
    /// <summary>
    /// Gets the default configuration section used for reference-doc hosting.
    /// </summary>
    public const string SectionName = "ReferenceDocs";

    /// <summary>
    /// Creates reference-doc hosting options with the default hosted-doc route settings.
    /// </summary>
    public ReferenceDocsHostingOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether hosted reference docs should be exposed.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the route prefix where the documentation should be served.
    /// </summary>
    /// <remarks>
    /// The value may be supplied with or without a leading slash. The host normalizes it into a
    /// rooted path such as <c>/reference</c>.
    /// </remarks>
    public string RoutePrefix { get; set; } = "/reference";

    /// <summary>
    /// Gets or sets the directory that contains the generated reference-doc output.
    /// </summary>
    /// <remarks>
    /// Relative paths are resolved against the ASP.NET Core content root.
    /// </remarks>
    public string? DirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the document that should open when a user requests the route prefix itself.
    /// </summary>
    public string DefaultDocument { get; set; } = "browse.html";

    /// <summary>
    /// Binds reference-doc hosting options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">The section path that contains the hosting settings.</param>
    /// <param name="contentRootPath">
    /// The application content root used to normalize relative documentation paths.
    /// </param>
    /// <returns>The bound and normalized hosting options.</returns>
    public static ReferenceDocsHostingOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName,
        string? contentRootPath = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath);
        var routePrefix = section["RoutePrefix"];
        var directoryPath = section["DirectoryPath"];
        var defaultDocument = section["DefaultDocument"];

        var options = new ReferenceDocsHostingOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: false),
            RoutePrefix = string.IsNullOrWhiteSpace(routePrefix) ? "/reference" : routePrefix.Trim(),
            DirectoryPath = NormalizeDirectoryPath(directoryPath, contentRootPath),
            DefaultDocument = string.IsNullOrWhiteSpace(defaultDocument) ? "browse.html" : defaultDocument.Trim()
        };

        if (options.Enabled && string.IsNullOrWhiteSpace(options.DirectoryPath) && !string.IsNullOrWhiteSpace(contentRootPath))
        {
            options.DirectoryPath = Path.GetFullPath(Path.Combine(contentRootPath, "docs", "reference"));
        }

        return options;
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static string? NormalizeDirectoryPath(string? directoryPath, string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return null;
        }

        var normalizedDirectoryPath = NormalizeRelativeDirectorySeparators(directoryPath.Trim());

        return Path.IsPathRooted(normalizedDirectoryPath)
            ? Path.GetFullPath(normalizedDirectoryPath)
            : string.IsNullOrWhiteSpace(contentRootPath)
                ? Path.GetFullPath(normalizedDirectoryPath)
                : Path.GetFullPath(Path.Combine(contentRootPath, normalizedDirectoryPath));
    }

    private static string NormalizeRelativeDirectorySeparators(string directoryPath)
    {
        return Path.IsPathRooted(directoryPath)
            ? directoryPath
            : directoryPath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
    }
}
