using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;

namespace Cephalon.Edge.Traefik.Services;

internal sealed class TraefikTrafficAutomationMaterializer : ICellTrafficAutomationProviderMaterializer
{
    private readonly IReadOnlyDictionary<string, TraefikIngressRouteProjection> projectionsByRouteId;
    private readonly ITraefikTrafficObservationSource? observationSource;
    private readonly string controlPlaneMode;
    private readonly int observationPollingIntervalSeconds;

    public TraefikTrafficAutomationMaterializer(
        TraefikTrafficProjectionCatalog projections,
        TraefikTrafficMaterializerOptions options,
        ITraefikTrafficObservationSource? observationSource = null)
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
        controlPlaneMode = TraefikTrafficObservationModes.Normalize(options.Observation.Mode);
        observationPollingIntervalSeconds = Math.Max(1, options.Observation.PollingIntervalSeconds);
    }

    public string MaterializerId { get; }

    public string ProviderId { get; }

    public int Priority { get; }

    internal bool SupportsLiveObservation =>
        string.Equals(
            controlPlaneMode,
            TraefikTrafficObservationModes.ObserveOnly,
            StringComparison.OrdinalIgnoreCase);

    internal TimeSpan ObservationPollingInterval =>
        TimeSpan.FromSeconds(observationPollingIntervalSeconds);

    public bool CanMaterialize(CellTrafficAutomationRuntimeDescriptor automation)
    {
        ArgumentNullException.ThrowIfNull(automation);

        return UsesProviderMaterialization(automation.MaterializationMode) &&
            string.Equals(automation.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase) &&
            projectionsByRouteId.ContainsKey(automation.RouteId);
    }

    public ValueTask<CellTrafficAutomationProviderMaterializationResult> MaterializeAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);
        cancellationToken.ThrowIfCancellationRequested();

        if (!projectionsByRouteId.TryGetValue(automation.RouteId, out var projection))
        {
            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Unavailable,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' has no projection for route '{automation.RouteId}'."));
        }

        if (SupportsLiveObservation)
        {
            return ObserveAsync(automation, cancellationToken);
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

        return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Pending,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata));
    }

    internal ValueTask<CellTrafficAutomationProviderMaterializationResult> RefreshAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);

        return SupportsLiveObservation
            ? ObserveAsync(automation, cancellationToken)
            : MaterializeAsync(automation, cancellationToken);
    }

    internal ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);

        if (!projectionsByRouteId.TryGetValue(automation.RouteId, out var projection))
        {
            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
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

            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Traefik traffic materializer '{MaterializerId}' is configured for observe-only mode, but no observation source is active.",
                metadata: metadata));
        }

        return observationSource.ObserveAsync(automation, projection, cancellationToken);
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
