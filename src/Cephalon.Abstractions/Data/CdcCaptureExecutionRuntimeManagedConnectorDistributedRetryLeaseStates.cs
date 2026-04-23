namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector distributed retry lease answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates
{
    /// <summary>
    /// Distributed retry lease posture does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Automatic retry can run on the current node without cross-node coordination.
    /// </summary>
    public const string SingleNode = "single-node";

    /// <summary>
    /// The current node holds the active retry lease, but Cephalon has not yet proven cross-node idempotency from retained history.
    /// </summary>
    public const string LeaseHeld = "lease-held";

    /// <summary>
    /// Automatic retry cannot run because no active retry lease is currently visible.
    /// </summary>
    public const string LeaseMissing = "lease-missing";

    /// <summary>
    /// Automatic retry cannot run because lease ownership or multi-node coordination remains conflicted.
    /// </summary>
    public const string LeaseConflicted = "lease-conflicted";

    /// <summary>
    /// Automatic retry can run because Cephalon has both lease ownership and restart-safe idempotency evidence.
    /// </summary>
    public const string IdempotentSafe = "idempotent-safe";

    /// <summary>
    /// Automatic retry cannot run because cross-node idempotency evidence is incomplete or unsafe.
    /// </summary>
    public const string IdempotencyRisk = "idempotency-risk";

    /// <summary>
    /// Distributed retry lease posture still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";
}
