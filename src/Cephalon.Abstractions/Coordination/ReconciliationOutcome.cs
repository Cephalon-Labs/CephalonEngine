namespace Cephalon.Abstractions.Coordination;

/// <summary>Describes the local execution outcome without implying durable recovery.</summary>
public enum ReconciliationOutcome
{
    /// <summary>The operation is reserved and may be executing.</summary>
    Running,
    /// <summary>The effect confirmed the desired revision.</summary>
    Applied,
    /// <summary>The supplied observation already matched the desired revision.</summary>
    Converged,
    /// <summary>A precondition failed without an effect.</summary>
    Stale,
    /// <summary>The plan was outside its validity window before an effect.</summary>
    Expired,
    /// <summary>Cancellation occurred when no effect was outstanding.</summary>
    Canceled,
    /// <summary>Only confirmed no-effect retries occurred and the attempt budget was consumed.</summary>
    Exhausted,
    /// <summary>The action was rejected without mutation.</summary>
    Rejected,
    /// <summary>An effect may have occurred; automatic retry is prohibited.</summary>
    InDoubt,
    /// <summary>The same tenant/operation identity was bound to another plan.</summary>
    Conflict,
    /// <summary>The local reservation capacity was reached; no invocation occurred.</summary>
    CapacityExceeded,
}
