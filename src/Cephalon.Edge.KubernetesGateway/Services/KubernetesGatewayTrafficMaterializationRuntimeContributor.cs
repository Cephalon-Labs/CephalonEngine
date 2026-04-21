using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;
using Cephalon.Engine.Technologies;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficMaterializationRuntimeContributor(
    KubernetesGatewayTrafficMaterializerOptions options,
    ICellTrafficAutomationRuntimeCatalog catalog) : ITechnologyRuntimeContributor
{
    private readonly IReadOnlyDictionary<string, KubernetesGatewayTrafficRouteProjection> projectionsByRouteId =
        KubernetesGatewayTrafficProjectionBuilder.Build(options);
    private readonly StringComparer comparer = StringComparer.OrdinalIgnoreCase;
    private readonly string materializerId = string.IsNullOrWhiteSpace(options.MaterializerId)
        ? KubernetesGatewayTrafficMaterializerOptions.DefaultMaterializerId
        : options.MaterializerId.Trim();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: BuiltInTechnologies.CellBasedArchitecture.Id,
            surfaceId: "kubernetes-gateway-traffic-materializations",
            displayName: "Kubernetes Gateway Traffic Materializations",
            description: "Provider-managed Kubernetes Gateway API intent projected from the shared cell traffic automation catalog.",
            entries: catalog.Automations
                .Where(OwnsAutomation)
                .Select(CreateEntry)
                .ToArray());
    }

    private bool OwnsAutomation(CellTrafficAutomationRuntimeDescriptor automation)
    {
        ArgumentNullException.ThrowIfNull(automation);

        return comparer.Equals(automation.ProviderMaterializerId, materializerId) &&
            projectionsByRouteId.ContainsKey(automation.RouteId);
    }

    private TechnologyRuntimeEntry CreateEntry(CellTrafficAutomationRuntimeDescriptor automation)
    {
        var projection = projectionsByRouteId[automation.RouteId];
        var metadata = projection.CreateMetadata();
        metadata["routeId"] = automation.RouteId;
        metadata["automationId"] = automation.Id;
        metadata["providerId"] = automation.ProviderId ?? string.Empty;
        metadata["materializerId"] = automation.ProviderMaterializerId ?? materializerId;
        metadata["providerMaterializationState"] = automation.ProviderMaterializationState ?? CellTrafficAutomationProviderMaterializationStates.Unavailable;
        metadata["materializationMode"] = automation.MaterializationMode;
        metadata["policySource"] = automation.PolicySource;
        metadata["sourceCellId"] = automation.SourceCellId;
        metadata["targetCellId"] = automation.TargetCellId;

        if (automation.ProviderMaterializationObservedAtUtc is not null)
        {
            metadata["providerMaterializationObservedAtUtc"] =
                automation.ProviderMaterializationObservedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(automation.ProviderMaterializationError))
        {
            metadata["providerMaterializationError"] = automation.ProviderMaterializationError!;
        }

        foreach (var pair in automation.RuntimeMetadata)
        {
            if (!pair.Key.StartsWith("providerMaterialization.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = pair.Key["providerMaterialization.".Length..];
            metadata[suffix] = pair.Value;
        }

        return new TechnologyRuntimeEntry(
            id: automation.Id,
            displayName: automation.DisplayName,
            description: $"Kubernetes Gateway API materialization for '{automation.DisplayName}'.",
            metadata: metadata);
    }
}
