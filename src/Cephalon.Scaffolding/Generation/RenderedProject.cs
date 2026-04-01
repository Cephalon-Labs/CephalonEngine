namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Represents one project produced by scaffold generation.
/// </summary>
public sealed class RenderedProject
{
    /// <summary>
    /// Creates a new rendered project.
    /// </summary>
    /// <param name="key">The unique key of the rendered project instance.</param>
    /// <param name="sourceProjectId">The source scaffold project identifier that produced this instance.</param>
    /// <param name="name">The generated project name.</param>
    /// <param name="path">The relative scaffold path of the project directory.</param>
    /// <param name="scope">The scaffold scope that produced the project.</param>
    /// <param name="role">The scaffold role of the project.</param>
    /// <param name="template">The scaffold template used to generate the project.</param>
    /// <param name="packages">The package references implied by the scaffold plan.</param>
    /// <param name="projectReferences">The project references implied by the scaffold plan.</param>
    /// <param name="metadata">Additional metadata associated with the project.</param>
    public RenderedProject(
        string key,
        string sourceProjectId,
        string name,
        string path,
        string scope,
        string role,
        string template,
        IReadOnlyList<string> packages,
        IReadOnlyList<string> projectReferences,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        SourceProjectId = sourceProjectId ?? throw new ArgumentNullException(nameof(sourceProjectId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Packages = packages ?? throw new ArgumentNullException(nameof(packages));
        ProjectReferences = projectReferences ?? throw new ArgumentNullException(nameof(projectReferences));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the unique key of the rendered project instance.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the source scaffold project identifier that produced this instance.
    /// </summary>
    public string SourceProjectId { get; }

    /// <summary>
    /// Gets the generated project name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the relative scaffold path of the project directory.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the scaffold scope that produced the project.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the scaffold role of the project.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Gets the scaffold template that was used to generate the project.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Gets the package references implied by the scaffold plan.
    /// </summary>
    public IReadOnlyList<string> Packages { get; }

    /// <summary>
    /// Gets the project references implied by the scaffold plan.
    /// </summary>
    public IReadOnlyList<string> ProjectReferences { get; }

    /// <summary>
    /// Gets additional metadata associated with the project.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
