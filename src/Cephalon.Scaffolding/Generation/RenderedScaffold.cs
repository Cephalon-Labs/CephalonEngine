using Cephalon.Abstractions.AppModel;

namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Represents the fully rendered output of a scaffold generation request.
/// </summary>
public sealed class RenderedScaffold
{
    /// <summary>
    /// Creates a new rendered scaffold.
    /// </summary>
    /// <param name="appProfile">The application profile used to drive generation.</param>
    /// <param name="request">The original scaffold request.</param>
    /// <param name="projects">The rendered projects.</param>
    /// <param name="folders">The rendered folders.</param>
    /// <param name="files">The rendered files.</param>
    public RenderedScaffold(
        AppProfile appProfile,
        ScaffoldRequest request,
        IReadOnlyList<RenderedProject> projects,
        IReadOnlyList<RenderedFolder> folders,
        IReadOnlyList<RenderedFile> files)
    {
        AppProfile = appProfile ?? throw new ArgumentNullException(nameof(appProfile));
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));
        Folders = folders ?? throw new ArgumentNullException(nameof(folders));
        Files = files ?? throw new ArgumentNullException(nameof(files));
    }

    /// <summary>
    /// Gets the application profile used to drive generation.
    /// </summary>
    public AppProfile AppProfile { get; }

    /// <summary>
    /// Gets the original scaffold request.
    /// </summary>
    public ScaffoldRequest Request { get; }

    /// <summary>
    /// Gets the rendered projects.
    /// </summary>
    public IReadOnlyList<RenderedProject> Projects { get; }

    /// <summary>
    /// Gets the rendered folders.
    /// </summary>
    public IReadOnlyList<RenderedFolder> Folders { get; }

    /// <summary>
    /// Gets the rendered files.
    /// </summary>
    public IReadOnlyList<RenderedFile> Files { get; }
}
