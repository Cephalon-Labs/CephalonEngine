using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.Traefik.Services;

internal interface ITraefikTrafficObservationSource
{
    ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken = default);

    ValueTask<TraefikTrafficCleanupSweepResult> SweepCleanupAsync(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> activeAutomations,
        IReadOnlyCollection<TraefikIngressRouteProjection> activeProjections,
        CancellationToken cancellationToken = default);
}
