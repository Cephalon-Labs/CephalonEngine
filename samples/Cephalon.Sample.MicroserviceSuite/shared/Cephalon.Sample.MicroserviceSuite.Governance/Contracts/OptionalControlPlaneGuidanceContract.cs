namespace Cephalon.Sample.MicroserviceSuite.Governance.Contracts;

/// <summary>
/// Describes one optional control-plane layering recommendation for the suite sample.
/// </summary>
/// <param name="Path">
/// The suggested control-plane path exposed by a future operator-facing host.
/// </param>
/// <param name="Purpose">
/// The operator-facing purpose of the suggested control-plane path.
/// </param>
/// <param name="Notes">
/// Additional guidance that keeps the control-plane layering additive.
/// </param>
/// <param name="IsOptional">
/// Indicates whether the control-plane path is optional instead of required for the suite to function.
/// </param>
public sealed record OptionalControlPlaneGuidanceContract(
    string Path,
    string Purpose,
    string Notes,
    bool IsOptional);
