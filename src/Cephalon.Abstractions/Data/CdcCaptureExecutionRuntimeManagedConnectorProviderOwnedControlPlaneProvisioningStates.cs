namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned control-plane provisioning answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates
{
    /// <summary>
    /// Provider-owned control-plane provisioning does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned control-plane provisioning still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned control-plane provisioning is currently ready on the shared runtime surface.
    /// </summary>
    public const string ProvisioningReady = "provisioning-ready";

    /// <summary>
    /// Provider-owned control-plane provisioning remains blocked by shared runtime policy, control-plane truth, or missing provisioning intent.
    /// </summary>
    public const string ProvisioningBlocked = "provisioning-blocked";

    /// <summary>
    /// Provider-owned control-plane provisioning is currently executing one bounded provider-facing step.
    /// </summary>
    public const string ProvisioningExecuting = "provisioning-executing";

    /// <summary>
    /// Provider-owned control-plane provisioning is partially available but still needs additional shared provider truth before Cephalon can rely on it fully.
    /// </summary>
    public const string ProvisioningPartial = "provisioning-partial";

    /// <summary>
    /// Provider-owned control-plane provisioning currently remains risky because broader shared runtime truth is not safe enough yet.
    /// </summary>
    public const string ProvisioningRisk = "provisioning-risk";
}
