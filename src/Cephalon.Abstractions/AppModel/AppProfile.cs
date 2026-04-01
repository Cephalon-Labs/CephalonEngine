using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

public sealed class AppProfile
{
    public AppProfile(
        string blueprintId,
        string blueprintDisplayName,
        string blueprintDescription,
        IReadOnlyList<PatternDescriptor> patterns,
        IReadOnlyList<TechnologyDescriptor>? technologies = null,
        IReadOnlyList<TransportDescriptor>? transports = null)
        : this(
            blueprintId,
            blueprintDisplayName,
            blueprintDescription,
            patterns,
            scaffold: null,
            technologies: technologies,
            transports)
    {
    }

    [JsonConstructor]
    public AppProfile(
        string blueprintId,
        string blueprintDisplayName,
        string blueprintDescription,
        IReadOnlyList<PatternDescriptor> patterns,
        ScaffoldPlan? scaffold,
        IReadOnlyList<TechnologyDescriptor>? technologies = null,
        IReadOnlyList<TransportDescriptor>? transports = null)
    {
        if (string.IsNullOrWhiteSpace(blueprintId))
        {
            throw new ArgumentException("Blueprint id is required.", nameof(blueprintId));
        }

        if (string.IsNullOrWhiteSpace(blueprintDisplayName))
        {
            throw new ArgumentException("Blueprint display name is required.", nameof(blueprintDisplayName));
        }

        if (string.IsNullOrWhiteSpace(blueprintDescription))
        {
            throw new ArgumentException("Blueprint description is required.", nameof(blueprintDescription));
        }

        BlueprintId = blueprintId.Trim();
        BlueprintDisplayName = blueprintDisplayName.Trim();
        BlueprintDescription = blueprintDescription.Trim();
        Patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
        Scaffold = scaffold;
        Technologies = technologies ?? [];
        Transports = transports ?? [];
    }

    public string BlueprintId { get; }

    public string BlueprintDisplayName { get; }

    public string BlueprintDescription { get; }

    public IReadOnlyList<PatternDescriptor> Patterns { get; }

    public ScaffoldPlan? Scaffold { get; }

    public IReadOnlyList<TechnologyDescriptor> Technologies { get; }

    public IReadOnlyList<TransportDescriptor> Transports { get; }
}
