namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Represents one folder produced by scaffold generation.
/// </summary>
public sealed class RenderedFolder
{
    /// <summary>
    /// Creates a new rendered folder.
    /// </summary>
    /// <param name="path">The relative scaffold path of the folder.</param>
    /// <param name="purpose">The descriptive purpose of the folder.</param>
    /// <param name="scope">The scaffold scope that produced the folder.</param>
    /// <param name="projectKey">The owning rendered project key, if the folder belongs to a project.</param>
    /// <param name="metadata">Additional metadata associated with the folder.</param>
    public RenderedFolder(
        string path,
        string purpose,
        string scope,
        string? projectKey = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Purpose = purpose ?? throw new ArgumentNullException(nameof(purpose));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        ProjectKey = projectKey;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the relative scaffold path of the folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the descriptive purpose of the folder.
    /// </summary>
    public string Purpose { get; }

    /// <summary>
    /// Gets the scaffold scope that produced the folder.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the owning rendered project key when the folder belongs to a rendered project.
    /// </summary>
    public string? ProjectKey { get; }

    /// <summary>
    /// Gets additional metadata associated with the folder.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
