namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates
{
    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening is currently ready on the shared runtime surface.
    /// </summary>
    public const string DependencyReady = "dependency-ready";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening remains blocked by missing dependency truth, task topology, or execution targeting.
    /// </summary>
    public const string DependencyBlocked = "dependency-blocked";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening is currently degraded by mismatched, stale, or incomplete dependency observations.
    /// </summary>
    public const string DependencyDegraded = "dependency-degraded";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening is fully hardened on the shared runtime surface.
    /// </summary>
    public const string ApplyAndReconcileHardened = "apply-and-reconcile-hardened";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening currently remains risky because broader provider or runtime truth is not safe enough yet.
    /// </summary>
    public const string DependencyRisk = "dependency-risk";
}
