namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector broader multi-node lease-execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories
{
    /// <summary>
    /// The runtime currently executes automatic retry on a single node without cross-node lease ownership.
    /// </summary>
    public const string SingleNodeRuntime = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.SingleNodeRuntime;

    /// <summary>
    /// The runtime depends on cross-node lease ownership before automatic retry should execute.
    /// </summary>
    public const string LeaseCoordinatedRuntime = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.LeaseCoordinatedRuntime;

    /// <summary>
    /// Multi-node lease execution still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly;

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.OwnerMatch;

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.OwnerMismatch;

    /// <summary>
    /// The current runtime still exposes one active reporter identifier.
    /// </summary>
    public const string ActiveReporterVisible = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.ActiveReporterVisible;

    /// <summary>
    /// The current runtime still exposes one active reporter lease.
    /// </summary>
    public const string ActiveLeaseVisible = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.ActiveLeaseVisible;

    /// <summary>
    /// The current runtime still exposes an active retry lease.
    /// </summary>
    public const string LeaseHeld = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.LeaseHeld;

    /// <summary>
    /// The current runtime does not currently expose an active retry lease.
    /// </summary>
    public const string LeaseMissing = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.LeaseMissing;

    /// <summary>
    /// The current runtime still exposes conflicting retry lease ownership.
    /// </summary>
    public const string LeaseConflict = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.LeaseConflict;

    /// <summary>
    /// Cross-node idempotency currently looks safe for the current multi-node lease posture.
    /// </summary>
    public const string CrossNodeIdempotentSafe = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.CrossNodeIdempotentSafe;

    /// <summary>
    /// Cross-node idempotency currently remains risky for the current multi-node lease posture.
    /// </summary>
    public const string CrossNodeIdempotencyRisk = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CrossNodeIdempotencyRisk;

    /// <summary>
    /// The shared bounded retry scheduler is currently disabled for the runtime.
    /// </summary>
    public const string SchedulerDisabled = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.SchedulerDisabled;

    /// <summary>
    /// The current retry policy is still waiting for a cooldown window to elapse.
    /// </summary>
    public const string CooldownWindow = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CooldownWindow;

    /// <summary>
    /// The current shared runtime truth does not currently need another automatic retry attempt.
    /// </summary>
    public const string NoFurtherRetryNeeded = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.NoFurtherRetryNeeded;

    /// <summary>
    /// The current node can execute the next bounded automatic retry step.
    /// </summary>
    public const string CurrentNodeExecutable = "current-node-executable";

    /// <summary>
    /// The current node cannot yet execute the next bounded automatic retry step.
    /// </summary>
    public const string CurrentNodeBlocked = "current-node-blocked";

    /// <summary>
    /// The current multi-node lease posture allows execution on this node.
    /// </summary>
    public const string LeaseExecutable = "lease-executable";

    /// <summary>
    /// The current multi-node lease posture still blocks execution on this node.
    /// </summary>
    public const string LeaseBlocked = "lease-blocked";

    /// <summary>
    /// The current multi-node lease posture still looks stale.
    /// </summary>
    public const string StaleLeaseRisk = "stale-lease-risk";
}
