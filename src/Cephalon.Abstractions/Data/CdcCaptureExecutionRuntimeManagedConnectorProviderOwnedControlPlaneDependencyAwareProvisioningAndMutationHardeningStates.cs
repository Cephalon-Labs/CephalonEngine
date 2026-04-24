namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned control-plane dependency-aware provisioning and mutation hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates
{
    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening is currently ready on the shared runtime surface.
    /// </summary>
    public const string DependencyReady = "dependency-ready";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning currently remains blocked by missing dependency truth, task topology, or provisioning posture.
    /// </summary>
    public const string ProvisioningBlocked = "provisioning-blocked";

    /// <summary>
    /// Provider-owned control-plane dependency-aware mutation currently remains blocked by missing dependency truth, task topology, or execution targeting.
    /// </summary>
    public const string MutationBlocked = "mutation-blocked";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening is currently degraded by mismatched, stale, or incomplete dependency observations.
    /// </summary>
    public const string DependencyDegraded = "dependency-degraded";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning hardening is fully hardened on the shared runtime surface.
    /// </summary>
    public const string ProvisioningHardened = "provisioning-hardened";

    /// <summary>
    /// Provider-owned control-plane dependency-aware mutation hardening is fully hardened on the shared runtime surface.
    /// </summary>
    public const string MutationHardened = "mutation-hardened";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening currently remains risky because broader provider or runtime truth is not safe enough yet.
    /// </summary>
    public const string DependencyRisk = "dependency-risk";
}
