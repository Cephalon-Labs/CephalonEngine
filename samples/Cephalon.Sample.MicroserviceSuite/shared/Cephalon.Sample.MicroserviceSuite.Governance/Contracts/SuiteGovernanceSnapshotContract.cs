namespace Cephalon.Sample.MicroserviceSuite.Governance.Contracts;

/// <summary>
/// Represents the shared governance snapshot returned by the microservice-suite sample services.
/// </summary>
/// <param name="Suite">
/// The logical suite name governed by the shared package.
/// </param>
/// <param name="Service">
/// The service boundary that owns the described governance snapshot.
/// </param>
/// <param name="GovernancePackage">
/// The shared governance package identifier reused by the suite.
/// </param>
/// <param name="GovernanceProfile">
/// The suite-level governance profile enforced by the shared package.
/// </param>
/// <param name="CapabilityKey">
/// The service capability that stays authoritative even when a gateway or control plane is added later.
/// </param>
/// <param name="Gateway">
/// The optional gateway guidance for the governed service.
/// </param>
/// <param name="ControlPlane">
/// The optional control-plane guidance for the governed service.
/// </param>
public sealed record SuiteGovernanceSnapshotContract(
    string Suite,
    string Service,
    string GovernancePackage,
    string GovernanceProfile,
    string CapabilityKey,
    OptionalGatewayGuidanceContract Gateway,
    OptionalControlPlaneGuidanceContract ControlPlane);
