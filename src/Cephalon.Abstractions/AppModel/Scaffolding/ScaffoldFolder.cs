namespace Cephalon.Abstractions.AppModel.Scaffolding;

public sealed class ScaffoldFolder
{
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

    public string PathTemplate { get; }

    public string Purpose { get; }

    public string Scope { get; }

    public string? ProjectId { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
