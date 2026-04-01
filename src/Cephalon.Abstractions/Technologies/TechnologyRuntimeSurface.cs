using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one operator-facing runtime surface exposed by an active technology pack.
/// </summary>
public sealed class TechnologyRuntimeSurface
{
    /// <summary>
    /// Creates a new technology runtime surface.
    /// </summary>
    /// <param name="technologyId">The owning technology identifier.</param>
    /// <param name="surfaceId">The stable surface identifier within that technology.</param>
    /// <param name="displayName">The operator-facing display name for the surface.</param>
    /// <param name="description">A human-readable description of the surface.</param>
    /// <param name="entries">The entries currently projected by the surface.</param>
    [JsonConstructor]
    public TechnologyRuntimeSurface(
        string technologyId,
        string surfaceId,
        string displayName,
        string description,
        IReadOnlyList<TechnologyRuntimeEntry>? entries = null)
    {
        if (string.IsNullOrWhiteSpace(technologyId))
        {
            throw new ArgumentException("Technology id is required.", nameof(technologyId));
        }

        if (string.IsNullOrWhiteSpace(surfaceId))
        {
            throw new ArgumentException("Surface id is required.", nameof(surfaceId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Surface display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Surface description is required.", nameof(description));
        }

        TechnologyId = technologyId.Trim();
        SurfaceId = surfaceId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Entries = entries ?? Array.Empty<TechnologyRuntimeEntry>();
    }

    /// <summary>
    /// Gets the identifier of the technology profile that owns this surface.
    /// </summary>
    public string TechnologyId { get; }

    /// <summary>
    /// Gets the stable identifier of this surface within the owning technology.
    /// </summary>
    public string SurfaceId { get; }

    /// <summary>
    /// Gets the operator-facing display name for the surface.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the surface.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the entries currently projected by this surface.
    /// </summary>
    public IReadOnlyList<TechnologyRuntimeEntry> Entries { get; }
}
