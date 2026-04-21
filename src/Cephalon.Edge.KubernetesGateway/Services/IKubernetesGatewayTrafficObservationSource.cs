using Cephalon.Abstractions.Technologies;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal interface IKubernetesGatewayTrafficObservationSource
{
    ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        KubernetesGatewayTrafficRouteProjection projection,
        CancellationToken cancellationToken = default);

    ValueTask<KubernetesGatewayTrafficCleanupSweepResult> SweepCleanupAsync(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> activeAutomations,
        IReadOnlyCollection<KubernetesGatewayTrafficRouteProjection> activeProjections,
        CancellationToken cancellationToken = default);
}
