namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned control-plane ownership answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates
{
    /// <summary>
    /// Provider-owned control-plane ownership does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned control-plane ownership still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned control-plane ownership is currently ready on the shared runtime surface.
    /// </summary>
    public const string OwnershipReady = "ownership-ready";

    /// <summary>
    /// Provider-owned control-plane ownership remains blocked by shared runtime policy, scheduler, or broader ownership truth.
    /// </summary>
    public const string OwnershipBlocked = "ownership-blocked";

    /// <summary>
    /// Provider-owned control-plane ownership is currently active on one bounded provider-facing step.
    /// </summary>
    public const string OwnershipActive = "ownership-active";

    /// <summary>
    /// Provider-owned control-plane ownership is partially available but still depends on bounded operator or shared runtime conditions.
    /// </summary>
    public const string OwnershipPartial = "ownership-partial";

    /// <summary>
    /// Provider-owned control-plane ownership currently remains risky because broader shared runtime truth is not safe enough yet.
    /// </summary>
    public const string OwnershipRisk = "ownership-risk";
}
