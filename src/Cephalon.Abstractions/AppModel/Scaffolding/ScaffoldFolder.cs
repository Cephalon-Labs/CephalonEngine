namespace Cephalon.Abstractions.AppModel.Scaffolding;

/// <summary>
/// Describes a folder that should exist in a scaffolded app shape.
/// </summary>
public sealed class ScaffoldFolder
{
    /// <summary>
    /// Creates a scaffold-folder description.
    /// </summary>
    /// <param name="pathTemplate">The folder path template.</param>
    /// <param name="purpose">The human-readable folder purpose.</param>
    /// <param name="scope">The scaffold scope that owns the folder.</param>
    /// <param name="projectId">The owning project identifier when the folder belongs to a project.</param>
    /// <param name="metadata">Optional folder metadata.</param>
    public ScaffoldFolder(
        string pathTemplate,
        string purpose,
        string scope,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(pathTemplate))
        {
            throw new ArgumentException("Scaffold folder path template is required.", nameof(pathTemplate));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Scaffold folder purpose is required.", nameof(purpose));
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scaffold folder scope is required.", nameof(scope));
        }

        PathTemplate = pathTemplate.Trim();
        Purpose = purpose.Trim();
        Scope = scope.Trim();
        ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the folder path template.
    /// </summary>
    public string PathTemplate { get; }

    /// <summary>
    /// Gets the human-readable purpose of the folder.
    /// </summary>
    public string Purpose { get; }

    /// <summary>
    /// Gets the scaffold scope that owns the folder.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the owning project identifier when the folder belongs to a project.
    /// </summary>
    public string? ProjectId { get; }

    /// <summary>
    /// Gets optional folder metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
