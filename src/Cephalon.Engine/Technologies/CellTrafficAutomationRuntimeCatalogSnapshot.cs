using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Technologies;

internal sealed class CellTrafficAutomationRuntimeCatalogSnapshot : ICellTrafficAutomationRuntimeCatalog
{
    private const string DefaultAutomationMode = "advisory";
    private const string DefaultActionMode = "quarantine-route";
    private const string DefaultMaterializationMode = "runtime-catalog-only";
    private const string DefaultTriggerModeSource = "source-health";
    private const string DefaultTriggerModeTarget = "target-health";
    private const string DefaultTriggerModeBoth = "source-or-target-health";

    private readonly CellTrafficAutomationRuntimeDescriptor[] automations;
    private readonly Dictionary<string, CellTrafficAutomationRuntimeDescriptor> automationsById;
    private readonly Dictionary<string, CellTrafficAutomationRuntimeDescriptor> automationsByRouteId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsBySourceCellId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsByTargetCellId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsByHealthIsolationId;

    public CellTrafficAutomationRuntimeCatalogSnapshot(
        IEnumerable<CellRouteDescriptor> routes,
        IEnumerable<CellHealthIsolationDescriptor> healthIsolations,
        CellTrafficAutomationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(healthIsolations);

        var configuredSettings = settings ?? CellTrafficAutomationSettings.Empty;
        var orderedRoutes = routes
            .OrderBy(static route => route.SourceCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.TargetCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var routePoliciesById = configuredSettings.Routes.ToDictionary(
            static route => route.RouteId,
            StringComparer.OrdinalIgnoreCase);
        ValidateKnownRoutes(routePoliciesById.Keys, orderedRoutes);

        if (!configuredSettings.HasValues)
        {
            automations = [];
            automationsById = new Dictionary<string, CellTrafficAutomationRuntimeDescriptor>(StringComparer.OrdinalIgnoreCase);
            automationsByRouteId = new Dictionary<string, CellTrafficAutomationRuntimeDescriptor>(StringComparer.OrdinalIgnoreCase);
            automationsBySourceModule = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);
            automationsBySourceCellId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);
            automationsByTargetCellId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);
            automationsByHealthIsolationId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var healthIsolationsByCellId = healthIsolations
            .GroupBy(static isolation => isolation.CellId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static isolation => isolation.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var selectedRoutes = configuredSettings.HasDefaultValues
            ? orderedRoutes
            : orderedRoutes
                .Where(route => routePoliciesById.ContainsKey(route.Id))
                .ToArray();

        automations = selectedRoutes
            .Select(route => CreateDescriptor(route, healthIsolationsByCellId, configuredSettings, routePoliciesById))
            .OrderBy(static automation => automation.SourceCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static automation => automation.TargetCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static automation => automation.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static automation => automation.RouteId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ValidateDuplicateIds(automations);

        automationsById = automations.ToDictionary(static automation => automation.Id, StringComparer.OrdinalIgnoreCase);
        automationsByRouteId = automations.ToDictionary(static automation => automation.RouteId, StringComparer.OrdinalIgnoreCase);
        automationsBySourceModule = CreateIndex(automations, static automation => automation.SourceModuleId);
        automationsBySourceCellId = CreateIndex(automations, static automation => automation.SourceCellId);
        automationsByTargetCellId = CreateIndex(automations, static automation => automation.TargetCellId);
        automationsByHealthIsolationId = automations
            .SelectMany(static automation =>
                automation.SourceHealthIsolationIds
                    .Concat(automation.TargetHealthIsolationIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(healthIsolationId => new KeyValuePair<string, CellTrafficAutomationRuntimeDescriptor>(healthIsolationId, automation)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>)group
                    .Select(static pair => pair.Value)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> Automations => automations;

    public CellTrafficAutomationRuntimeDescriptor? GetById(string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return null;
        }

        return automationsById.TryGetValue(automationId.Trim(), out var automation)
            ? automation
            : null;
    }

    public CellTrafficAutomationRuntimeDescriptor? GetByRouteId(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return automationsByRouteId.TryGetValue(routeId.Trim(), out var automation)
            ? automation
            : null;
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return automationsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceCellId(string sourceCellId)
    {
        if (string.IsNullOrWhiteSpace(sourceCellId))
        {
            return [];
        }

        return automationsBySourceCellId.TryGetValue(sourceCellId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByTargetCellId(string targetCellId)
    {
        if (string.IsNullOrWhiteSpace(targetCellId))
        {
            return [];
        }

        return automationsByTargetCellId.TryGetValue(targetCellId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByHealthIsolationId(string healthIsolationId)
    {
        if (string.IsNullOrWhiteSpace(healthIsolationId))
        {
            return [];
        }

        return automationsByHealthIsolationId.TryGetValue(healthIsolationId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static CellTrafficAutomationRuntimeDescriptor CreateDescriptor(
        CellRouteDescriptor route,
        Dictionary<string, CellHealthIsolationDescriptor[]> healthIsolationsByCellId,
        CellTrafficAutomationSettings settings,
        Dictionary<string, CellTrafficAutomationRouteSettings> routePoliciesById)
    {
        var sourceHealthIsolations = healthIsolationsByCellId.TryGetValue(route.SourceCellId, out var sourceMatches)
            ? sourceMatches
            : [];
        var targetHealthIsolations = healthIsolationsByCellId.TryGetValue(route.TargetCellId, out var targetMatches)
            ? targetMatches
            : [];

        if (sourceHealthIsolations.Length == 0 && targetHealthIsolations.Length == 0)
        {
            throw new InvalidOperationException(
                $"Cell traffic automation route '{route.Id}' does not reference any source or target cell with an active health isolation.");
        }

        var routePolicy = routePoliciesById.TryGetValue(route.Id, out var policy)
            ? policy
            : null;
        var sourceHealthIsolationIds = sourceHealthIsolations
            .Select(static isolation => isolation.Id)
            .ToArray();
        var targetHealthIsolationIds = targetHealthIsolations
            .Select(static isolation => isolation.Id)
            .ToArray();
        var dependencyIds = sourceHealthIsolations
            .Concat(targetHealthIsolations)
            .SelectMany(static isolation => isolation.DependencyIds)
            .Where(static dependencyId => !string.IsNullOrWhiteSpace(dependencyId))
            .Select(static dependencyId => dependencyId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static dependencyId => dependencyId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var runtimeMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceHealthIsolationCount"] = sourceHealthIsolationIds.Length.ToString(CultureInfo.InvariantCulture),
            ["targetHealthIsolationCount"] = targetHealthIsolationIds.Length.ToString(CultureInfo.InvariantCulture),
            ["dependencyCount"] = dependencyIds.Length.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(routePolicy?.Notes))
        {
            runtimeMetadata["note"] = routePolicy.Notes!;
        }

        if (routePolicy is not null)
        {
            foreach (var pair in routePolicy.Metadata)
            {
                runtimeMetadata.TryAdd(pair.Key, pair.Value);
            }
        }

        return new CellTrafficAutomationRuntimeDescriptor(
            id: route.Id,
            routeId: route.Id,
            sourceModuleId: route.SourceModuleId,
            sourceCellId: route.SourceCellId,
            targetCellId: route.TargetCellId,
            displayName: route.DisplayName,
            description: route.Description,
            routingStrategy: route.RoutingStrategy,
            governanceMode: route.GovernanceMode,
            automationMode: routePolicy?.AutomationMode
                ?? settings.DefaultAutomationMode
                ?? DefaultAutomationMode,
            triggerMode: routePolicy?.TriggerMode
                ?? settings.DefaultTriggerMode
                ?? ResolveDefaultTriggerMode(sourceHealthIsolationIds.Length, targetHealthIsolationIds.Length),
            actionMode: routePolicy?.ActionMode
                ?? settings.DefaultActionMode
                ?? DefaultActionMode,
            materializationMode: routePolicy?.MaterializationMode
                ?? settings.DefaultMaterializationMode
                ?? DefaultMaterializationMode,
            policySource: routePolicy is null
                ? "cell-default"
                : "cell-route",
            transportIds: route.TransportIds,
            requiredCapabilityKey: route.RequiredCapabilityKey,
            sourceHealthIsolationIds: sourceHealthIsolationIds,
            targetHealthIsolationIds: targetHealthIsolationIds,
            dependencyIds: dependencyIds,
            metadata: route.Metadata,
            runtimeMetadata: runtimeMetadata);
    }

    private static string ResolveDefaultTriggerMode(int sourceHealthIsolationCount, int targetHealthIsolationCount)
    {
        return (sourceHealthIsolationCount > 0, targetHealthIsolationCount > 0) switch
        {
            (true, true) => DefaultTriggerModeBoth,
            (true, false) => DefaultTriggerModeSource,
            (false, true) => DefaultTriggerModeTarget,
            _ => throw new InvalidOperationException("Cell traffic automation requires one source or target health isolation.")
        };
    }

    private static void ValidateKnownRoutes(
        Dictionary<string, CellTrafficAutomationRouteSettings>.KeyCollection configuredRouteIds,
        IReadOnlyCollection<CellRouteDescriptor> routes)
    {
        if (configuredRouteIds.Count == 0)
        {
            return;
        }

        var knownRouteIds = routes
            .Select(static route => route.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownRouteIds = configuredRouteIds
            .Where(routeId => !knownRouteIds.Contains(routeId))
            .OrderBy(static routeId => routeId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownRouteIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Cell traffic automation references unknown route ids: {string.Join(", ", unknownRouteIds)}.");
        }
    }

    private static Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> CreateIndex(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> automations,
        Func<CellTrafficAutomationRuntimeDescriptor, string> keySelector)
    {
        return automations
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateDuplicateIds(IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> automations)
    {
        var duplicateId = automations
            .GroupBy(static automation => automation.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is null)
        {
            return;
        }

        var owners = duplicateId
            .Select(static automation => automation.SourceModuleId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException(
            $"Cell traffic automation '{duplicateId.Key}' is registered multiple times by: {string.Join(", ", owners)}.");
    }
}
