using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.Traefik.Services;

internal interface ITraefikTrafficObservationSource
{
    ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken = default);
}
