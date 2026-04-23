namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector automatic background retry coordination answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates
{
    /// <summary>
    /// Automatic background retry coordination does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Automatic background retry can run on the current node without reporter-lease coordination.
    /// </summary>
    public const string SingleNode = "single-node";

    /// <summary>
    /// Automatic background retry remains uncoordinated on the current node.
    /// </summary>
    public const string Uncoordinated = "uncoordinated";

    /// <summary>
    /// The current node holds the active reporter lease for automatic background retry.
    /// </summary>
    public const string LeaseHeld = "lease-held";

    /// <summary>
    /// Automatic background retry cannot run because no active reporter lease is currently visible.
    /// </summary>
    public const string LeaseMissing = "lease-missing";

    /// <summary>
    /// Automatic background retry cannot run because reporter coordination is currently conflicted.
    /// </summary>
    public const string Conflicted = "conflicted";

    /// <summary>
    /// Automatic background retry still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";
}
