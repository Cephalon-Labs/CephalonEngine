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
            metadata: metadata));
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
                metadata: metadata));
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
                metadata: metadata));
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
                metadata: metadata));
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
            return result;
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

        return new CellTrafficAutomationProviderMaterializationResult(
            state: result.State,
            observedAtUtc: result.ObservedAtUtc,
            error: result.Error,
            metadata: metadata);
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
            metadata: metadata);
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
}
