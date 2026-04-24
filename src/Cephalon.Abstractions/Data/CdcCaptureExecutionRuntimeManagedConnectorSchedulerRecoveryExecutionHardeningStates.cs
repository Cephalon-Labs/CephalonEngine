namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector scheduler recovery and execution-hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates
{
    /// <summary>
    /// Scheduler recovery and execution hardening does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Scheduler recovery and execution hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Scheduler recovery completed enough that the current node can safely resume bounded execution.
    /// </summary>
    public const string RecoveryReady = "recovery-ready";

    /// <summary>
    /// Scheduler recovery remains blocked by missing, unhealthy, or incomplete durable journal evidence.
    /// </summary>
    public const string RecoveryBlocked = "recovery-blocked";

    /// <summary>
    /// Scheduler recovery is currently replaying bounded execution evidence on the current node.
    /// </summary>
    public const string Replaying = "replaying";

    /// <summary>
    /// Scheduler execution truth currently looks hardened enough for truthful bounded execution on the shared lane.
    /// </summary>
    public const string ExecutionHardened = "execution-hardened";

    /// <summary>
    /// Scheduler execution still remains risky because lease, scheduler, or automatic execution truth is not yet safe enough.
    /// </summary>
    public const string ExecutionRisk = "execution-risk";
}
