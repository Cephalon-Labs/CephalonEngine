namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector broader multi-node lease-execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates
{
    /// <summary>
    /// Multi-node lease execution does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Multi-node lease execution still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// The runtime currently executes automatic retry on a single node without cross-node lease ownership.
    /// </summary>
    public const string SingleNode = "single-node";

    /// <summary>
    /// The current node can execute the next bounded automatic retry step under the active multi-node lease posture.
    /// </summary>
    public const string LeaseExecutable = "lease-executable";

    /// <summary>
    /// The current node cannot yet execute the next bounded automatic retry step even though lease execution applies.
    /// </summary>
    public const string LeaseBlocked = "lease-blocked";

    /// <summary>
    /// The current multi-node lease posture remains conflicted across nodes.
    /// </summary>
    public const string LeaseConflicted = "lease-conflicted";

    /// <summary>
    /// The current multi-node lease posture remains risky because ownership truth still looks stale.
    /// </summary>
    public const string StaleLeaseRisk = "stale-lease-risk";
}
