namespace Cephalon.Abstractions.AppModel.Scaffolding;

/// <summary>
/// Describes one project emitted by a scaffold plan.
/// </summary>
public sealed class ScaffoldProject
{
    /// <summary>
    /// Creates a scaffold-project description.
    /// </summary>
    /// <param name="id">The stable project identifier.</param>
    /// <param name="nameTemplate">The project-name template.</param>
    /// <param name="pathTemplate">The project-path template.</param>
    /// <param name="scope">The scaffold scope that owns the project.</param>
    /// <param name="role">The canonical project role.</param>
    /// <param name="template">The template used to create the project.</param>
    /// <param name="dependsOn">The project identifiers this project depends on.</param>
    /// <param name="packages">The package hints associated with the project.</param>
    /// <param name="metadata">Optional project metadata.</param>
    public ScaffoldProject(
        string id,
        string nameTemplate,
        string pathTemplate,
        string scope,
        string role,
        string template,
        IReadOnlyList<string>? dependsOn = null,
        IReadOnlyList<string>? packages = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Scaffold project id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(nameTemplate))
        {
            throw new ArgumentException("Scaffold project name template is required.", nameof(nameTemplate));
        }

        if (string.IsNullOrWhiteSpace(pathTemplate))
        {
            throw new ArgumentException("Scaffold project path template is required.", nameof(pathTemplate));
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scaffold project scope is required.", nameof(scope));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Scaffold project role is required.", nameof(role));
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Scaffold project template is required.", nameof(template));
        }

        Id = id.Trim();
        NameTemplate = nameTemplate.Trim();
        PathTemplate = pathTemplate.Trim();
        Scope = scope.Trim();
        Role = role.Trim();
        Template = template.Trim();
        DependsOn = Normalize(dependsOn);
        Packages = Normalize(packages);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable project identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the project-name template.
    /// </summary>
    public string NameTemplate { get; }

    /// <summary>
    /// Gets the project-path template.
    /// </summary>
    public string PathTemplate { get; }

    /// <summary>
    /// Gets the scaffold scope that owns the project.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the canonical project role.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Gets the template used to create the project.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Gets the project identifiers this project depends on.
    /// </summary>
    public IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Gets the package hints associated with the project.
    /// </summary>
    public IReadOnlyList<string> Packages { get; }

    /// <summary>
    /// Gets optional project metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
