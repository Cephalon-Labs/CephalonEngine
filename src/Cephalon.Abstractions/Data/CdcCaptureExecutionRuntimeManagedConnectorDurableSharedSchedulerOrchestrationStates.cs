namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector durable shared scheduler-orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates
{
    /// <summary>
    /// Durable shared scheduler orchestration does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Durable shared scheduler orchestration is currently disabled for the runtime.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// Durable shared scheduler orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Durable shared scheduler orchestration currently does not need to keep the runtime scheduled.
    /// </summary>
    public const string Unscheduled = "unscheduled";

    /// <summary>
    /// Durable shared scheduler orchestration can currently keep one bounded automatic retry scheduled on the current node.
    /// </summary>
    public const string Scheduled = "scheduled";

    /// <summary>
    /// Durable shared scheduler orchestration remains blocked by broader lease-execution truth.
    /// </summary>
    public const string LeaseBlocked = "lease-blocked";

    /// <summary>
    /// Durable shared scheduler orchestration still needs durable journal recovery or persistence hardening.
    /// </summary>
    public const string RecoveryNeeded = "recovery-needed";

    /// <summary>
    /// Durable shared scheduler orchestration remains conflicted across coordination or lease ownership truth.
    /// </summary>
    public const string SchedulerConflicted = "scheduler-conflicted";
}
