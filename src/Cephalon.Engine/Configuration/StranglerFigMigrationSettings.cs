using Cephalon.Abstractions.Patterns;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven strangler-fig migration settings for a Cephalon app.
/// </summary>
public sealed class StranglerFigMigrationSettings
{
    /// <summary>
    /// Gets an empty strangler-fig migration-settings instance.
    /// </summary>
    public static StranglerFigMigrationSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StranglerFigMigrationSettings" /> class.
    /// </summary>
    /// <param name="defaultTarget">The default target to request for routes without an explicit route-level override.</param>
    /// <param name="defaultProgressState">The default normalized progress state for routes without an explicit route-level override.</param>
    /// <param name="defaultProgressPercent">The default normalized progress percent for routes without an explicit route-level override.</param>
    /// <param name="routes">The route-specific strangler-fig migration-policy entries.</param>
    public StranglerFigMigrationSettings(
        StranglerFigTarget? defaultTarget = null,
        string? defaultProgressState = null,
        int? defaultProgressPercent = null,
        IReadOnlyList<StranglerFigRoutePolicySettings>? routes = null)
    {
        DefaultTarget = defaultTarget;
        DefaultProgressState = StranglerFigMigrationConventions.NormalizeOptionalProgressState(
            defaultProgressState,
            nameof(DefaultProgressState));
        DefaultProgressPercent = defaultProgressPercent is null
            ? null
            : StranglerFigMigrationConventions.NormalizeProgressPercentOrDefault(defaultProgressPercent);
        Routes = NormalizeRoutes(routes);
    }

    /// <summary>
    /// Gets the default target to request for routes without an explicit route-level override.
    /// </summary>
    public StranglerFigTarget? DefaultTarget { get; }

    /// <summary>
    /// Gets the default normalized progress state for routes without an explicit route-level override.
    /// </summary>
    public string? DefaultProgressState { get; }

    /// <summary>
    /// Gets the default normalized progress percent for routes without an explicit route-level override.
    /// </summary>
    public int? DefaultProgressPercent { get; }

    /// <summary>
    /// Gets the route-specific strangler-fig migration-policy entries.
    /// </summary>
    public IReadOnlyList<StranglerFigRoutePolicySettings> Routes { get; }

    /// <summary>
    /// Gets a value indicating whether any strangler-fig migration settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        DefaultTarget.HasValue ||
        DefaultProgressState is not null ||
        DefaultProgressPercent.HasValue ||
        Routes.Count > 0;

    /// <summary>
    /// Reads strangler-fig migration settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The configuration section that contains the strangler-fig migration settings.</param>
    /// <returns>The parsed strangler-fig migration settings.</returns>
    public static StranglerFigMigrationSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        var routePolicies = section
            .GetSection("Routes")
            .GetChildren()
            .Select(StranglerFigRoutePolicySettings.FromSection)
            .ToArray();

        return new StranglerFigMigrationSettings(
            defaultTarget: StranglerFigMigrationConventions.ParseOptionalTarget(
                section["DefaultTarget"],
                "DefaultTarget"),
            defaultProgressState: StranglerFigMigrationConventions.NormalizeOptionalProgressState(
                section["DefaultProgressState"],
                "DefaultProgressState"),
            defaultProgressPercent: StranglerFigMigrationConventions.ParseOptionalProgressPercent(
                section["DefaultProgressPercent"],
                "DefaultProgressPercent"),
            routes: routePolicies);
    }

    private static StranglerFigRoutePolicySettings[] NormalizeRoutes(
        IReadOnlyList<StranglerFigRoutePolicySettings>? routes)
    {
        if (routes is null || routes.Count == 0)
        {
            return [];
        }

        var normalized = routes
            .Where(static route => route is not null)
            .OrderBy(static route => route.RouteId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicateRouteIds = normalized
            .GroupBy(static route => route.RouteId, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();
        if (duplicateRouteIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate strangler-fig migration route policies are not supported: {string.Join(", ", duplicateRouteIds)}.");
        }

        return normalized;
    }
}
