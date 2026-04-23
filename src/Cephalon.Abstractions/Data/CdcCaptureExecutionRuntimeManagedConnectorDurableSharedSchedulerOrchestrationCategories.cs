namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector durable shared scheduler-orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories
{
    /// <summary>
    /// The runtime participates in the durable shared scheduler-orchestration lane.
    /// </summary>
    public const string DurableSharedScheduler = "durable-shared-scheduler";

    /// <summary>
    /// Durable shared scheduler orchestration currently keeps one bounded retry scheduled on the current node.
    /// </summary>
    public const string Scheduled = "scheduled";

    /// <summary>
    /// Durable shared scheduler orchestration currently does not need to keep the runtime scheduled.
    /// </summary>
    public const string Unscheduled = "unscheduled";

    /// <summary>
    /// Durable shared scheduler orchestration currently still needs durable journal recovery or persistence hardening.
    /// </summary>
    public const string RecoveryNeeded = "recovery-needed";

    /// <summary>
    /// Durable shared scheduler orchestration currently remains conflicted across coordination or lease ownership truth.
    /// </summary>
    public const string SchedulerConflicted = "scheduler-conflicted";

    /// <summary>
    /// Durable shared scheduler orchestration currently remains blocked by broader lease-execution truth.
    /// </summary>
    public const string LeaseBlocked = "lease-blocked";

    /// <summary>
    /// The runtime currently executes automatic retry on a single node without cross-node lease ownership.
    /// </summary>
    public const string SingleNodeRuntime = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.SingleNodeRuntime;

    /// <summary>
    /// The runtime depends on cross-node lease ownership before automatic retry should execute.
    /// </summary>
    public const string LeaseCoordinatedRuntime = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.LeaseCoordinatedRuntime;

    /// <summary>
    /// Durable shared scheduler orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.OperatorOnly;

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.OwnerMatch;

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.OwnerMismatch;

    /// <summary>
    /// The runtime still exposes one active reporter identifier.
    /// </summary>
    public const string ActiveReporterVisible = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.ActiveReporterVisible;

    /// <summary>
    /// The runtime still exposes one active reporter lease.
    /// </summary>
    public const string ActiveLeaseVisible = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.ActiveLeaseVisible;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently looks healthy for shared scheduler decisions.
    /// </summary>
    public const string DurableJournalHealthy = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.DurableJournalHealthy;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.RecoveredHistory;

    /// <summary>
    /// Automatic retry currently depends on in-memory command history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.InMemoryJournalOnly;

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
    /// The current node can keep one bounded automatic retry scheduled.
    /// </summary>
    public const string CurrentNodeSchedulable = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.CurrentNodeSchedulable;

    /// <summary>
    /// The current node cannot yet keep the runtime scheduled.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionCategories.CurrentNodeBlocked;
}
