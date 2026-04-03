using Cephalon.Abstractions.AppModel.Scaffolding;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes a suite-level Cephalon blueprint composed from existing app-level contracts.
/// </summary>
public sealed class SuiteBlueprint
{
    /// <summary>
    /// Creates a suite blueprint.
    /// </summary>
    /// <param name="id">The stable suite-blueprint identifier.</param>
    /// <param name="displayName">The human-readable suite-blueprint name.</param>
    /// <param name="description">The suite-blueprint description.</param>
    /// <param name="scaffold">The suite-scaffold plan associated with the suite blueprint.</param>
    /// <param name="metadata">Optional suite-blueprint metadata.</param>
    [JsonConstructor]
    public SuiteBlueprint(
        string id,
        string displayName,
        string description,
        SuiteScaffoldPlan scaffold,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Suite blueprint id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Suite blueprint display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Suite blueprint description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Scaffold = scaffold ?? throw new ArgumentNullException(nameof(scaffold));
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable suite-blueprint identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable suite-blueprint name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the suite-blueprint description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the suite-scaffold plan associated with the suite blueprint.
    /// </summary>
    public SuiteScaffoldPlan Scaffold { get; }

    /// <summary>
    /// Gets additional suite-blueprint metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
