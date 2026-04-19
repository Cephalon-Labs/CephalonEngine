using Cephalon.Abstractions.Patterns;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one route-specific strangler-fig migration-policy override.
/// </summary>
public sealed class StranglerFigRoutePolicySettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StranglerFigRoutePolicySettings" /> class.
    /// </summary>
    /// <param name="routeId">The stable strangler-fig route identifier to target.</param>
    /// <param name="target">The route-specific requested target override.</param>
    /// <param name="progressState">The route-specific normalized migration-progress state.</param>
    /// <param name="progressPercent">The route-specific normalized migration-progress percentage.</param>
    /// <param name="notes">Optional operator-facing notes that explain the route-specific override.</param>
    public StranglerFigRoutePolicySettings(
        string routeId,
        StranglerFigTarget? target = null,
        string? progressState = null,
        int? progressPercent = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
        }

        RouteId = routeId.Trim();
        Target = target;
        ProgressState = StranglerFigMigrationConventions.NormalizeOptionalProgressState(
            progressState,
            nameof(ProgressState));
        ProgressPercent = progressPercent is null
            ? null
            : StranglerFigMigrationConventions.NormalizeProgressPercentOrDefault(progressPercent);
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
    }

    /// <summary>
    /// Gets the stable strangler-fig route identifier to target.
    /// </summary>
    public string RouteId { get; }

    /// <summary>
    /// Gets the route-specific requested target override.
    /// </summary>
    public StranglerFigTarget? Target { get; }

    /// <summary>
    /// Gets the route-specific normalized migration-progress state.
    /// </summary>
    public string? ProgressState { get; }

    /// <summary>
    /// Gets the route-specific normalized migration-progress percentage.
    /// </summary>
    public int? ProgressPercent { get; }

    /// <summary>
    /// Gets optional operator-facing notes that explain the route-specific override.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Reads one route-specific strangler-fig migration-policy override from configuration.
    /// </summary>
    /// <param name="section">The configuration section to read.</param>
    /// <returns>The parsed route-policy settings.</returns>
    public static StranglerFigRoutePolicySettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var routeId = ResolveRouteId(section);

        return new StranglerFigRoutePolicySettings(
            routeId: routeId,
            target: StranglerFigMigrationConventions.ParseOptionalTarget(section["Target"], $"Routes:{routeId}:Target"),
            progressState: StranglerFigMigrationConventions.NormalizeOptionalProgressState(
                section["ProgressState"],
                $"Routes:{routeId}:ProgressState"),
            progressPercent: StranglerFigMigrationConventions.ParseOptionalProgressPercent(
                section["ProgressPercent"],
                $"Routes:{routeId}:ProgressPercent"),
            notes: section["Notes"]);
    }

    private static string ResolveRouteId(IConfigurationSection section)
    {
        var configuredRouteId = section["RouteId"];
        if (!string.IsNullOrWhiteSpace(configuredRouteId))
        {
            return configuredRouteId.Trim();
        }

        return int.TryParse(section.Key, out _)
            ? throw new InvalidOperationException(
                $"Strangler-fig migration route entries must declare RouteId when using array-based configuration. Section '{section.Path}' is missing RouteId.")
            : section.Key.Trim();
    }
}
