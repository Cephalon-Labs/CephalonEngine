using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;

namespace Cephalon.Edge.Traefik.Services;

internal sealed class TraefikTrafficAutomationMaterializer : ICellTrafficAutomationProviderMaterializer
{
    private readonly IReadOnlyDictionary<string, TraefikIngressRouteProjection> projectionsByRouteId;
    private readonly ITraefikTrafficObservationSource? observationSource;
    private readonly ITraefikTrafficApplyService? applyService;
    private readonly string controlPlaneMode;
    private readonly int observationPollingIntervalSeconds;
    private readonly bool cleanupSweepEnabled;
    private readonly Lock cleanupSummaryGate = new();
    private TraefikTrafficCleanupSweepResult? lastCleanupSweepResult;

    public TraefikTrafficAutomationMaterializer(
        TraefikTrafficProjectionCatalog projections,
        TraefikTrafficMaterializerOptions options,
        ITraefikTrafficObservationSource? observationSource = null,
        ITraefikTrafficApplyService? applyService = null)
    {
        ArgumentNullException.ThrowIfNull(projections);
        ArgumentNullException.ThrowIfNull(options);

        MaterializerId = string.IsNullOrWhiteSpace(options.MaterializerId)
            ? throw new InvalidOperationException("Traefik traffic materializer requires a materializer id.")
            : options.MaterializerId.Trim();
        ProviderId = string.IsNullOrWhiteSpace(options.ProviderId)
            ? throw new InvalidOperationException("Traefik traffic materializer requires a provider id.")
            : options.ProviderId.Trim();
        Priority = options.Priority;
        projectionsByRouteId = projections.Projections;
        this.observationSource = observationSource;
        this.applyService = applyService;
        controlPlaneMode = TraefikTrafficObservationModes.Normalize(options.Observation.Mode);
        observationPollingIntervalSeconds = Math.Max(1, options.Observation.PollingIntervalSeconds);
        cleanupSweepEnabled = options.Observation.EnableCleanupSweep;
    }

    public string MaterializerId { get; }

    public string ProviderId { get; }

    public int Priority { get; }

    internal bool SupportsLiveReconciliation =>
        string.Equals(
            controlPlaneMode,
            TraefikTrafficObservationModes.ObserveOnly,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            controlPlaneMode,
            TraefikTrafficObservationModes.ApplyAndReconcile,
            StringComparison.OrdinalIgnoreCase);

    internal bool UsesApplyAndReconcile =>
        string.Equals(
            controlPlaneMode,
            TraefikTrafficObservationModes.ApplyAndReconcile,
            StringComparison.OrdinalIgnoreCase);

    internal bool SupportsCleanupSweep =>
        UsesApplyAndReconcile &&
        cleanupSweepEnabled &&
        observationSource is not null;

    internal TimeSpan ObservationPollingInterval =>
        TimeSpan.FromSeconds(observationPollingIntervalSeconds);

    public bool CanMaterialize(CellTrafficAutomationRuntimeDescriptor automation)
    {
        ArgumentNullException.ThrowIfNull(automation);

        return UsesProviderMaterialization(automation.MaterializationMode) &&
            string.Equals(automation.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase) &&
            projectionsByRouteId.ContainsKey(automation.RouteId);
    }

