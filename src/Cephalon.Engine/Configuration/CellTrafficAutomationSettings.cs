using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven cell traffic-automation settings for a Cephalon app.
/// </summary>
public sealed class CellTrafficAutomationSettings
{
    /// <summary>
    /// Gets an empty cell traffic-automation settings instance.
    /// </summary>
    public static CellTrafficAutomationSettings Empty { get; } = new();

    /// <summary>
    /// Creates cell traffic-automation settings.
    /// </summary>
    /// <param name="defaultAutomationMode">The default normalized automation posture for active governed routes.</param>
    /// <param name="defaultTriggerMode">The default normalized trigger posture for active governed routes.</param>
    /// <param name="defaultActionMode">The default normalized action posture for active governed routes.</param>
    /// <param name="defaultMaterializationMode">The default normalized materialization posture for active governed routes.</param>
    /// <param name="routes">The route-specific cell traffic-automation overrides.</param>
    public CellTrafficAutomationSettings(
        string? defaultAutomationMode = null,
        string? defaultTriggerMode = null,
        string? defaultActionMode = null,
        string? defaultMaterializationMode = null,
        IReadOnlyList<CellTrafficAutomationRouteSettings>? routes = null)
    {
        DefaultAutomationMode = NormalizeOptionalToken(defaultAutomationMode);
        DefaultTriggerMode = NormalizeOptionalToken(defaultTriggerMode);
        DefaultActionMode = NormalizeOptionalToken(defaultActionMode);
        DefaultMaterializationMode = NormalizeOptionalToken(defaultMaterializationMode);
        Routes = NormalizeRoutes(routes);
    }

    /// <summary>
    /// Gets the default normalized automation posture for active governed routes.
    /// </summary>
    public string? DefaultAutomationMode { get; }

    /// <summary>
    /// Gets the default normalized trigger posture for active governed routes.
    /// </summary>
    public string? DefaultTriggerMode { get; }

    /// <summary>
    /// Gets the default normalized action posture for active governed routes.
    /// </summary>
    public string? DefaultActionMode { get; }

    /// <summary>
    /// Gets the default normalized materialization posture for active governed routes.
    /// </summary>
    public string? DefaultMaterializationMode { get; }

    /// <summary>
    /// Gets the route-specific cell traffic-automation overrides.
    /// </summary>
    public IReadOnlyList<CellTrafficAutomationRouteSettings> Routes { get; }

    /// <summary>
    /// Gets a value indicating whether any default traffic-automation values were explicitly supplied.
    /// </summary>
    public bool HasDefaultValues =>
        DefaultAutomationMode is not null ||
        DefaultTriggerMode is not null ||
        DefaultActionMode is not null ||
        DefaultMaterializationMode is not null;

    /// <summary>
    /// Gets a value indicating whether any cell traffic-automation settings were explicitly supplied.
    /// </summary>
    public bool HasValues => HasDefaultValues || Routes.Count > 0;

    /// <summary>
    /// Reads cell traffic-automation settings from the supplied configuration section.
    /// </summary>
    /// <param name="section">The configuration section that contains the cell traffic-automation settings.</param>
    /// <returns>The parsed cell traffic-automation settings.</returns>
    public static CellTrafficAutomationSettings FromSection(IConfigurationSection? section)
    {
        if (section is null || !section.Exists())
        {
            return Empty;
        }

        var routeOverrides = section
            .GetSection("Routes")
            .GetChildren()
            .Where(static child => child.Exists())
            .Select(CellTrafficAutomationRouteSettings.FromSection)
            .ToArray();

        return new CellTrafficAutomationSettings(
            defaultAutomationMode: section["DefaultAutomationMode"],
            defaultTriggerMode: section["DefaultTriggerMode"],
            defaultActionMode: section["DefaultActionMode"],
            defaultMaterializationMode: section["DefaultMaterializationMode"],
            routes: routeOverrides);
    }

    private static string? NormalizeOptionalToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static CellTrafficAutomationRouteSettings[] NormalizeRoutes(
        IReadOnlyList<CellTrafficAutomationRouteSettings>? routes)
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
                $"Duplicate cell traffic automation route overrides are not supported: {string.Join(", ", duplicateRouteIds)}.");
        }

        return normalized;
    }
}
