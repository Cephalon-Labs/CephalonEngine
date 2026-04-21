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
        : this(
            routeId,
            automationMode,
            triggerMode,
            actionMode,
            materializationMode,
            notes,
            metadata,
            providerId: null,
            edgeNodeIds: null)
    {
    }

    /// <summary>
    /// Creates cell traffic-automation route settings with provider and edge targeting.
    /// </summary>
    /// <param name="routeId">The stable governed route identifier.</param>
    /// <param name="automationMode">The optional normalized automation posture for this route.</param>
    /// <param name="triggerMode">The optional normalized trigger posture for this route.</param>
    /// <param name="actionMode">The optional normalized action posture for this route.</param>
    /// <param name="materializationMode">The optional normalized materialization posture for this route.</param>
    /// <param name="notes">Optional operator-facing notes for this route-specific overlay.</param>
    /// <param name="metadata">Optional route-specific runtime metadata.</param>
    /// <param name="providerId">The optional external provider or control-plane identifier for this route.</param>
    /// <param name="edgeNodeIds">The optional edge-node identifiers for this route.</param>
    public CellTrafficAutomationRouteSettings(
        string routeId,
        string? automationMode,
        string? triggerMode,
        string? actionMode,
        string? materializationMode,
        string? notes,
        IReadOnlyDictionary<string, string>? metadata,
        string? providerId,
        IReadOnlyList<string>? edgeNodeIds = null)
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
        ProviderId = NormalizeOptionalValue(providerId);
        EdgeNodeIds = NormalizeValues(edgeNodeIds);
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
    /// Gets the optional external provider or control-plane identifier for this route.
    /// </summary>
    public string? ProviderId { get; }

    /// <summary>
    /// Gets the optional edge-node identifiers for this route.
    /// </summary>
    public IReadOnlyList<string> EdgeNodeIds { get; }

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
            metadata: ReadMetadata(section.GetSection("Metadata")),
            providerId: section["ProviderId"],
            edgeNodeIds: ReadValues(section.GetSection("EdgeNodeIds")));
    }

    private static string? NormalizeOptionalToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string[] NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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

    private static string[] ReadValues(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return [];
        }

        return NormalizeValues(section
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray());
    }
}
