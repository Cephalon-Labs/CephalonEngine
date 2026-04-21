using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficAutomationMaterializer : ICellTrafficAutomationProviderMaterializer
{
    private readonly IReadOnlyDictionary<string, KubernetesGatewayTrafficRouteProjection> projectionsByRouteId;
    private readonly IKubernetesGatewayTrafficObservationSource? observationSource;
    private readonly IKubernetesGatewayTrafficApplyService? applyService;
    private readonly string controlPlaneMode;
    private readonly int observationPollingIntervalSeconds;
    private readonly bool cleanupSweepEnabled;
    private readonly Lock cleanupSummaryGate = new();
    private KubernetesGatewayTrafficCleanupSweepResult? lastCleanupSweepResult;

    public KubernetesGatewayTrafficAutomationMaterializer(
        KubernetesGatewayTrafficProjectionCatalog projections,
        KubernetesGatewayTrafficMaterializerOptions options,
        IKubernetesGatewayTrafficObservationSource? observationSource = null,
        IKubernetesGatewayTrafficApplyService? applyService = null)
    {
        ArgumentNullException.ThrowIfNull(projections);
        ArgumentNullException.ThrowIfNull(options);

        MaterializerId = string.IsNullOrWhiteSpace(options.MaterializerId)
            ? throw new InvalidOperationException("Kubernetes Gateway traffic materializer requires a materializer id.")
            : options.MaterializerId.Trim();
        ProviderId = string.IsNullOrWhiteSpace(options.ProviderId)
            ? throw new InvalidOperationException("Kubernetes Gateway traffic materializer requires a provider id.")
            : options.ProviderId.Trim();
        Priority = options.Priority;
        projectionsByRouteId = projections.Projections;
        this.observationSource = observationSource;
        this.applyService = applyService;
        controlPlaneMode = KubernetesGatewayTrafficObservationModes.Normalize(options.Observation.Mode);
        observationPollingIntervalSeconds = Math.Max(1, options.Observation.PollingIntervalSeconds);
        cleanupSweepEnabled = options.Observation.EnableCleanupSweep;
    }

    public string MaterializerId { get; }

    public string ProviderId { get; }

    public int Priority { get; }

    internal bool SupportsLiveReconciliation =>
        string.Equals(
            controlPlaneMode,
            KubernetesGatewayTrafficObservationModes.ObserveOnly,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            controlPlaneMode,
            KubernetesGatewayTrafficObservationModes.ApplyAndReconcile,
            StringComparison.OrdinalIgnoreCase);

    internal bool UsesApplyAndReconcile =>
        string.Equals(
            controlPlaneMode,
            KubernetesGatewayTrafficObservationModes.ApplyAndReconcile,
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

        if (!projectionsByRouteId.TryGetValue(automation.RouteId, out var projection))
        {
            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Unavailable,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Kubernetes Gateway traffic materializer '{MaterializerId}' has no projection for route '{automation.RouteId}'."));
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
        metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.ConfiguredIntent;
        metadata["resourceState"] = "projection-only";
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
        metadata["driftState"] = "unknown";
        metadata["driftReasons"] = string.Empty;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Project;
        metadata["gatewayWriteAction"] = "none";
        metadata["httpRouteWriteAction"] = "none";

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
                error: $"Kubernetes Gateway traffic materializer '{MaterializerId}' has no projection for route '{automation.RouteId}'."));
        }

        if (observationSource is null)
        {
            var metadata = projection.CreateMetadata();
            metadata["providerAction"] = "observe-only";
            metadata["observationMode"] = controlPlaneMode;
            metadata["statusSource"] = "observation-unavailable";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;

            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Kubernetes Gateway traffic materializer '{MaterializerId}' is configured for observe-only mode, but no observation source is active.",
                metadata: metadata,
                conditions: CreateMaterializationConditions(metadata)));
        }

        return AttachCleanupSummary(
            await observationSource.ObserveAsync(automation, projection, cancellationToken).ConfigureAwait(false));
    }

    internal async ValueTask<KubernetesGatewayTrafficCleanupSweepResult> SweepCleanupAsync(
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
        KubernetesGatewayTrafficRouteProjection projection,
        CancellationToken cancellationToken)
    {
        if (applyService is null)
        {
            var metadata = projection.CreateMetadata();
            metadata["providerAction"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
            metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
            metadata["statusSource"] = "apply-unavailable";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile;
            metadata["gatewayWriteAction"] = "none";
            metadata["httpRouteWriteAction"] = "none";

            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Kubernetes Gateway traffic materializer '{MaterializerId}' is configured for apply-and-reconcile mode, but no apply service is active.",
                metadata: metadata,
                conditions: CreateMaterializationConditions(metadata)));
        }

        if (observationSource is null)
        {
            var metadata = projection.CreateMetadata();
            metadata["providerAction"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
            metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
            metadata["statusSource"] = "observation-unavailable";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile;
            metadata["gatewayWriteAction"] = "none";
            metadata["httpRouteWriteAction"] = "none";

            return AttachCleanupSummary(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Kubernetes Gateway traffic materializer '{MaterializerId}' is configured for apply-and-reconcile mode, but no observation source is active.",
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

    private KubernetesGatewayTrafficCleanupSweepResult GetCleanupSweepResult()
    {
        lock (cleanupSummaryGate)
        {
            lastCleanupSweepResult ??= CreateDefaultCleanupSweepResult();
            return lastCleanupSweepResult;
        }
    }

    private KubernetesGatewayTrafficCleanupSweepResult RecordCleanupSweepResult(
        KubernetesGatewayTrafficCleanupSweepResult result)
    {
        lock (cleanupSummaryGate)
        {
            lastCleanupSweepResult = result;
            return result;
        }
    }

    private KubernetesGatewayTrafficCleanupSweepResult CreateDefaultCleanupSweepResult()
    {
        var metadata = CreateCleanupSweepMetadata();
        var state = cleanupSweepEnabled ? "pending" : "disabled";
        return new KubernetesGatewayTrafficCleanupSweepResult(
            DateTimeOffset.UtcNow,
            state,
            metadata: metadata);
    }

    private KubernetesGatewayTrafficCleanupSweepResult CreateUnavailableCleanupSweepResult()
    {
        return new KubernetesGatewayTrafficCleanupSweepResult(
            DateTimeOffset.UtcNow,
            "unavailable",
            "Kubernetes Gateway cleanup sweep requires an observation source.",
            CreateCleanupSweepMetadata());
    }

    private KubernetesGatewayTrafficCleanupSweepResult CreateFailedCleanupSweepResult(string error)
    {
        return new KubernetesGatewayTrafficCleanupSweepResult(
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
            ["cleanupStrategy"] = "primary-only",
            ["candidateCount"] = "0",
            ["removedResourceCount"] = "0",
            ["deletedTransferredResourceCount"] = "0",
            ["prunedOrphanResourceCount"] = "0",
            ["primaryCandidateCount"] = "0",
            ["removedPrimaryResourceCount"] = "0",
            ["deletedTransferredPrimaryResourceCount"] = "0",
            ["prunedOrphanPrimaryResourceCount"] = "0",
            ["primaryResourceIds"] = string.Empty,
            ["dependencyCandidateCount"] = "0",
            ["removedDependencyResourceCount"] = "0",
            ["deletedTransferredDependencyResourceCount"] = "0",
            ["prunedOrphanDependencyResourceCount"] = "0",
            ["dependencyResourceIds"] = string.Empty,
            ["dependencyKinds"] = string.Empty,
            ["dependencyNamespaces"] = string.Empty,
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

        metadata["providerAction"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
        metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
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
            conditionId: "gateway-accepted",
            metadataKey: "gatewayAcceptedCondition",
            category: CellTrafficAutomationMaterializationConditionCategories.Readiness,
            trueDescription: "The target Gateway reports Accepted=True.",
            falseDescription: metadata.TryGetValue("gatewayAcceptedMessage", out var gatewayAcceptedMessage) && !string.IsNullOrWhiteSpace(gatewayAcceptedMessage)
                ? gatewayAcceptedMessage
                : "The target Gateway does not currently report Accepted=True.",
            unknownDescription: "The target Gateway Accepted condition is not currently known.");
        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "gateway-programmed",
            metadataKey: "gatewayProgrammedCondition",
            category: CellTrafficAutomationMaterializationConditionCategories.Readiness,
            trueDescription: "The target Gateway reports Programmed=True.",
            falseDescription: metadata.TryGetValue("gatewayProgrammedMessage", out var gatewayProgrammedMessage) && !string.IsNullOrWhiteSpace(gatewayProgrammedMessage)
                ? gatewayProgrammedMessage
                : "The target Gateway does not currently report Programmed=True.",
            unknownDescription: "The target Gateway Programmed condition is not currently known.");
        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "http-route-accepted",
            metadataKey: "httpRouteAcceptedCondition",
            category: CellTrafficAutomationMaterializationConditionCategories.Readiness,
            trueDescription: "The target HTTPRoute reports Accepted=True.",
            falseDescription: metadata.TryGetValue("httpRouteAcceptedMessage", out var httpRouteAcceptedMessage) && !string.IsNullOrWhiteSpace(httpRouteAcceptedMessage)
                ? httpRouteAcceptedMessage
                : "The target HTTPRoute does not currently report Accepted=True.",
            unknownDescription: "The target HTTPRoute Accepted condition is not currently known.");
        AddBooleanCondition(
            conditions,
            metadata,
            conditionId: "http-route-resolved-refs",
            metadataKey: "httpRouteResolvedRefsCondition",
            category: CellTrafficAutomationMaterializationConditionCategories.Dependency,
            trueDescription: "The target HTTPRoute reports ResolvedRefs=True.",
            falseDescription: metadata.TryGetValue("httpRouteResolvedRefsMessage", out var httpRouteResolvedRefsMessage) && !string.IsNullOrWhiteSpace(httpRouteResolvedRefsMessage)
                ? httpRouteResolvedRefsMessage
                : "The target HTTPRoute does not currently report ResolvedRefs=True.",
            unknownDescription: "The target HTTPRoute ResolvedRefs condition is not currently known.");

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
            "gateway-api-status" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Met,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: NormalizeReason(resourceState) ?? "live-status",
                description: "The Kubernetes Gateway runtime reports live control-plane truth."),
            "configured-intent" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "configured-intent",
                description: "The runtime currently exposes projected intent without a live Kubernetes observation."),
            "control-plane-apply" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: NormalizeReason(resourceState) ?? "control-plane-apply",
                description: "The runtime has written intent to the control plane and is waiting for a fresh live observation."),
            "apply-unavailable" or "observation-unavailable" => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: NormalizeReason(resourceState) ?? "observation-unavailable",
                description: "The runtime cannot currently reach the Kubernetes API to reconcile or observe traffic materialization."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Observation,
                "runtime-observable",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: NormalizeReason(resourceState) ?? "observation-error",
                description: "The runtime encountered an error while reading or reconciling Kubernetes Gateway materialization.")
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
                description: "The Kubernetes HTTPRoute is currently owned by the active Cephalon automation."),
            CellTrafficAutomationOwnershipStates.Requested => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: ownershipReason ?? "requested",
                description: "The Kubernetes HTTPRoute has been requested but is not yet confirmed as Cephalon-owned."),
            CellTrafficAutomationOwnershipStates.Orphaned or CellTrafficAutomationOwnershipStates.Transferred or CellTrafficAutomationOwnershipStates.Pruned => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: ownershipReason ?? normalizedState,
                description: "The Kubernetes HTTPRoute carries stale or transitional Cephalon ownership metadata."),
            CellTrafficAutomationOwnershipStates.OwnershipConflict => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: ownershipReason ?? normalizedState,
                description: "The Kubernetes HTTPRoute is not safely owned by the active Cephalon automation."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Ownership,
                "ownership",
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: ownershipReason ?? "unknown",
                description: "The runtime cannot currently determine Kubernetes HTTPRoute ownership.")
        };
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateDependencyCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("dependencyState", out var dependencyState);
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
                description: "The expected Gateway and HTTPRoute dependency posture is currently satisfied."),
            CellTrafficAutomationDependencyStates.Missing => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Error,
                reason: "missing",
                description: "One or more required Kubernetes Gateway dependencies are missing."),
            CellTrafficAutomationDependencyStates.Mixed => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: "mixed",
                description: "Only part of the expected Kubernetes Gateway dependency posture is currently satisfied."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Dependency,
                "dependencies",
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "unknown",
                description: "The runtime cannot currently determine Kubernetes Gateway dependency posture.")
        };
    }

    private static CellTrafficAutomationMaterializationConditionDescriptor CreateDriftCondition(
        Dictionary<string, string> metadata)
    {
        metadata.TryGetValue("driftState", out var driftState);
        metadata.TryGetValue("driftReasons", out var driftReasons);
        var description = string.IsNullOrWhiteSpace(driftReasons)
            ? null
            : $"Observed Kubernetes Gateway state drifts for: {driftReasons}.";
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
                description: "Observed Kubernetes Gateway state matches the authored Cephalon intent."),
            CellTrafficAutomationDriftStates.Reconciling => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Pending,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "reconciling",
                description: "Observed Kubernetes Gateway state is still converging toward the authored Cephalon intent."),
            CellTrafficAutomationDriftStates.Drifted => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Unmet,
                CellTrafficAutomationMaterializationConditionSeverities.Warning,
                reason: "drifted",
                description: description ?? "Observed Kubernetes Gateway state drifts from the authored Cephalon intent."),
            _ => new CellTrafficAutomationMaterializationConditionDescriptor(
                CellTrafficAutomationMaterializationConditionDimensions.Provider,
                CellTrafficAutomationMaterializationConditionCategories.Drift,
                "intent-alignment",
                CellTrafficAutomationMaterializationConditionStates.Unknown,
                CellTrafficAutomationMaterializationConditionSeverities.Info,
                reason: "unknown",
                description: "The runtime cannot currently determine Kubernetes Gateway drift posture.")
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
            description: $"The active Kubernetes Gateway lifecycle posture is '{normalizedAction}'.");
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
