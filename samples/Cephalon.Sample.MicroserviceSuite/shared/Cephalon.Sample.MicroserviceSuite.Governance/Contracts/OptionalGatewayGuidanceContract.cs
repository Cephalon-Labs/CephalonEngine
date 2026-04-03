namespace Cephalon.Sample.MicroserviceSuite.Governance.Contracts;

/// <summary>
/// Describes one optional gateway-layer recommendation for the suite sample.
/// </summary>
/// <param name="RoutePrefix">
/// The suggested gateway route prefix for the governed service.
/// </param>
/// <param name="Purpose">
/// The service-facing purpose of the suggested gateway route.
/// </param>
/// <param name="Notes">
/// Additional guidance that keeps the gateway layering additive.
/// </param>
/// <param name="IsOptional">
/// Indicates whether the gateway route is optional instead of required for the suite to function.
/// </param>
public sealed record OptionalGatewayGuidanceContract(
    string RoutePrefix,
    string Purpose,
    string Notes,
    bool IsOptional);
