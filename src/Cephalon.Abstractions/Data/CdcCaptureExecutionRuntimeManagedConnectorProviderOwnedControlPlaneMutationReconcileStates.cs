namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned control-plane mutation and reconcile answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStates
{
    /// <summary>
    /// Provider-owned control-plane mutation and reconcile does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned control-plane mutation and reconcile still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned control-plane mutation is currently ready on the shared runtime surface.
    /// </summary>
    public const string MutationReady = "mutation-ready";

    /// <summary>
    /// Provider-owned control-plane reconcile is currently ready on the shared runtime surface.
    /// </summary>
    public const string ReconcileReady = "reconcile-ready";

    /// <summary>
    /// Provider-owned control-plane mutation remains blocked by shared runtime policy, control-plane truth, or missing command intent.
    /// </summary>
    public const string MutationBlocked = "mutation-blocked";

    /// <summary>
    /// Provider-owned control-plane reconcile remains blocked by shared runtime policy, control-plane truth, or missing command intent.
    /// </summary>
    public const string ReconcileBlocked = "reconcile-blocked";

    /// <summary>
    /// Provider-owned control-plane mutation or reconcile is currently executing one bounded provider-facing step.
    /// </summary>
    public const string MutationExecuting = "mutation-executing";

    /// <summary>
    /// Provider-owned control-plane mutation or reconcile currently remains risky because broader shared runtime truth is not safe enough yet.
    /// </summary>
    public const string MutationRisk = "mutation-risk";
}
