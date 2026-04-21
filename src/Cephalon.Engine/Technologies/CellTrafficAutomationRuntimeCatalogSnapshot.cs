using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Technologies;

internal sealed class CellTrafficAutomationRuntimeCatalogSnapshot :
    ICellTrafficAutomationRuntimeCatalog,
    ICellTrafficAutomationMaterializationReportSink
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
    private readonly Dictionary<string, MaterializationObservation> edgeMaterializationObservationsByAutomationId = new(Comparer);
    private readonly Dictionary<string, MaterializationObservation> providerMaterializationObservationsByAutomationId = new(Comparer);

    public CellTrafficAutomationRuntimeCatalogSnapshot(
        IEnumerable<CellRouteDescriptor> routes,
        IEnumerable<CellHealthIsolationDescriptor> healthIsolations,
        CellTrafficAutomationSettings? settings = null,
        bool edgeTechnologySelected = false,
        IEnumerable<ICellTrafficAutomationProviderMaterializer>? providerMaterializers = null,
        IEnumerable<ICellTrafficAutomationEdgeMaterializer>? edgeMaterializers = null)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(healthIsolations);

        var configuredSettings = settings ?? CellTrafficAutomationSettings.Empty;
        var validatedProviderMaterializers = ValidateProviderMaterializers(providerMaterializers);
        var validatedEdgeMaterializers = ValidateEdgeMaterializers(edgeMaterializers);
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
            .Select(route => CreateDescriptor(route, healthIsolationsByCellId, configuredSettings, routePoliciesById, edgeTechnologySelected, validatedProviderMaterializers, validatedEdgeMaterializers))
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

    internal IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> GetPendingEdgeMaterializations()
    {
        lock (gate)
        {
            return automations
                .Where(static automation =>
                    automation.EdgeNodeIds.Count > 0 &&
                    !string.IsNullOrWhiteSpace(automation.EdgeMaterializerId) &&
                    Comparer.Equals(
                        automation.EdgeMaterializationState,
                        CellTrafficAutomationMaterializationStates.Pending))
                .Select(Enrich)
                .ToArray();
        }
    }

    public ValueTask ReportProviderAsync(
        string automationId,
        string materializerId,
        CellTrafficAutomationProviderMaterializationResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(materializerId);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

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
            providerMaterializationObservationsByAutomationId[normalizedAutomationId] = new MaterializationObservation(
                normalizedMaterializerId,
                result.State,
                result.ObservedAtUtc,
                result.Error,
                result.Metadata);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ReportEdgeAsync(
        string automationId,
        string materializerId,
        CellTrafficAutomationMaterializationResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(materializerId);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedAutomationId = automationId.Trim();
        var normalizedMaterializerId = materializerId.Trim();

        if (!automationsById.TryGetValue(normalizedAutomationId, out var automation))
        {
            throw new InvalidOperationException(
                $"Cell traffic automation '{normalizedAutomationId}' is not registered in the active runtime.");
        }

        if (!Comparer.Equals(automation.EdgeMaterializerId, normalizedMaterializerId))
        {
            var selectedMaterializer = string.IsNullOrWhiteSpace(automation.EdgeMaterializerId)
                ? "none"
                : automation.EdgeMaterializerId;
            throw new InvalidOperationException(
                $"Cell traffic automation '{normalizedAutomationId}' is assigned to edge materializer '{selectedMaterializer}' and cannot report through '{normalizedMaterializerId}'.");
        }

        lock (gate)
        {
            edgeMaterializationObservationsByAutomationId[normalizedAutomationId] = new MaterializationObservation(
                normalizedMaterializerId,
                result.State,
                result.ObservedAtUtc,
                result.Error,
                result.Metadata);
        }

        return ValueTask.CompletedTask;
    }

    private static CellTrafficAutomationRuntimeDescriptor CreateDescriptor(
        CellRouteDescriptor route,
        Dictionary<string, CellHealthIsolationDescriptor[]> healthIsolationsByCellId,
        CellTrafficAutomationSettings settings,
        Dictionary<string, CellTrafficAutomationRouteSettings> routePoliciesById,
        bool edgeTechnologySelected,
        IReadOnlyList<ICellTrafficAutomationProviderMaterializer> providerMaterializers,
        IReadOnlyList<ICellTrafficAutomationEdgeMaterializer> edgeMaterializers)
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

        var selectionDescriptor = new CellTrafficAutomationRuntimeDescriptor(
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
            edgeMaterializerId: null,
            edgeMaterializationState: null,
            edgeMaterializationObservedAtUtc: null,
            edgeMaterializationError: null,
            providerMaterializerId: null,
            providerMaterializationState: null,
            providerMaterializationObservedAtUtc: null,
            providerMaterializationError: null,
            materializationState: null,
            materializationObservedAtUtc: null,
            materializationError: null);

        string? providerMaterializerId = null;
        string? providerMaterializationState = null;
        if (!string.IsNullOrWhiteSpace(providerId) &&
            UsesProviderMaterialization(materializationMode))
        {
            var selectedProviderMaterializer = SelectProviderMaterializer(selectionDescriptor, providerMaterializers, runtimeMetadata);
            if (selectedProviderMaterializer is not null)
            {
                providerMaterializerId = selectedProviderMaterializer.MaterializerId;
                providerMaterializationState = CellTrafficAutomationProviderMaterializationStates.Pending;
            }
            else
            {
                providerMaterializationState = CellTrafficAutomationProviderMaterializationStates.Unavailable;
            }
        }

        string? edgeMaterializerId = null;
        string? edgeMaterializationState = null;
        if (UsesEdgeMaterialization(materializationMode))
        {
            var selectedEdgeMaterializer = SelectEdgeMaterializer(selectionDescriptor, edgeMaterializers, runtimeMetadata);
            if (selectedEdgeMaterializer is not null)
            {
                edgeMaterializerId = selectedEdgeMaterializer.MaterializerId;
                edgeMaterializationState = CellTrafficAutomationMaterializationStates.Pending;
            }
            else
            {
                edgeMaterializationState = CellTrafficAutomationMaterializationStates.Unavailable;
            }
        }

        var materializationSummary = ResolveMaterializationSummary(
            materializationMode,
            providerMaterializationState,
            observedProviderMaterializationAtUtc: null,
            providerMaterializationError: null,
            edgeMaterializationState,
            observedEdgeMaterializationAtUtc: null,
            edgeMaterializationError: null);
        ApplyMaterializationSummaryMetadata(
            runtimeMetadata,
            materializationMode,
            providerMaterializerId,
            providerMaterializationState,
            edgeMaterializerId,
            edgeMaterializationState,
            materializationSummary);

        return new CellTrafficAutomationRuntimeDescriptor(
            id: selectionDescriptor.Id,
            routeId: selectionDescriptor.RouteId,
            sourceModuleId: selectionDescriptor.SourceModuleId,
            sourceCellId: selectionDescriptor.SourceCellId,
            targetCellId: selectionDescriptor.TargetCellId,
            displayName: selectionDescriptor.DisplayName,
            description: selectionDescriptor.Description,
            routingStrategy: selectionDescriptor.RoutingStrategy,
            governanceMode: selectionDescriptor.GovernanceMode,
            automationMode: selectionDescriptor.AutomationMode,
            triggerMode: selectionDescriptor.TriggerMode,
            actionMode: selectionDescriptor.ActionMode,
            materializationMode: selectionDescriptor.MaterializationMode,
            policySource: selectionDescriptor.PolicySource,
            transportIds: selectionDescriptor.TransportIds,
            requiredCapabilityKey: selectionDescriptor.RequiredCapabilityKey,
            sourceHealthIsolationIds: selectionDescriptor.SourceHealthIsolationIds,
            targetHealthIsolationIds: selectionDescriptor.TargetHealthIsolationIds,
            dependencyIds: selectionDescriptor.DependencyIds,
            metadata: selectionDescriptor.Metadata,
            runtimeMetadata: runtimeMetadata,
            providerId: selectionDescriptor.ProviderId,
            edgeNodeIds: selectionDescriptor.EdgeNodeIds,
            edgeMaterializerId: edgeMaterializerId,
            edgeMaterializationState: edgeMaterializationState,
            edgeMaterializationObservedAtUtc: null,
            edgeMaterializationError: null,
            providerMaterializerId: providerMaterializerId,
            providerMaterializationState: providerMaterializationState,
            providerMaterializationObservedAtUtc: null,
            providerMaterializationError: null,
            materializationState: materializationSummary.State,
            materializationObservedAtUtc: materializationSummary.ObservedAtUtc,
            materializationError: materializationSummary.Error);
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

    private static bool UsesEdgeMaterialization(string materializationMode)
    {
        return materializationMode.Trim().ToLowerInvariant() switch
        {
            "edge-managed" => true,
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
        var hasProviderObservation = providerMaterializationObservationsByAutomationId.TryGetValue(automation.Id, out var providerObservation);
        var hasEdgeObservation = edgeMaterializationObservationsByAutomationId.TryGetValue(automation.Id, out var edgeObservation);
        var runtimeMetadata = automation.RuntimeMetadata.Count == 0
            ? new Dictionary<string, string>(Comparer)
            : new Dictionary<string, string>(automation.RuntimeMetadata, Comparer);
        if (hasProviderObservation)
        {
            foreach (var pair in providerObservation!.Metadata)
            {
                runtimeMetadata[$"providerMaterialization.{pair.Key}"] = pair.Value;
            }
        }

        if (hasEdgeObservation)
        {
            foreach (var pair in edgeObservation!.Metadata)
            {
                runtimeMetadata[$"edgeMaterialization.{pair.Key}"] = pair.Value;
            }
        }

        var edgeMaterializerId = hasEdgeObservation
            ? edgeObservation!.MaterializerId
            : automation.EdgeMaterializerId;
        var edgeMaterializationState = hasEdgeObservation
            ? edgeObservation!.State
            : automation.EdgeMaterializationState;
        var edgeMaterializationObservedAtUtc = hasEdgeObservation
            ? edgeObservation!.ObservedAtUtc
            : automation.EdgeMaterializationObservedAtUtc;
        var edgeMaterializationError = hasEdgeObservation
            ? edgeObservation!.Error
            : automation.EdgeMaterializationError;
        var providerMaterializerId = hasProviderObservation
            ? providerObservation!.MaterializerId
            : automation.ProviderMaterializerId;
        var providerMaterializationState = hasProviderObservation
            ? providerObservation!.State
            : automation.ProviderMaterializationState;
        var providerMaterializationObservedAtUtc = hasProviderObservation
            ? providerObservation!.ObservedAtUtc
            : automation.ProviderMaterializationObservedAtUtc;
        var providerMaterializationError = hasProviderObservation
            ? providerObservation!.Error
            : automation.ProviderMaterializationError;
        var materializationSummary = ResolveMaterializationSummary(
            automation.MaterializationMode,
            providerMaterializationState,
            providerMaterializationObservedAtUtc,
            providerMaterializationError,
            edgeMaterializationState,
            edgeMaterializationObservedAtUtc,
            edgeMaterializationError);
        ApplyMaterializationSummaryMetadata(
            runtimeMetadata,
            automation.MaterializationMode,
            providerMaterializerId,
            providerMaterializationState,
            edgeMaterializerId,
            edgeMaterializationState,
            materializationSummary);

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
            edgeMaterializerId: edgeMaterializerId,
            edgeMaterializationState: edgeMaterializationState,
            edgeMaterializationObservedAtUtc: edgeMaterializationObservedAtUtc,
            edgeMaterializationError: edgeMaterializationError,
            providerMaterializerId: providerMaterializerId,
            providerMaterializationState: providerMaterializationState,
            providerMaterializationObservedAtUtc: providerMaterializationObservedAtUtc,
            providerMaterializationError: providerMaterializationError,
            materializationState: materializationSummary.State,
            materializationObservedAtUtc: materializationSummary.ObservedAtUtc,
            materializationError: materializationSummary.Error);
    }

    private static ICellTrafficAutomationProviderMaterializer[] ValidateProviderMaterializers(
        IEnumerable<ICellTrafficAutomationProviderMaterializer>? providerMaterializers)
    {
        if (providerMaterializers is null)
        {
            return [];
        }

        var materializers = providerMaterializers.ToArray();
        var uniqueIds = new HashSet<string>(Comparer);
        foreach (var materializer in materializers)
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

            var normalizedMaterializerId = materializer.MaterializerId.Trim();
            if (!uniqueIds.Add(normalizedMaterializerId))
            {
                throw new InvalidOperationException(
                    $"Multiple cell traffic automation provider materializers are registered with materializer id '{normalizedMaterializerId}'.");
            }
        }

        return materializers;
    }

    private static ICellTrafficAutomationEdgeMaterializer[] ValidateEdgeMaterializers(
        IEnumerable<ICellTrafficAutomationEdgeMaterializer>? edgeMaterializers)
    {
        if (edgeMaterializers is null)
        {
            return [];
        }

        var materializers = edgeMaterializers.ToArray();
        var uniqueIds = new HashSet<string>(Comparer);
        foreach (var materializer in materializers)
        {
            ArgumentNullException.ThrowIfNull(materializer);

            if (string.IsNullOrWhiteSpace(materializer.MaterializerId))
            {
                throw new InvalidOperationException(
                    $"Cell traffic automation edge materializer '{materializer.GetType().FullName}' must declare a materializer id.");
            }

            if (!uniqueIds.Add(materializer.MaterializerId.Trim()))
            {
                throw new InvalidOperationException(
                    $"Multiple cell traffic automation edge materializers are registered with materializer id '{materializer.MaterializerId.Trim()}'.");
            }
        }

        return materializers;
    }

    private static ICellTrafficAutomationProviderMaterializer? SelectProviderMaterializer(
        CellTrafficAutomationRuntimeDescriptor automation,
        IReadOnlyList<ICellTrafficAutomationProviderMaterializer> providerMaterializers,
        Dictionary<string, string> runtimeMetadata)
    {
        if (providerMaterializers.Count == 0 || string.IsNullOrWhiteSpace(automation.ProviderId))
        {
            ApplySelectionMetadata(runtimeMetadata, "providerSelection", []);
            return null;
        }

        var matches = providerMaterializers
            .Where(materializer =>
                Comparer.Equals(materializer.ProviderId, automation.ProviderId) &&
                materializer.CanMaterialize(automation))
            .OrderByDescending(static materializer => materializer.Priority)
            .ThenBy(static materializer => materializer.MaterializerId, Comparer)
            .ToArray();
        ApplySelectionMetadata(
            runtimeMetadata,
            "providerSelection",
            matches.Select(static materializer => new MaterializerCandidate(materializer.MaterializerId, materializer.Priority)).ToArray());

        if (matches.Length == 0)
        {
            return null;
        }

        var selectedPriority = matches[0].Priority;
        var topMatches = matches
            .Where(materializer => materializer.Priority == selectedPriority)
            .ToArray();
        if (topMatches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple provider materializers matched cell traffic automation '{automation.Id}' for provider '{automation.ProviderId}' at priority '{selectedPriority.ToString(CultureInfo.InvariantCulture)}': {string.Join(", ", topMatches.Select(static materializer => materializer.MaterializerId).OrderBy(static materializerId => materializerId, Comparer))}.");
        }

        runtimeMetadata["providerSelection.selectedPriority"] = selectedPriority.ToString(CultureInfo.InvariantCulture);
        return topMatches[0];
    }

    private static ICellTrafficAutomationEdgeMaterializer? SelectEdgeMaterializer(
        CellTrafficAutomationRuntimeDescriptor automation,
        IReadOnlyList<ICellTrafficAutomationEdgeMaterializer> edgeMaterializers,
        Dictionary<string, string> runtimeMetadata)
    {
        if (edgeMaterializers.Count == 0)
        {
            ApplySelectionMetadata(runtimeMetadata, "edgeSelection", []);
            return null;
        }

        var matches = edgeMaterializers
            .Where(materializer => materializer.CanMaterialize(automation))
            .OrderByDescending(static materializer => materializer.Priority)
            .ThenBy(static materializer => materializer.MaterializerId, Comparer)
            .ToArray();
        ApplySelectionMetadata(
            runtimeMetadata,
            "edgeSelection",
            matches.Select(static materializer => new MaterializerCandidate(materializer.MaterializerId, materializer.Priority)).ToArray());

        if (matches.Length == 0)
        {
            return null;
        }

        var selectedPriority = matches[0].Priority;
        var topMatches = matches
            .Where(materializer => materializer.Priority == selectedPriority)
            .ToArray();
        if (topMatches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple edge materializers matched cell traffic automation '{automation.Id}' at priority '{selectedPriority.ToString(CultureInfo.InvariantCulture)}': {string.Join(", ", topMatches.Select(static materializer => materializer.MaterializerId).OrderBy(static materializerId => materializerId, Comparer))}.");
        }

        runtimeMetadata["edgeSelection.selectedPriority"] = selectedPriority.ToString(CultureInfo.InvariantCulture);
        return topMatches[0];
    }

    private static void ApplySelectionMetadata(
        Dictionary<string, string> runtimeMetadata,
        string prefix,
        IReadOnlyList<MaterializerCandidate> candidates)
    {
        runtimeMetadata[$"{prefix}.matchingCandidateCount"] = candidates.Count.ToString(CultureInfo.InvariantCulture);
        runtimeMetadata[$"{prefix}.matchingCandidateIds"] = string.Join(",", candidates.Select(static candidate => candidate.MaterializerId));
    }

    private static void ApplyMaterializationSummaryMetadata(
        Dictionary<string, string> runtimeMetadata,
        string materializationMode,
        string? providerMaterializerId,
        string? providerMaterializationState,
        string? edgeMaterializerId,
        string? edgeMaterializationState,
        MaterializationSummary summary)
    {
        var requiredDimensions = GetRequiredMaterializationDimensions(materializationMode);
        var selectedDimensions = GetSelectedMaterializationDimensions(providerMaterializerId, edgeMaterializerId);
        runtimeMetadata["materialization.requiredDimensionCount"] = requiredDimensions.Count.ToString(CultureInfo.InvariantCulture);
        runtimeMetadata["materialization.requiredDimensions"] = string.Join(",", requiredDimensions);
        runtimeMetadata["materialization.selectedDimensionCount"] = selectedDimensions.Count.ToString(CultureInfo.InvariantCulture);
        runtimeMetadata["materialization.selectedDimensions"] = string.Join(",", selectedDimensions);
        runtimeMetadata["materialization.stateBreakdown"] = string.Join(
            ",",
            GetStateBreakdown(materializationMode, providerMaterializationState, edgeMaterializationState));

        if (!string.IsNullOrWhiteSpace(summary.State))
        {
            runtimeMetadata["materialization.state"] = summary.State!;
        }
    }

    private static MaterializationSummary ResolveMaterializationSummary(
        string materializationMode,
        string? providerMaterializationState,
        DateTimeOffset? observedProviderMaterializationAtUtc,
        string? providerMaterializationError,
        string? edgeMaterializationState,
        DateTimeOffset? observedEdgeMaterializationAtUtc,
        string? edgeMaterializationError)
    {
        var dimensions = new List<MaterializationDimensionState>(capacity: 2);
        if (UsesProviderMaterialization(materializationMode) &&
            !string.IsNullOrWhiteSpace(providerMaterializationState))
        {
            dimensions.Add(new MaterializationDimensionState(
                "provider",
                providerMaterializationState!,
                observedProviderMaterializationAtUtc,
                providerMaterializationError));
        }

        if (UsesEdgeMaterialization(materializationMode) &&
            !string.IsNullOrWhiteSpace(edgeMaterializationState))
        {
            dimensions.Add(new MaterializationDimensionState(
                "edge",
                edgeMaterializationState!,
                observedEdgeMaterializationAtUtc,
                edgeMaterializationError));
        }

        if (dimensions.Count == 0)
        {
            return MaterializationSummary.Empty;
        }

        var distinctStates = dimensions
            .Select(static dimension => dimension.State)
            .Distinct(Comparer)
            .ToArray();
        var summaryState = distinctStates.Length == 1
            ? distinctStates[0]
            : distinctStates.All(state =>
                Comparer.Equals(state, CellTrafficAutomationMaterializationStates.Pending) ||
                Comparer.Equals(state, CellTrafficAutomationMaterializationStates.Applied))
                    ? CellTrafficAutomationMaterializationStates.Pending
                    : CellTrafficAutomationMaterializationStates.Partial;
        var observedAtValues = dimensions
            .Where(static dimension => dimension.ObservedAtUtc is not null)
            .Select(static dimension => dimension.ObservedAtUtc!.Value)
            .ToArray();
        var errors = dimensions
            .Where(static dimension => !string.IsNullOrWhiteSpace(dimension.Error))
            .Select(static dimension => $"{dimension.Dimension}: {dimension.Error}")
            .ToArray();

        return new MaterializationSummary(
            summaryState,
            observedAtValues.Length == 0
                ? null
                : observedAtValues.Max(),
            errors.Length == 0
                ? null
                : string.Join("; ", errors));
    }

    private static List<string> GetRequiredMaterializationDimensions(string materializationMode)
    {
        var dimensions = new List<string>(capacity: 2);
        if (UsesProviderMaterialization(materializationMode))
        {
            dimensions.Add("provider");
        }

        if (UsesEdgeMaterialization(materializationMode))
        {
            dimensions.Add("edge");
        }

        return dimensions;
    }

    private static List<string> GetSelectedMaterializationDimensions(
        string? providerMaterializerId,
        string? edgeMaterializerId)
    {
        var dimensions = new List<string>(capacity: 2);
        if (!string.IsNullOrWhiteSpace(providerMaterializerId))
        {
            dimensions.Add("provider");
        }

        if (!string.IsNullOrWhiteSpace(edgeMaterializerId))
        {
            dimensions.Add("edge");
        }

        return dimensions;
    }

    private static List<string> GetStateBreakdown(
        string materializationMode,
        string? providerMaterializationState,
        string? edgeMaterializationState)
    {
        var breakdown = new List<string>(capacity: 2);
        if (UsesProviderMaterialization(materializationMode))
        {
            breakdown.Add($"provider:{providerMaterializationState ?? "none"}");
        }

        if (UsesEdgeMaterialization(materializationMode))
        {
            breakdown.Add($"edge:{edgeMaterializationState ?? "none"}");
        }

        return breakdown;
    }

    private sealed class MaterializationObservation
    {
        public MaterializationObservation(
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

    private sealed record MaterializerCandidate(string MaterializerId, int Priority);

    private sealed record MaterializationDimensionState(
        string Dimension,
        string State,
        DateTimeOffset? ObservedAtUtc,
        string? Error);

    private sealed record MaterializationSummary(
        string? State,
        DateTimeOffset? ObservedAtUtc,
        string? Error)
    {
        public static MaterializationSummary Empty { get; } = new(null, null, null);
    }
}
