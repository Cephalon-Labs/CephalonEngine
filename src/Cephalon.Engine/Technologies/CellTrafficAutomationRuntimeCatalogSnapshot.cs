using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Technologies;

internal sealed class CellTrafficAutomationRuntimeCatalogSnapshot : ICellTrafficAutomationRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private const string DefaultAutomationMode = "advisory";
    private const string DefaultActionMode = "quarantine-route";
    private const string DefaultMaterializationMode = "runtime-catalog-only";
    private const string DefaultTriggerModeSource = "source-health";
    private const string DefaultTriggerModeTarget = "target-health";
    private const string DefaultTriggerModeBoth = "source-or-target-health";

    private readonly Lock gate = new();
    private readonly CellTrafficAutomationRuntimeDescriptor[] automations;
    private readonly Dictionary<string, CellTrafficAutomationRuntimeDescriptor> automationsById;
    private readonly Dictionary<string, CellTrafficAutomationRuntimeDescriptor> automationsByRouteId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsBySourceCellId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsByTargetCellId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsByProvider;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsByEdgeNodeId;
    private readonly Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> automationsByHealthIsolationId;
    private readonly Dictionary<string, ProviderMaterializationObservation> providerMaterializationObservationsByAutomationId = new(Comparer);

    public CellTrafficAutomationRuntimeCatalogSnapshot(
        IEnumerable<CellRouteDescriptor> routes,
        IEnumerable<CellHealthIsolationDescriptor> healthIsolations,
        CellTrafficAutomationSettings? settings = null,
        bool edgeTechnologySelected = false,
        IEnumerable<ICellTrafficAutomationProviderMaterializer>? providerMaterializers = null)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(healthIsolations);

        var configuredSettings = settings ?? CellTrafficAutomationSettings.Empty;
        var providerMaterializersByProvider = CreateProviderMaterializerIndex(providerMaterializers);
        var orderedRoutes = routes
            .OrderBy(static route => route.SourceCellId, Comparer)
            .ThenBy(static route => route.TargetCellId, Comparer)
            .ThenBy(static route => route.SourceModuleId, Comparer)
            .ThenBy(static route => route.Id, Comparer)
            .ToArray();
        var routePoliciesById = configuredSettings.Routes.ToDictionary(
            static route => route.RouteId,
            Comparer);
        ValidateKnownRoutes(routePoliciesById.Keys, orderedRoutes);

        if (!configuredSettings.HasValues)
        {
            automations = [];
            automationsById = new Dictionary<string, CellTrafficAutomationRuntimeDescriptor>(Comparer);
            automationsByRouteId = new Dictionary<string, CellTrafficAutomationRuntimeDescriptor>(Comparer);
            automationsBySourceModule = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(Comparer);
            automationsBySourceCellId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(Comparer);
            automationsByTargetCellId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(Comparer);
            automationsByProvider = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(Comparer);
            automationsByEdgeNodeId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(Comparer);
            automationsByHealthIsolationId = new Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>>(Comparer);
            return;
        }

        var healthIsolationsByCellId = healthIsolations
            .GroupBy(static isolation => isolation.CellId, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static isolation => isolation.Id, Comparer)
                    .ToArray(),
                Comparer);
        var selectedRoutes = configuredSettings.HasDefaultValues
            ? orderedRoutes
            : orderedRoutes
                .Where(route => routePoliciesById.ContainsKey(route.Id))
                .ToArray();

        automations = selectedRoutes
            .Select(route => CreateDescriptor(route, healthIsolationsByCellId, configuredSettings, routePoliciesById, edgeTechnologySelected, providerMaterializersByProvider))
            .OrderBy(static automation => automation.SourceCellId, Comparer)
            .ThenBy(static automation => automation.TargetCellId, Comparer)
            .ThenBy(static automation => automation.SourceModuleId, Comparer)
            .ThenBy(static automation => automation.RouteId, Comparer)
            .ToArray();
        ValidateDuplicateIds(automations);

        automationsById = automations.ToDictionary(static automation => automation.Id, Comparer);
        automationsByRouteId = automations.ToDictionary(static automation => automation.RouteId, Comparer);
        automationsBySourceModule = CreateIndex(automations, static automation => automation.SourceModuleId);
        automationsBySourceCellId = CreateIndex(automations, static automation => automation.SourceCellId);
        automationsByTargetCellId = CreateIndex(automations, static automation => automation.TargetCellId);
        automationsByProvider = CreateOptionalIndex(automations, static automation => automation.ProviderId);
        automationsByEdgeNodeId = automations
            .SelectMany(static automation => automation.EdgeNodeIds
                .Select(edgeNodeId => new KeyValuePair<string, CellTrafficAutomationRuntimeDescriptor>(edgeNodeId, automation)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>)group
                    .Select(static pair => pair.Value)
                    .ToArray(),
                Comparer);
        automationsByHealthIsolationId = automations
            .SelectMany(static automation =>
                automation.SourceHealthIsolationIds
                    .Concat(automation.TargetHealthIsolationIds)
                    .Distinct(Comparer)
                    .Select(healthIsolationId => new KeyValuePair<string, CellTrafficAutomationRuntimeDescriptor>(healthIsolationId, automation)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>)group
                    .Select(static pair => pair.Value)
                    .ToArray(),
                Comparer);
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> Automations
    {
        get
        {
            lock (gate)
            {
                return automations
                    .Select(Enrich)
                    .ToArray();
            }
        }
    }

    public CellTrafficAutomationRuntimeDescriptor? GetById(string automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return null;
        }

        return automationsById.TryGetValue(automationId.Trim(), out var automation)
            ? Enrich(automation)
            : null;
    }

    public CellTrafficAutomationRuntimeDescriptor? GetByRouteId(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return automationsByRouteId.TryGetValue(routeId.Trim(), out var automation)
            ? Enrich(automation)
            : null;
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return automationsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches.Select(Enrich).ToArray()
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetBySourceCellId(string sourceCellId)
    {
        if (string.IsNullOrWhiteSpace(sourceCellId))
        {
            return [];
        }

        return automationsBySourceCellId.TryGetValue(sourceCellId.Trim(), out var matches)
            ? matches.Select(Enrich).ToArray()
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByTargetCellId(string targetCellId)
    {
        if (string.IsNullOrWhiteSpace(targetCellId))
        {
            return [];
        }

        return automationsByTargetCellId.TryGetValue(targetCellId.Trim(), out var matches)
            ? matches.Select(Enrich).ToArray()
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return automationsByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches.Select(Enrich).ToArray()
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId)
    {
        if (string.IsNullOrWhiteSpace(edgeNodeId))
        {
            return [];
        }

        return automationsByEdgeNodeId.TryGetValue(edgeNodeId.Trim(), out var matches)
            ? matches.Select(Enrich).ToArray()
            : [];
    }

    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetByHealthIsolationId(string healthIsolationId)
    {
        if (string.IsNullOrWhiteSpace(healthIsolationId))
        {
            return [];
        }

        return automationsByHealthIsolationId.TryGetValue(healthIsolationId.Trim(), out var matches)
            ? matches.Select(Enrich).ToArray()
            : [];
    }

    internal IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetPendingProviderMaterializations()
    {
        lock (gate)
        {
            return automations
                .Where(static automation =>
                    !string.IsNullOrWhiteSpace(automation.ProviderId) &&
                    !string.IsNullOrWhiteSpace(automation.ProviderMaterializerId) &&
                    Comparer.Equals(
                        automation.ProviderMaterializationState,
                        CellTrafficAutomationProviderMaterializationStates.Pending))
                .Select(Enrich)
                .ToArray();
        }
    }

    internal void ReportProviderMaterialization(
        string automationId,
        string materializerId,
        CellTrafficAutomationProviderMaterializationResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(materializerId);
        ArgumentNullException.ThrowIfNull(result);

        var normalizedAutomationId = automationId.Trim();
        var normalizedMaterializerId = materializerId.Trim();

        if (!automationsById.TryGetValue(normalizedAutomationId, out var automation))
        {
            throw new InvalidOperationException(
                $"Cell traffic automation '{normalizedAutomationId}' is not registered in the active runtime.");
        }

        if (!Comparer.Equals(automation.ProviderMaterializerId, normalizedMaterializerId))
        {
            var selectedMaterializer = string.IsNullOrWhiteSpace(automation.ProviderMaterializerId)
                ? "none"
                : automation.ProviderMaterializerId;
            throw new InvalidOperationException(
                $"Cell traffic automation '{normalizedAutomationId}' is assigned to provider materializer '{selectedMaterializer}' and cannot report through '{normalizedMaterializerId}'.");
        }

        lock (gate)
        {
            providerMaterializationObservationsByAutomationId[normalizedAutomationId] = new ProviderMaterializationObservation(
                normalizedMaterializerId,
                result.State,
                result.ObservedAtUtc,
                result.Error,
                result.Metadata);
        }
    }

    private static CellTrafficAutomationRuntimeDescriptor CreateDescriptor(
        CellRouteDescriptor route,
        Dictionary<string, CellHealthIsolationDescriptor[]> healthIsolationsByCellId,
        CellTrafficAutomationSettings settings,
        Dictionary<string, CellTrafficAutomationRouteSettings> routePoliciesById,
        bool edgeTechnologySelected,
        Dictionary<string, ICellTrafficAutomationProviderMaterializer> providerMaterializersByProvider)
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
        var providerId = routePolicy?.ProviderId ?? settings.DefaultProviderId;
        var edgeNodeIds = routePolicy is not null && routePolicy.EdgeNodeIds.Count > 0
            ? routePolicy.EdgeNodeIds
            : settings.DefaultEdgeNodeIds;
        var materializationMode = routePolicy?.MaterializationMode
            ?? settings.DefaultMaterializationMode
            ?? ResolveDefaultMaterializationMode(providerId, edgeNodeIds.Count);

        string? providerMaterializerId = null;
        string? providerMaterializationState = null;
        if (!string.IsNullOrWhiteSpace(providerId) &&
            UsesProviderMaterialization(materializationMode))
        {
            if (providerMaterializersByProvider.TryGetValue(providerId!, out var providerMaterializer))
            {
                providerMaterializerId = providerMaterializer.MaterializerId;
                providerMaterializationState = CellTrafficAutomationProviderMaterializationStates.Pending;
            }
            else
            {
                providerMaterializationState = CellTrafficAutomationProviderMaterializationStates.Unavailable;
            }
        }

        if (edgeNodeIds.Count > 0 && !edgeTechnologySelected)
        {
            throw new InvalidOperationException(
                $"Cell traffic automation route '{route.Id}' targets edge nodes but the '{BuiltInTechnologies.EdgeNativeDelivery.Id}' technology is not active.");
        }

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
            ["dependencyCount"] = dependencyIds.Length.ToString(CultureInfo.InvariantCulture),
            ["edgeNodeCount"] = edgeNodeIds.Count.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(routePolicy?.Notes))
        {
            runtimeMetadata["note"] = routePolicy.Notes!;
        }

        if (!string.IsNullOrWhiteSpace(providerId))
        {
            runtimeMetadata["providerId"] = providerId!;
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
            materializationMode: materializationMode,
            policySource: routePolicy is null
                ? "cell-default"
                : "cell-route",
            transportIds: route.TransportIds,
            requiredCapabilityKey: route.RequiredCapabilityKey,
            sourceHealthIsolationIds: sourceHealthIsolationIds,
            targetHealthIsolationIds: targetHealthIsolationIds,
            dependencyIds: dependencyIds,
            metadata: route.Metadata,
            runtimeMetadata: runtimeMetadata,
            providerId: providerId,
            edgeNodeIds: edgeNodeIds,
            providerMaterializerId: providerMaterializerId,
            providerMaterializationState: providerMaterializationState,
            providerMaterializationObservedAtUtc: null,
            providerMaterializationError: null);
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

    private static string ResolveDefaultMaterializationMode(string? providerId, int edgeNodeCount)
    {
        return (!string.IsNullOrWhiteSpace(providerId), edgeNodeCount > 0) switch
        {
            (true, true) => "provider-and-edge-managed",
            (true, false) => "provider-managed",
            (false, true) => "edge-managed",
            _ => DefaultMaterializationMode
        };
    }

    private static bool UsesProviderMaterialization(string materializationMode)
    {
        return materializationMode.Trim().ToLowerInvariant() switch
        {
            "provider-managed" => true,
            "provider-and-edge-managed" => true,
            _ => false
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
            .ToHashSet(Comparer);
        var unknownRouteIds = configuredRouteIds
            .Where(routeId => !knownRouteIds.Contains(routeId))
            .OrderBy(static routeId => routeId, Comparer)
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
            .GroupBy(keySelector, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>)group.ToArray(),
                Comparer);
    }

    private static Dictionary<string, IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>> CreateOptionalIndex(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> automations,
        Func<CellTrafficAutomationRuntimeDescriptor, string?> keySelector)
    {
        return automations
            .Select(automation => new KeyValuePair<string?, CellTrafficAutomationRuntimeDescriptor>(keySelector(automation), automation))
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .GroupBy(static pair => pair.Key!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellTrafficAutomationRuntimeDescriptor>)group
                    .Select(static pair => pair.Value)
                    .ToArray(),
                Comparer);
    }

    private static void ValidateDuplicateIds(IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> automations)
    {
        var duplicateId = automations
            .GroupBy(static automation => automation.Id, Comparer)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is null)
        {
            return;
        }

        var owners = duplicateId
            .Select(static automation => automation.SourceModuleId)
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer);

        throw new InvalidOperationException(
            $"Cell traffic automation '{duplicateId.Key}' is registered multiple times by: {string.Join(", ", owners)}.");
    }

    private CellTrafficAutomationRuntimeDescriptor Enrich(CellTrafficAutomationRuntimeDescriptor automation)
    {
        if (!providerMaterializationObservationsByAutomationId.TryGetValue(automation.Id, out var observation))
        {
            return automation;
        }

        var runtimeMetadata = automation.RuntimeMetadata.Count == 0
            ? new Dictionary<string, string>(Comparer)
            : new Dictionary<string, string>(automation.RuntimeMetadata, Comparer);
        foreach (var pair in observation.Metadata)
        {
            runtimeMetadata[$"providerMaterialization.{pair.Key}"] = pair.Value;
        }

        return new CellTrafficAutomationRuntimeDescriptor(
            id: automation.Id,
            routeId: automation.RouteId,
            sourceModuleId: automation.SourceModuleId,
            sourceCellId: automation.SourceCellId,
            targetCellId: automation.TargetCellId,
            displayName: automation.DisplayName,
            description: automation.Description,
            routingStrategy: automation.RoutingStrategy,
            governanceMode: automation.GovernanceMode,
            automationMode: automation.AutomationMode,
            triggerMode: automation.TriggerMode,
            actionMode: automation.ActionMode,
            materializationMode: automation.MaterializationMode,
            policySource: automation.PolicySource,
            transportIds: automation.TransportIds,
            requiredCapabilityKey: automation.RequiredCapabilityKey,
            sourceHealthIsolationIds: automation.SourceHealthIsolationIds,
            targetHealthIsolationIds: automation.TargetHealthIsolationIds,
            dependencyIds: automation.DependencyIds,
            metadata: automation.Metadata,
            runtimeMetadata: runtimeMetadata,
            providerId: automation.ProviderId,
            edgeNodeIds: automation.EdgeNodeIds,
            providerMaterializerId: observation.MaterializerId,
            providerMaterializationState: observation.State,
            providerMaterializationObservedAtUtc: observation.ObservedAtUtc,
            providerMaterializationError: observation.Error);
    }

    private static Dictionary<string, ICellTrafficAutomationProviderMaterializer> CreateProviderMaterializerIndex(
        IEnumerable<ICellTrafficAutomationProviderMaterializer>? providerMaterializers)
    {
        var index = new Dictionary<string, ICellTrafficAutomationProviderMaterializer>(Comparer);

        if (providerMaterializers is null)
        {
            return index;
        }

        foreach (var materializer in providerMaterializers)
        {
            ArgumentNullException.ThrowIfNull(materializer);

            if (string.IsNullOrWhiteSpace(materializer.ProviderId))
            {
                throw new InvalidOperationException(
                    $"Cell traffic automation provider materializer '{materializer.GetType().FullName}' must declare a provider id.");
            }

            if (string.IsNullOrWhiteSpace(materializer.MaterializerId))
            {
                throw new InvalidOperationException(
                    $"Cell traffic automation provider materializer '{materializer.GetType().FullName}' must declare a materializer id.");
            }

            var normalizedProviderId = materializer.ProviderId.Trim();
            if (!index.TryAdd(normalizedProviderId, materializer))
            {
                throw new InvalidOperationException(
                    $"Multiple cell traffic automation provider materializers are registered for provider '{normalizedProviderId}'.");
            }
        }

        return index;
    }

    private sealed class ProviderMaterializationObservation
    {
        public ProviderMaterializationObservation(
            string materializerId,
            string state,
            DateTimeOffset observedAtUtc,
            string? error,
            IReadOnlyDictionary<string, string> metadata)
        {
            MaterializerId = materializerId;
            State = state;
            ObservedAtUtc = observedAtUtc;
            Error = error;
            Metadata = metadata.Count == 0
                ? new Dictionary<string, string>(Comparer)
                : new Dictionary<string, string>(metadata, Comparer);
        }

        public string MaterializerId { get; }

        public string State { get; }

        public DateTimeOffset ObservedAtUtc { get; }

        public string? Error { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}
