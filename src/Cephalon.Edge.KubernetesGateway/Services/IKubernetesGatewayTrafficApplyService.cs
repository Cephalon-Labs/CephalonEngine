using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal interface IKubernetesGatewayTrafficApplyService
{
    ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        KubernetesGatewayTrafficRouteProjection projection,
        CancellationToken cancellationToken = default);
}
