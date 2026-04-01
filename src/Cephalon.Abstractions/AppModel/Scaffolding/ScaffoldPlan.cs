namespace Cephalon.Abstractions.AppModel.Scaffolding;

public sealed class ScaffoldPlan
{
    public ScaffoldPlan(
        string id,
        string displayName,
        string description,
        IReadOnlyList<ScaffoldProject> projects,
        IReadOnlyList<ScaffoldFolder> folders,
        IReadOnlyList<string>? conventions = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Scaffold plan id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Scaffold plan display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Scaffold plan description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));
        Folders = folders ?? throw new ArgumentNullException(nameof(folders));
        Conventions = Normalize(conventions);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        ValidateProjectIdentity(Projects);
        ValidateProjectDependencies(Projects);
        ValidateFolderOwnership(Projects, Folders);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public IReadOnlyList<ScaffoldProject> Projects { get; }

    public IReadOnlyList<ScaffoldFolder> Folders { get; }

    public IReadOnlyList<string> Conventions { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static void ValidateProjectIdentity(IReadOnlyList<ScaffoldProject> projects)
    {
        var duplicate = projects
            .GroupBy(project => project.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Scaffold project '{duplicate.Key}' is registered multiple times.");
        }
    }

    private static void ValidateProjectDependencies(IReadOnlyList<ScaffoldProject> projects)
    {
        var ids = projects
            .Select(project => project.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            foreach (var dependency in project.DependsOn)
            {
                if (!ids.Contains(dependency))
                {
                    throw new InvalidOperationException(
                        $"Scaffold project '{project.Id}' depends on '{dependency}', but that project was not defined.");
                }
            }
        }
    }

    private static void ValidateFolderOwnership(
        IReadOnlyList<ScaffoldProject> projects,
        IReadOnlyList<ScaffoldFolder> folders)
    {
        var ids = projects
            .Select(project => project.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders.Where(folder => folder.ProjectId is not null))
        {
            if (!ids.Contains(folder.ProjectId!))
            {
                throw new InvalidOperationException(
                    $"Scaffold folder '{folder.PathTemplate}' refers to project '{folder.ProjectId}', but that project was not defined.");
            }
        }
    }

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
