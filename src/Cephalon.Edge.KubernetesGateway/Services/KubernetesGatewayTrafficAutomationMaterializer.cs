using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficAutomationMaterializer : ICellTrafficAutomationProviderMaterializer
{
    private readonly IReadOnlyDictionary<string, KubernetesGatewayTrafficRouteProjection> projectionsByRouteId;

    public KubernetesGatewayTrafficAutomationMaterializer(KubernetesGatewayTrafficMaterializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        MaterializerId = string.IsNullOrWhiteSpace(options.MaterializerId)
            ? throw new InvalidOperationException("Kubernetes Gateway traffic materializer requires a materializer id.")
            : options.MaterializerId.Trim();
        ProviderId = string.IsNullOrWhiteSpace(options.ProviderId)
            ? throw new InvalidOperationException("Kubernetes Gateway traffic materializer requires a provider id.")
            : options.ProviderId.Trim();
        Priority = options.Priority;
        projectionsByRouteId = KubernetesGatewayTrafficProjectionBuilder.Build(options);
    }

    public string MaterializerId { get; }

    public string ProviderId { get; }

    public int Priority { get; }

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

        if (!projectionsByRouteId.TryGetValue(automation.RouteId, out var projection))
        {
            return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
                state: CellTrafficAutomationProviderMaterializationStates.Unavailable,
                observedAtUtc: DateTimeOffset.UtcNow,
                error: $"Kubernetes Gateway traffic materializer '{MaterializerId}' has no projection for route '{automation.RouteId}'."));
        }

        var metadata = projection.CreateMetadata();
        metadata["providerAction"] = "projected-intent";

        return ValueTask.FromResult(new CellTrafficAutomationProviderMaterializationResult(
            state: CellTrafficAutomationProviderMaterializationStates.Applied,
            observedAtUtc: DateTimeOffset.UtcNow,
            metadata: metadata));
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
