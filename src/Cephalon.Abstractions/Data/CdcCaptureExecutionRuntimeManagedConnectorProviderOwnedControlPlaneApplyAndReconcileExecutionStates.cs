namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned control-plane apply-and-reconcile execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates
{
    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution is currently ready on the shared runtime surface.
    /// </summary>
    public const string ApplyAndReconcileReady = "apply-and-reconcile-ready";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution remains blocked by shared runtime policy, control-plane truth, or missing execution intent.
    /// </summary>
    public const string ApplyAndReconcileBlocked = "apply-and-reconcile-blocked";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution is currently exercising one bounded provider-facing step.
    /// </summary>
    public const string ApplyAndReconcileExecuting = "apply-and-reconcile-executing";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution no longer needs another shared execution step for the current operation.
    /// </summary>
    public const string ApplyAndReconcileCompleted = "apply-and-reconcile-completed";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution currently remains risky because broader shared runtime truth is not safe enough yet.
    /// </summary>
    public const string ApplyAndReconcileRisk = "apply-and-reconcile-risk";
}
