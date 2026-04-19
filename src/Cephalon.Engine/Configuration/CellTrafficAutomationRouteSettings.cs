using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one configuration-driven cell traffic-automation route override.
/// </summary>
public sealed class CellTrafficAutomationRouteSettings
{
    /// <summary>
    /// Creates cell traffic-automation route settings.
    /// </summary>
    /// <param name="routeId">The stable governed route identifier.</param>
    /// <param name="automationMode">The optional normalized automation posture for this route.</param>
    /// <param name="triggerMode">The optional normalized trigger posture for this route.</param>
    /// <param name="actionMode">The optional normalized action posture for this route.</param>
    /// <param name="materializationMode">The optional normalized materialization posture for this route.</param>
    /// <param name="notes">Optional operator-facing notes for this route-specific overlay.</param>
    /// <param name="metadata">Optional route-specific runtime metadata.</param>
    public CellTrafficAutomationRouteSettings(
        string routeId,
        string? automationMode = null,
        string? triggerMode = null,
        string? actionMode = null,
        string? materializationMode = null,
        string? notes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
        }

        RouteId = routeId.Trim();
        AutomationMode = NormalizeOptionalToken(automationMode);
        TriggerMode = NormalizeOptionalToken(triggerMode);
        ActionMode = NormalizeOptionalToken(actionMode);
        MaterializationMode = NormalizeOptionalToken(materializationMode);
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable governed route identifier.
    /// </summary>
    public string RouteId { get; }

    /// <summary>
    /// Gets the optional normalized automation posture for this route.
    /// </summary>
    public string? AutomationMode { get; }

    /// <summary>
    /// Gets the optional normalized trigger posture for this route.
    /// </summary>
    public string? TriggerMode { get; }

    /// <summary>
    /// Gets the optional normalized action posture for this route.
    /// </summary>
    public string? ActionMode { get; }

    /// <summary>
    /// Gets the optional normalized materialization posture for this route.
    /// </summary>
    public string? MaterializationMode { get; }

    /// <summary>
    /// Gets optional operator-facing notes for this route-specific overlay.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets optional route-specific runtime metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Reads one cell traffic-automation route override from configuration.
    /// </summary>
    /// <param name="section">The configuration section that contains the route override.</param>
    /// <returns>The parsed route settings.</returns>
    public static CellTrafficAutomationRouteSettings FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new CellTrafficAutomationRouteSettings(
            routeId: section["RouteId"]
                ?? throw new InvalidOperationException("Cell traffic automation route id is required."),
            automationMode: section["AutomationMode"],
            triggerMode: section["TriggerMode"],
            actionMode: section["ActionMode"],
            materializationMode: section["MaterializationMode"],
            notes: section["Notes"],
            metadata: ReadMetadata(section.GetSection("Metadata")));
    }

    private static string? NormalizeOptionalToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static Dictionary<string, string> ReadMetadata(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return section
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}
