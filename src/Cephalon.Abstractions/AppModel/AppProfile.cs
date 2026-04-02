using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the resolved runtime profile selected for a Cephalon app.
/// </summary>
public sealed class AppProfile
{
    /// <summary>
    /// Creates an app profile without scaffold metadata.
    /// </summary>
    /// <param name="blueprintId">The selected blueprint identifier.</param>
    /// <param name="blueprintDisplayName">The selected blueprint display name.</param>
    /// <param name="blueprintDescription">The selected blueprint description.</param>
    /// <param name="patterns">The patterns active for the app.</param>
    /// <param name="technologies">The selected technology profiles.</param>
    /// <param name="transports">The selected transports.</param>
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

    /// <summary>
    /// Creates an app profile with optional scaffold metadata.
    /// </summary>
    /// <param name="blueprintId">The selected blueprint identifier.</param>
    /// <param name="blueprintDisplayName">The selected blueprint display name.</param>
    /// <param name="blueprintDescription">The selected blueprint description.</param>
    /// <param name="patterns">The patterns active for the app.</param>
    /// <param name="scaffold">The scaffold plan associated with the app shape.</param>
    /// <param name="technologies">The selected technology profiles.</param>
    /// <param name="transports">The selected transports.</param>
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

    /// <summary>
    /// Gets the selected blueprint identifier.
    /// </summary>
    public string BlueprintId { get; }

    /// <summary>
    /// Gets the selected blueprint display name.
    /// </summary>
    public string BlueprintDisplayName { get; }

    /// <summary>
    /// Gets the selected blueprint description.
    /// </summary>
    public string BlueprintDescription { get; }

    /// <summary>
    /// Gets the active patterns for the app.
    /// </summary>
    public IReadOnlyList<PatternDescriptor> Patterns { get; }

    /// <summary>
    /// Gets the scaffold plan associated with the app shape, when one is defined.
    /// </summary>
    public ScaffoldPlan? Scaffold { get; }

    /// <summary>
    /// Gets the selected technology profiles.
    /// </summary>
    public IReadOnlyList<TechnologyDescriptor> Technologies { get; }

    /// <summary>
    /// Gets the selected transports.
    /// </summary>
    public IReadOnlyList<TransportDescriptor> Transports { get; }
}
