namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector distributed retry orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates
{
    /// <summary>
    /// Distributed retry orchestration does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Distributed retry orchestration is currently disabled for the runtime.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// Distributed retry orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Distributed retry orchestration is waiting for the active cooldown window to elapse.
    /// </summary>
    public const string Cooldown = "cooldown";

    /// <summary>
    /// Distributed retry orchestration remains blocked by lease, durability, or safety truth.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// Distributed retry orchestration can schedule one bounded automatic retry attempt on the current node.
    /// </summary>
    public const string Scheduled = "scheduled";

    /// <summary>
    /// Distributed retry orchestration does not currently need to schedule another retry attempt.
    /// </summary>
    public const string Completed = "completed";
}
