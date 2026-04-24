namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector scheduler recovery and execution-hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories
{
    /// <summary>
    /// The runtime participates in the scheduler recovery and execution-hardening lane.
    /// </summary>
    public const string SchedulerRecoveryExecutionHardening = "scheduler-recovery-execution-hardening";

    /// <summary>
    /// Scheduler recovery and execution hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.OperatorOnly;

    /// <summary>
    /// Scheduler recovery completed enough that bounded execution can resume safely.
    /// </summary>
    public const string RecoveryReady = "recovery-ready";

    /// <summary>
    /// Scheduler recovery remains blocked by missing or unhealthy durable evidence.
    /// </summary>
    public const string RecoveryBlocked = "recovery-blocked";

    /// <summary>
    /// Scheduler recovery is replaying retained execution evidence.
    /// </summary>
    public const string Replaying = "replaying";

    /// <summary>
    /// Scheduler execution truth currently looks hardened enough for bounded execution.
    /// </summary>
    public const string ExecutionHardened = "execution-hardened";

    /// <summary>
    /// Scheduler execution truth currently remains risky.
    /// </summary>
    public const string ExecutionRisk = "execution-risk";

    /// <summary>
    /// The runtime currently executes automatic retry on a single node without cross-node lease ownership.
    /// </summary>
    public const string SingleNodeRuntime = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.SingleNodeRuntime;

    /// <summary>
    /// The runtime depends on cross-node lease ownership before automatic retry should execute.
    /// </summary>
    public const string LeaseCoordinatedRuntime = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.LeaseCoordinatedRuntime;

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.OwnerMatch;

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.OwnerMismatch;

    /// <summary>
    /// The runtime still exposes one active reporter identifier.
    /// </summary>
    public const string ActiveReporterVisible = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.ActiveReporterVisible;

    /// <summary>
    /// The runtime still exposes one active reporter lease.
    /// </summary>
    public const string ActiveLeaseVisible = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.ActiveLeaseVisible;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.RecoveredHistory;

    /// <summary>
    /// Automatic retry currently depends on in-memory command history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.InMemoryJournalOnly;

    /// <summary>
    /// The durable command journal currently reports a recovery error.
    /// </summary>
    public const string RecoveryFailed = "recovery-failed";

    /// <summary>
    /// The durable command journal currently reports a persistence error.
    /// </summary>
    public const string PersistenceFailed = "persistence-failed";

    /// <summary>
    /// The durable shared scheduler currently keeps one bounded retry scheduled.
    /// </summary>
    public const string SchedulerScheduled = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.Scheduled;

    /// <summary>
    /// The durable shared scheduler currently does not need to keep the runtime scheduled.
    /// </summary>
    public const string SchedulerUnscheduled = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.Unscheduled;

    /// <summary>
    /// The durable shared scheduler currently remains conflicted.
    /// </summary>
    public const string SchedulerConflicted = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.SchedulerConflicted;

    /// <summary>
    /// The durable shared scheduler currently remains blocked by broader lease truth.
    /// </summary>
    public const string SchedulerLeaseBlocked = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.LeaseBlocked;

    /// <summary>
    /// The current retry policy is still waiting for a cooldown window to elapse.
    /// </summary>
    public const string CooldownWindow = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.CooldownWindow;

    /// <summary>
    /// The current shared runtime truth does not currently need another automatic retry attempt.
    /// </summary>
    public const string NoFurtherRetryNeeded = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.NoFurtherRetryNeeded;

    /// <summary>
    /// The bounded command history already records one automatic retry attempt.
    /// </summary>
    public const string AutomaticRetryAttemptRecorded = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories.AutomaticRetryAttemptRecorded;

    /// <summary>
    /// Retained history currently contains one automatic retry attempt for the current retry fingerprint.
    /// </summary>
    public const string MatchingAutomaticRetryAttempt = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.MatchingAutomaticRetryAttempt;

    /// <summary>
    /// The latest automatic retry execution already translated into one provider-facing command shape.
    /// </summary>
    public const string LatestAutomaticExecutionAdapted = "latest-automatic-execution-adapted";

    /// <summary>
    /// The latest automatic retry execution failed while Cephalon translated it.
    /// </summary>
    public const string LatestAutomaticExecutionFailed = "latest-automatic-execution-failed";

    /// <summary>
    /// The latest automatic retry execution remained blocked before provider translation.
    /// </summary>
    public const string LatestAutomaticExecutionBlocked = "latest-automatic-execution-blocked";

    /// <summary>
    /// The latest automatic retry execution remained operator-owned.
    /// </summary>
    public const string LatestAutomaticExecutionOperatorOnly = "latest-automatic-execution-operator-only";

    /// <summary>
    /// The latest automatic retry execution could not resolve one provider execution adapter.
    /// </summary>
    public const string LatestAutomaticExecutionUnavailable = "latest-automatic-execution-unavailable";

    /// <summary>
    /// The current node can execute automatic retry safely for the current hardening answer.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot safely execute automatic retry yet.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.CurrentNodeBlocked;
}
