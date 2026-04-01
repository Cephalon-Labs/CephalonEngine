using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.AppModel.Scaffolding;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

public sealed class AppBlueprint
{
    public AppBlueprint(
        string id,
        string displayName,
        string description,
        IReadOnlyList<PatternDescriptor> patterns,
        IReadOnlyDictionary<string, string>? metadata = null)
        : this(id, displayName, description, patterns, scaffold: null, metadata)
    {
    }

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

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public IReadOnlyList<PatternDescriptor> Patterns { get; }

    public ScaffoldPlan? Scaffold { get; }

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
