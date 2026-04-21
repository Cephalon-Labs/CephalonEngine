using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.Traefik.Services;

internal interface ITraefikTrafficApplyService
{
    ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken = default);
}
