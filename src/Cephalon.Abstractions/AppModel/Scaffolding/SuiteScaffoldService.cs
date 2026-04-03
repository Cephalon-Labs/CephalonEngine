using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel.Scaffolding;

/// <summary>
/// Describes one service slot inside a suite-level scaffold plan.
/// </summary>
public sealed class SuiteScaffoldService
{
    /// <summary>
    /// Creates a suite-scaffold service description.
    /// </summary>
    /// <param name="id">The stable service-slot identifier.</param>
    /// <param name="displayName">The human-readable service-slot name.</param>
    /// <param name="description">The service-slot description.</param>
    /// <param name="blueprintId">The app blueprint identifier used for the service.</param>
    /// <param name="nameTemplate">The generated app-name template for the service.</param>
    /// <param name="pathTemplate">The generated root-path template for the service.</param>
    /// <param name="dependsOn">The service or shared-project identifiers this service depends on.</param>
    /// <param name="metadata">Optional service metadata.</param>
    [JsonConstructor]
    public SuiteScaffoldService(
        string id,
        string displayName,
        string description,
        string blueprintId,
        string nameTemplate,
        string pathTemplate,
        IReadOnlyList<string>? dependsOn = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Suite scaffold service id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Suite scaffold service display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Suite scaffold service description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(blueprintId))
        {
            throw new ArgumentException("Suite scaffold service blueprint id is required.", nameof(blueprintId));
        }

        if (string.IsNullOrWhiteSpace(nameTemplate))
        {
            throw new ArgumentException("Suite scaffold service name template is required.", nameof(nameTemplate));
        }

        if (string.IsNullOrWhiteSpace(pathTemplate))
        {
            throw new ArgumentException("Suite scaffold service path template is required.", nameof(pathTemplate));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        BlueprintId = blueprintId.Trim();
        NameTemplate = nameTemplate.Trim();
        PathTemplate = pathTemplate.Trim();
        DependsOn = Normalize(dependsOn);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable service-slot identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable service-slot name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the service-slot description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the app blueprint identifier used for the service.
    /// </summary>
    public string BlueprintId { get; }

    /// <summary>
    /// Gets the generated app-name template for the service.
    /// </summary>
    public string NameTemplate { get; }

    /// <summary>
    /// Gets the generated root-path template for the service.
    /// </summary>
    public string PathTemplate { get; }

    /// <summary>
    /// Gets the service or shared-project identifiers this service depends on.
    /// </summary>
    public IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Gets optional service metadata.
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
