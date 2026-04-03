using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel.Scaffolding;

/// <summary>
/// Describes a suite-level scaffold plan for coordinated multi-service Cephalon solutions.
/// </summary>
public sealed class SuiteScaffoldPlan
{
    /// <summary>
    /// Creates a suite-level scaffold plan.
    /// </summary>
    /// <param name="id">The stable suite-scaffold identifier.</param>
    /// <param name="displayName">The human-readable suite-scaffold name.</param>
    /// <param name="description">The suite-scaffold description.</param>
    /// <param name="services">The service slots emitted by the suite scaffold.</param>
    /// <param name="sharedProjects">The shared projects emitted outside individual services.</param>
    /// <param name="sharedFolders">The shared folders emitted outside individual services.</param>
    /// <param name="conventions">The conventions implied by the suite scaffold.</param>
    /// <param name="metadata">Optional suite-scaffold metadata.</param>
    [JsonConstructor]
    public SuiteScaffoldPlan(
        string id,
        string displayName,
        string description,
        IReadOnlyList<SuiteScaffoldService> services,
        IReadOnlyList<ScaffoldProject>? sharedProjects = null,
        IReadOnlyList<ScaffoldFolder>? sharedFolders = null,
        IReadOnlyList<string>? conventions = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Suite scaffold plan id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Suite scaffold plan display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Suite scaffold plan description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Services = services ?? throw new ArgumentNullException(nameof(services));
        SharedProjects = sharedProjects ?? [];
        SharedFolders = sharedFolders ?? [];
        Conventions = Normalize(conventions);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        if (Services.Count == 0)
        {
            throw new InvalidOperationException(
                "Suite scaffold plans must define at least one service slot.");
        }

        ValidateServiceIdentity(Services);
        ValidateProjectIdentity(SharedProjects);
        ValidateDependencyIdentity(Services, SharedProjects);
        ValidateFolderOwnership(SharedProjects, SharedFolders);
    }

    /// <summary>
    /// Gets the stable suite-scaffold identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable suite-scaffold name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the suite-scaffold description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the service slots emitted by the suite scaffold.
    /// </summary>
    public IReadOnlyList<SuiteScaffoldService> Services { get; }

    /// <summary>
    /// Gets the shared projects emitted outside individual services.
    /// </summary>
    public IReadOnlyList<ScaffoldProject> SharedProjects { get; }

    /// <summary>
    /// Gets the shared folders emitted outside individual services.
    /// </summary>
    public IReadOnlyList<ScaffoldFolder> SharedFolders { get; }

    /// <summary>
    /// Gets the conventions implied by the suite scaffold.
    /// </summary>
    public IReadOnlyList<string> Conventions { get; }

    /// <summary>
    /// Gets optional suite-scaffold metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static void ValidateServiceIdentity(IReadOnlyList<SuiteScaffoldService> services)
    {
        var duplicate = services
            .GroupBy(service => service.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Suite scaffold service '{duplicate.Key}' is registered multiple times.");
        }
    }

    private static void ValidateProjectIdentity(IReadOnlyList<ScaffoldProject> projects)
    {
        var duplicate = projects
            .GroupBy(project => project.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Suite scaffold shared project '{duplicate.Key}' is registered multiple times.");
        }
    }

    private static void ValidateDependencyIdentity(
        IReadOnlyList<SuiteScaffoldService> services,
        IReadOnlyList<ScaffoldProject> sharedProjects)
    {
        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in sharedProjects)
        {
            ids.Add(project.Id, $"shared project '{project.Id}'");
        }

        foreach (var service in services)
        {
            if (!ids.TryAdd(service.Id, $"service '{service.Id}'"))
            {
                throw new InvalidOperationException(
                    $"Suite scaffold identity '{service.Id}' is used by both a service and a shared project.");
            }
        }

        foreach (var service in services)
        {
            foreach (var dependency in service.DependsOn)
            {
                if (!ids.ContainsKey(dependency))
                {
                    throw new InvalidOperationException(
                        $"Suite scaffold service '{service.Id}' depends on '{dependency}', but that dependency was not defined.");
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
                    $"Suite scaffold folder '{folder.PathTemplate}' refers to shared project '{folder.ProjectId}', but that project was not defined.");
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
