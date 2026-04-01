namespace Cephalon.Abstractions.AppModel.Scaffolding;

public sealed class ScaffoldProject
{
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

    public string Id { get; }

    public string NameTemplate { get; }

    public string PathTemplate { get; }

    public string Scope { get; }

    public string Role { get; }

    public string Template { get; }

    public IReadOnlyList<string> DependsOn { get; }

    public IReadOnlyList<string> Packages { get; }

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