    public async ValueTask<CellTrafficAutomationProviderMaterializationResult> MaterializeAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);
        cancellationToken.ThrowIfCancellationRequested();

        if (!projectionsByRouteId.TryGetValue(automation.RouteId, out var projection))
        {
            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Unavailable,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' has no projection for route '{automation.RouteId}'."));
        }

        if (UsesApplyAndReconcile)
        {
            return await ApplyAndReconcileAsync(automation, projection, cancellationToken).ConfigureAwait(false);
        }

        if (SupportsLiveReconciliation)
        {
            return await ObserveAsync(automation, cancellationToken).ConfigureAwait(false);
        }

        var metadata = projection.CreateMetadata();
        metadata["providerAction"] = "projected-intent";
        metadata["observationMode"] = TraefikTrafficObservationModes.ConfiguredIntent;
        metadata["resourceState"] = "projection-only";
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
        metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
        metadata["driftReasons"] = string.Empty;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Project;
        metadata["ingressRouteWriteAction"] = "none";

        return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Pending,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata,
            conditions: CreateMaterializationConditions(metadata)));
    }

    internal async ValueTask<CellTrafficAutomationProviderMaterializationResult> RefreshAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);

        return UsesApplyAndReconcile
            ? await MaterializeAsync(automation, cancellationToken).ConfigureAwait(false)
            : await ObserveAsync(automation, cancellationToken).ConfigureAwait(false);
    }

    internal async ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);

        if (!projectionsByRouteId.TryGetValue(automation.RouteId, out var projection))
        {
            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Unavailable,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' has no projection for route '{automation.RouteId}'."));
        }

        if (observationSource is null)
        {
            var metadata = projection.CreateMetadata();
            metadata["providerAction"] = TraefikTrafficObservationModes.ObserveOnly;
            metadata["observationMode"] = controlPlaneMode;
            metadata["statusSource"] = "observation-unavailable";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["driftReasons"] = string.Empty;
            metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;
            metadata["ingressRouteWriteAction"] = "none";

            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' is configured for '{controlPlaneMode}' mode, but no observation source is active.",
                metadata: metadata,
                conditions: CreateMaterializationConditions(metadata)));
        }

        return AttachCleanupSummary(
            await observationSource.ObserveAsync(automation, projection, cancellationToken).ConfigureAwait(false));
    }

    internal async ValueTask<TraefikTrafficCleanupSweepResult> SweepCleanupAsync(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> activeAutomations,
        CancellationToken cancellationToken = default)
    {
        if (!UsesApplyAndReconcile)
        {
            return RecordCleanupSweepResult(CreateDefaultCleanupSweepResult());
        }

        if (!cleanupSweepEnabled)
        {
            return RecordCleanupSweepResult(CreateDefaultCleanupSweepResult());
        }

        if (observationSource is null)
        {
            return RecordCleanupSweepResult(CreateUnavailableCleanupSweepResult());
        }

        try
        {
            var cleanupResult = await observationSource.SweepCleanupAsync(
                activeAutomations,
                projectionsByRouteId.Values.ToArray(),
                cancellationToken).ConfigureAwait(false);
            return RecordCleanupSweepResult(cleanupResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return RecordCleanupSweepResult(CreateFailedCleanupSweepResult(exception.Message));
        }
    }

    private async ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAndReconcileAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken)
    {
        if (applyService is null)
        {
            var metadata = projection.CreateMetadata();
            metadata["providerAction"] = TraefikTrafficObservationModes.ApplyAndReconcile;
            metadata["observationMode"] = TraefikTrafficObservationModes.ApplyAndReconcile;
            metadata["statusSource"] = "apply-unavailable";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["driftReasons"] = string.Empty;
            metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile;
            metadata["ingressRouteWriteAction"] = "none";

            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' is configured for apply-and-reconcile mode, but no apply service is active.",
                metadata: metadata,
                conditions: CreateMaterializationConditions(metadata)));
        }

        if (observationSource is null)
        {
            var metadata = projection.CreateMetadata();
            metadata["providerAction"] = TraefikTrafficObservationModes.ApplyAndReconcile;
            metadata["observationMode"] = TraefikTrafficObservationModes.ApplyAndReconcile;
            metadata["statusSource"] = "observation-unavailable";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["driftReasons"] = string.Empty;
            metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile;
            metadata["ingressRouteWriteAction"] = "none";

            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' is configured for apply-and-reconcile mode, but no observation source is active.",
                metadata: metadata,
                conditions: CreateMaterializationConditions(metadata)));
        }

        var applyResult = await applyService.ApplyAsync(automation, projection, cancellationToken).ConfigureAwait(false);
        if (string.Equals(
                applyResult.State,
                CellTrafficAutomationProviderMaterializationStates.Failed,
                StringComparison.OrdinalIgnoreCase))
        {
            return AttachCleanupSummary(applyResult);
        }

        var observedResult = await observationSource.ObserveAsync(automation, projection, cancellationToken).ConfigureAwait(false);
        return AttachCleanupSummary(MergeApplyAndObservedResult(applyResult, observedResult));
    }

    private CellTrafficAutomationProviderMaterializationResult AttachCleanupSummary(
        CellTrafficAutomationProviderMaterializationResult result)
    {
        if (!UsesApplyAndReconcile)
        {
            if (result.Conditions.Count > 0 || result.Metadata.Count == 0)
            {
                return result;
            }

            var normalizedMetadata = new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase);
            return new CellTrafficAutomationProviderMaterializationResult(
                state: result.State,
                observedAtUtc: result.ObservedAtUtc,
                error: result.Error,
                metadata: normalizedMetadata,
                conditions: CreateMaterializationConditions(normalizedMetadata));
        }

        var cleanupResult = GetCleanupSweepResult();
        var metadata = result.Metadata.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase);
        metadata["cleanupSweepEnabled"] = cleanupSweepEnabled ? "true" : "false";
        metadata["cleanupState"] = cleanupResult.State;
        metadata["cleanupObservedAtUtc"] = cleanupResult.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(cleanupResult.Error))
        {
            metadata["cleanupError"] = cleanupResult.Error!;
        }
        else
        {
            metadata.Remove("cleanupError");
        }

        foreach (var pair in cleanupResult.Metadata)
        {
            metadata[$"cleanup.{pair.Key}"] = pair.Value;
        }

        var conditions = result.Conditions.Count == 0
            ? CreateMaterializationConditions(metadata)
            : result.Conditions;

        return new CellTrafficAutomationProviderMaterializationResult(
            state: result.State,
            observedAtUtc: result.ObservedAtUtc,
            error: result.Error,
            metadata: metadata,
            conditions: conditions);
    }

    private TraefikTrafficCleanupSweepResult GetCleanupSweepResult()
    {
        lock (cleanupSummaryGate)
        {
            lastCleanupSweepResult ??= CreateDefaultCleanupSweepResult();
            return lastCleanupSweepResult;
        }
    }

    private TraefikTrafficCleanupSweepResult RecordCleanupSweepResult(
        TraefikTrafficCleanupSweepResult result)
    {
        lock (cleanupSummaryGate)
        {
            lastCleanupSweepResult = result;
            return result;
        }
    }

    private TraefikTrafficCleanupSweepResult CreateDefaultCleanupSweepResult()
    {
        var metadata = CreateCleanupSweepMetadata();
        var state = cleanupSweepEnabled ? "pending" : "disabled";
        return new TraefikTrafficCleanupSweepResult(
            DateTimeOffset.UtcNow,
            state,
            metadata: metadata);
    }

    private TraefikTrafficCleanupSweepResult CreateUnavailableCleanupSweepResult()
    {
        return new TraefikTrafficCleanupSweepResult(
            DateTimeOffset.UtcNow,
            "unavailable",
            "Traefik cleanup sweep requires an observation source.",
            CreateCleanupSweepMetadata());
    }

    private TraefikTrafficCleanupSweepResult CreateFailedCleanupSweepResult(string error)
    {
        return new TraefikTrafficCleanupSweepResult(
            DateTimeOffset.UtcNow,
            "failed",
            error,
            CreateCleanupSweepMetadata());
    }

    private Dictionary<string, string> CreateCleanupSweepMetadata()
    {
        var namespaces = projectionsByRouteId.Values
            .Select(static projection => projection.RouteNamespace)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["candidateCount"] = "0",
            ["removedResourceCount"] = "0",
            ["deletedTransferredResourceCount"] = "0",
            ["prunedOrphanResourceCount"] = "0",
            ["resourceIds"] = string.Empty,
            ["lifecycleActions"] = string.Empty,
            ["namespaces"] = string.Join(",", namespaces)
        };
    }

    private static CellTrafficAutomationProviderMaterializationResult MergeApplyAndObservedResult(
        CellTrafficAutomationProviderMaterializationResult applyResult,
        CellTrafficAutomationProviderMaterializationResult observedResult)
    {
        var metadata = applyResult.Metadata.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(applyResult.Metadata, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in observedResult.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata["providerAction"] = TraefikTrafficObservationModes.ApplyAndReconcile;
        metadata["observationMode"] = TraefikTrafficObservationModes.ApplyAndReconcile;
        if (applyResult.Metadata.TryGetValue("lifecycleAction", out var lifecycleAction) &&
            !string.IsNullOrWhiteSpace(lifecycleAction))
        {
            metadata["lifecycleAction"] = lifecycleAction;
        }

        return new CellTrafficAutomationProviderMaterializationResult(
            state: observedResult.State,
            observedAtUtc: observedResult.ObservedAtUtc,
            error: observedResult.Error,
            metadata: metadata,
            conditions: MergeConditions(applyResult.Conditions, observedResult.Conditions));
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

    private static List<CellTrafficAutomationMaterializationConditionDescriptor> CreateMaterializationConditions(
        Dictionary<string, string> metadata)
    {
        var conditions = new List<CellTrafficAutomationMaterializationConditionDescriptor>
        {
            CreateObservationCondition(metadata),
            CreateOwnershipCondition(metadata),
            CreateDependencyCondition(metadata),
            CreateDriftCondition(metadata),
            CreateLifecycleCondition(metadata)
        };

        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "ingress-route-present",
            metadataKey: "ingressRouteExists",
            category: CellTrafficAutomationMaterializationConditionCategories.Readiness,
            trueDescription: "The target Traefik IngressRoute exists.",
            falseDescription: "The target Traefik IngressRoute is missing.",
            unknownDescription: "The runtime cannot currently determine whether the target Traefik IngressRoute exists.");
        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "backend-service-present",
            metadataKey: "backendServiceExists",
            category: CellTrafficAutomationMaterializationConditionCategories.Dependency,
            trueDescription: "The backend Service dependency exists.",
            falseDescription: "The backend Service dependency is missing.",
            unknownDescription: "The runtime cannot currently determine backend Service dependency posture.");
        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "tls-options-present",
            metadataKey: "tlsOptionsExists",
            category: CellTrafficAutomationMaterializationConditionCategories.Dependency,
            trueDescription: "The TLS options dependency exists.",
            falseDescription: "The TLS options dependency is missing.",
            unknownDescription: "The runtime cannot currently determine TLS options dependency posture.");
        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "tls-secret-present",
            metadataKey: "tlsSecretExists",
            category: CellTrafficAutomationMaterializationConditionCategories.Dependency,
            trueDescription: "The TLS Secret dependency exists.",
            falseDescription: "The TLS Secret dependency is missing.",
            unknownDescription: "The runtime cannot currently determine TLS Secret dependency posture.");

        if (metadata.TryGetValue("missingMiddlewareRefs", out var missingMiddlewareRefs) &&
            !string.IsNullOrWhiteSpace(missingMiddlewareRefs))
        {
            conditions.Add(new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "middleware-present",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: "missing-middleware",
                description: $"The following Traefik middleware dependencies are missing: {missingMiddlewareRefs}.",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["missingMiddlewareRefs"] = missingMiddlewareRefs
                }));
        }
        else
        {
            conditions.Add(new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "middleware-present",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "resolved",
                description: "All declared Traefik middleware dependencies are currently available."));
        }

        return conditions;
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor[] MergeConditions(
        IReadOnlyList<CellTrafficAutomationMaterializationConditionDescriptor> applyConditions,
        IReadOnlyList<CellTrafficAutomationMaterializationConditionDescriptor> observedConditions)
    {
        return applyConditions
            .Concat(observedConditions)
            .GroupBy(
                static condition => string.Join(
                    "|",
                    condition.Dimension,
                    condition.Category,
                    condition.ConditionId,
                    condition.State,
                    condition.Severity,
                    condition.Reason ?? string.Empty,
                    condition.Description ?? string.Empty),
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static condition => condition.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static condition => condition.ConditionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateObservationCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("statusSource", out var statusSource);
        metadata.TryGetValue("resourceState", out var resourceState);

        return NormalizeStatusSource(statusSource) switch
        {
            "traefik-ingressroute-observation" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: NormalizeReason(resourceState) ?? "live-status",
                description: "The runtime reports live Traefik control-plane truth."),
            "configured-intent" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "configured-intent",
                description: "The runtime currently exposes projected Traefik intent without a live control-plane observation."),
            "control-plane-apply" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: NormalizeReason(resourceState) ?? "control-plane-apply",
                description: "The runtime has written intent to Traefik and is waiting for a fresh live observation."),
            "apply-unavailable" or "observation-unavailable" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: NormalizeReason(resourceState) ?? "observation-unavailable",
                description: "The runtime cannot currently reach the Kubernetes API to reconcile or observe Traefik materialization."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: NormalizeReason(resourceState) ?? "observation-error",
                description: "The runtime encountered an error while reading or reconciling Traefik materialization.")
        };
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateOwnershipCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("ownershipState", out var ownershipState);
        metadata.TryGetValue("ownershipReason", out var ownershipReason);

        var normalizedState = NormalizeState(ownershipState);
        return normalizedState switch
        {
            CellTrafficAutomationOwnershipStates.Owned => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: ownershipReason ?? "owned",
                description: "The Traefik IngressRoute is currently owned by the active Cephalon automation."),
            CellTrafficAutomationOwnershipStates.Requested => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: ownershipReason ?? "requested",
                description: "The Traefik IngressRoute has been requested but is not yet confirmed as Cephalon-owned."),
            CellTrafficAutomationOwnershipStates.Orphaned or CellTrafficAutomationOwnershipStates.Transferred or CellTrafficAutomationOwnershipStates.Pruned => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: ownershipReason ?? normalizedState,
                description: "The Traefik IngressRoute carries stale or transitional Cephalon ownership metadata."),
            CellTrafficAutomationOwnershipStates.OwnershipConflict => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: ownershipReason ?? normalizedState,
                description: "The Traefik IngressRoute is not safely owned by the active Cephalon automation."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: ownershipReason ?? "unknown",
                description: "The runtime cannot currently determine Traefik IngressRoute ownership.")
        };
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateDependencyCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("dependencyState", out var dependencyState);
        metadata.TryGetValue("dependencyMissingRefs", out var dependencyMissingRefs);

        var normalizedState = NormalizeState(dependencyState);
        return normalizedState switch
        {
            CellTrafficAutomationDependencyStates.Satisfied => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "satisfied",
                description: "The expected Traefik dependency posture is currently satisfied."),
            CellTrafficAutomationDependencyStates.Missing => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: "missing",
                description: string.IsNullOrWhiteSpace(dependencyMissingRefs)
                    ? "One or more required Traefik dependencies are missing."
                    : $"Missing Traefik dependencies: {dependencyMissingRefs}."),
            CellTrafficAutomationDependencyStates.Mixed => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: "mixed",
                description: "Only part of the expected Traefik dependency posture is currently satisfied."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "unknown",
                description: "The runtime cannot currently determine Traefik dependency posture.")
        };
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateDriftCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("driftState", out var driftState);
        metadata.TryGetValue("driftReasons", out var driftReasons);

        var normalizedState = NormalizeState(driftState);
        return normalizedState switch
        {
            CellTrafficAutomationDriftStates.InSync => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "in-sync",
                description: "Observed Traefik state matches the authored Cephalon intent."),
            CellTrafficAutomationDriftStates.Reconciling => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "reconciling",
                description: "Observed Traefik state is still converging toward the authored Cephalon intent."),
            CellTrafficAutomationDriftStates.Drifted => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: "drifted",
                description: string.IsNullOrWhiteSpace(driftReasons)
                    ? "Observed Traefik state drifts from the authored Cephalon intent."
                    : $"Observed Traefik state drifts for: {driftReasons}."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "unknown",
                description: "The runtime cannot currently determine Traefik drift posture.")
        };
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateLifecycleCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("lifecycleAction", out var lifecycleAction);
        var normalizedAction = NormalizeReason(lifecycleAction) ?? "unknown";
        return new CellTrafficAutomationMaterializationConditionDescriptor(
            CellTrafficAutomationMaterializationConditionDimensions.Provider,
            CellTrafficAutomationMaterializationConditionCategories.Lifecycle,
            "reconcile-action",
            normalizedAction switch
            {
                CellTrafficAutomationLifecycleActions.Create or
                CellTrafficAutomationLifecycleActions.Replace or
                CellTrafficAutomationLifecycleActions.Transfer or
                CellTrafficAutomationLifecycleActions.Delete or
                CellTrafficAutomationLifecycleActions.Prune => CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationLifecycleActions.Project or
                CellTrafficAutomationLifecycleActions.Reconcile or
                CellTrafficAutomationLifecycleActions.Observe => CellTrafficAutomationMaterializationConditionStates.Pending,
                _ => CellTrafficAutomationMaterializationConditionStates.Unknown
            },
            CellTrafficAutomationMaterializationConditionSeverities.Info,
            reason: normalizedAction,
            description: $"The active Traefik lifecycle posture is '{normalizedAction}'.");
    }

    private static void AddBooleanCondition(
        List<CellTrafficAutomationMaterializationConditionDescriptor> conditions,
        Dictionary<string, string> metadata,
        string conditionId,
        string metadataKey,
        string category,
        string trueDescription,
        string falseDescription,
        string unknownDescription)
    {
        if (!metadata.TryGetValue(metadataKey, out var value))
        {
            return;
        }

        var normalizedValue = NormalizeReason(value) ?? CellTrafficAutomationMaterializationConditionStates.Unknown;
        var (state, severity, reason, description) = normalizedValue switch
        {
            "true" => (
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                "reported-true",
                trueDescription),
            "false" => (
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                "reported-false",
                falseDescription),
            _ => (
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                "reported-unknown",
                unknownDescription)
        };

        conditions.Add(new CellTrafficAutomationMaterializationConditionDescriptor(
            CellTrafficAutomationMaterializationConditionDimensions.Provider,
            category,
            conditionId,
            state,
            severity,
            reason,
            description));
    }

    private static string NormalizeState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static string? NormalizeReason(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string NormalizeStatusSource(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
