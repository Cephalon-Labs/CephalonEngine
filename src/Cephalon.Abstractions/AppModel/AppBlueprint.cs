using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.AppModel.Scaffolding;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes a shipped Cephalon blueprint together with its baseline patterns and scaffold shape.
/// </summary>
public sealed class AppBlueprint
{
    /// <summary>
    /// Creates a blueprint without scaffold metadata.
    /// </summary>
    /// <param name="id">The stable blueprint identifier.</param>
    /// <param name="displayName">The human-readable blueprint name.</param>
    /// <param name="description">The blueprint description.</param>
    /// <param name="patterns">The baseline patterns implied by the blueprint.</param>
    /// <param name="metadata">Optional blueprint metadata.</param>
    public AppBlueprint(
        string id,
        string displayName,
        string description,
        IReadOnlyList<PatternDescriptor> patterns,
        IReadOnlyDictionary<string, string>? metadata = null)
        : this(id, displayName, description, patterns, scaffold: null, metadata)
    {
    }

    /// <summary>
    /// Creates a blueprint with optional scaffold metadata.
    /// </summary>
    /// <param name="id">The stable blueprint identifier.</param>
    /// <param name="displayName">The human-readable blueprint name.</param>
    /// <param name="description">The blueprint description.</param>
    /// <param name="patterns">The baseline patterns implied by the blueprint.</param>
    /// <param name="scaffold">The scaffold plan associated with the blueprint.</param>
    /// <param name="metadata">Optional blueprint metadata.</param>
    [JsonConstructor]
    public AppBlueprint(
        string id,
        string displayName,
        string description,
        IReadOnlyList<PatternDescriptor> patterns,
        ScaffoldPlan? scaffold,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Blueprint id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Blueprint display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Blueprint description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
        Scaffold = scaffold;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        ValidatePatternIdentity(Patterns);
    }

    /// <summary>
    /// Gets the stable blueprint identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable blueprint name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the blueprint description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the baseline patterns implied by the blueprint.
    /// </summary>
    public IReadOnlyList<PatternDescriptor> Patterns { get; }

    /// <summary>
    /// Gets the scaffold plan associated with the blueprint, when one is defined.
    /// </summary>
    public ScaffoldPlan? Scaffold { get; }

    /// <summary>
    /// Gets additional blueprint metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static void ValidatePatternIdentity(IReadOnlyList<PatternDescriptor> patterns)
    {
        var duplicate = patterns
            .GroupBy(pattern => pattern.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Blueprint pattern '{duplicate.Key}' is registered multiple times.");
        }
    }
}
