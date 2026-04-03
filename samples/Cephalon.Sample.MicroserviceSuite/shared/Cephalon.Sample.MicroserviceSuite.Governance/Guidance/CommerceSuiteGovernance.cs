using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;
using Cephalon.Sample.MicroserviceSuite.Governance.Contracts;

namespace Cephalon.Sample.MicroserviceSuite.Governance.Guidance;

/// <summary>
/// Provides the shared governance profile and additive gateway/control-plane guidance for the microservice-suite sample.
/// </summary>
public static class CommerceSuiteGovernance
{
    /// <summary>
    /// Gets the shared governance package identifier used by the sample suite.
    /// </summary>
    public const string GovernancePackageId = "Cephalon.Sample.MicroserviceSuite.Governance";

    /// <summary>
    /// Gets the shared governance profile name used by the sample suite.
    /// </summary>
    public const string GovernanceProfile = "commerce-suite-governance";

    /// <summary>
    /// Gets the shared route prefix recommended for one optional gateway host.
    /// </summary>
    public const string GatewayRouteRoot = "/suite/commerce";

    /// <summary>
    /// Gets the shared path recommended for one optional operator-facing control-plane host.
    /// </summary>
    public const string ControlPlanePath = "/suite/control-plane/runtime";

    /// <summary>
    /// Builds the shared governance snapshot returned by one suite service.
    /// </summary>
    /// <param name="service">
    /// The governed service boundary producing the response.
    /// </param>
    /// <returns>
    /// The governance snapshot for the requested service.
    /// </returns>
    public static SuiteGovernanceSnapshotContract CreateSnapshot(string service)
    {
        var normalizedService = NormalizeService(service);

        return new SuiteGovernanceSnapshotContract(
            Suite: CommerceSuiteConventions.SuiteName,
            Service: normalizedService,
            GovernancePackage: GovernancePackageId,
            GovernanceProfile: GovernanceProfile,
            CapabilityKey: ResolveCapabilityKey(normalizedService),
            Gateway: new OptionalGatewayGuidanceContract(
                RoutePrefix: ResolveGatewayRoutePrefix(normalizedService),
                Purpose: "Expose one suite-facing route without moving service logic or ownership out of the service host.",
                Notes: "Keep the gateway additive: it can aggregate or proxy routes, but the service host stays authoritative for capabilities and runtime behavior.",
                IsOptional: true),
            ControlPlane: new OptionalControlPlaneGuidanceContract(
                Path: ControlPlanePath,
                Purpose: "Aggregate operator-facing runtime answers such as snapshot, runtime-story, diagnostics, and package metadata from the existing services.",
                Notes: "Keep the control plane additive and consume the existing /engine/* surfaces instead of inventing a new engine-owned coordinator abstraction.",
                IsOptional: true));
    }

    private static string NormalizeService(string service)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        var normalizedService = service.Trim();

        if (!CommerceSuiteConventions.KnownServices.Contains(normalizedService, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Service '{service}' does not belong to the '{CommerceSuiteConventions.SuiteName}' governance profile.");
        }

        return normalizedService;
    }

    private static string ResolveCapabilityKey(string service)
    {
        return service switch
        {
            var value when string.Equals(value, CommerceSuiteConventions.CatalogService, StringComparison.OrdinalIgnoreCase) => "catalog.overview",
            var value when string.Equals(value, CommerceSuiteConventions.OrdersService, StringComparison.OrdinalIgnoreCase) => "orders.coordination",
            _ => throw new InvalidOperationException($"Service '{service}' does not have a governed capability mapping.")
        };
    }

    private static string ResolveGatewayRoutePrefix(string service)
    {
        return service switch
        {
            var value when string.Equals(value, CommerceSuiteConventions.CatalogService, StringComparison.OrdinalIgnoreCase) => $"{GatewayRouteRoot}/catalog",
            var value when string.Equals(value, CommerceSuiteConventions.OrdersService, StringComparison.OrdinalIgnoreCase) => $"{GatewayRouteRoot}/orders",
            _ => throw new InvalidOperationException($"Service '{service}' does not have a gateway route mapping.")
        };
    }
}
