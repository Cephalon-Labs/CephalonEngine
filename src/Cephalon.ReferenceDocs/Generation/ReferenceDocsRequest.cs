namespace Cephalon.ReferenceDocs.Generation;

/// <summary>
/// Describes the input required to generate Cephalon reference documentation.
/// </summary>
public sealed class ReferenceDocsRequest
{
    /// <summary>
    /// Creates a new reference docs request.
    /// </summary>
    /// <param name="rootPath">The repository root path.</param>
    /// <param name="outputPath">The output directory where reference docs should be written.</param>
    /// <param name="configuration">The build configuration to read from.</param>
    /// <param name="targetFramework">The target framework to read from.</param>
    /// <param name="assemblies">The assemblies to document. When omitted, the generator uses its curated defaults.</param>
    public ReferenceDocsRequest(
        string rootPath,
        string outputPath,
        string configuration = "Debug",
        string targetFramework = "net10.0",
        IReadOnlyList<string>? assemblies = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Repository root path is required.", nameof(rootPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(configuration))
        {
            throw new ArgumentException("Build configuration is required.", nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new ArgumentException("Target framework is required.", nameof(targetFramework));
        }

        RootPath = Path.GetFullPath(rootPath.Trim());
        OutputPath = Path.GetFullPath(outputPath.Trim());
        Configuration = configuration.Trim();
        TargetFramework = targetFramework.Trim();
        Assemblies = assemblies?
            .Where(static assembly => !string.IsNullOrWhiteSpace(assembly))
            .Select(static assembly => assembly.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets the repository root path.
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// Gets the output directory where reference docs should be written.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets the build configuration to read from.
    /// </summary>
    public string Configuration { get; }

    /// <summary>
    /// Gets the target framework to read from.
    /// </summary>
    public string TargetFramework { get; }

    /// <summary>
    /// Gets the assemblies to document. When empty, the generator uses its curated defaults.
    /// </summary>
    public IReadOnlyList<string> Assemblies { get; }
}
