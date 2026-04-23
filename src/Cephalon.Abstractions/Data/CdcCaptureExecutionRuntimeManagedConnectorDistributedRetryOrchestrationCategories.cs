namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector distributed retry orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationCategories
{
    /// <summary>
    /// The runtime participates in the shared bounded distributed retry scheduler.
    /// </summary>
    public const string BoundedSharedScheduler = "bounded-shared-scheduler";

    /// <summary>
    /// The shared bounded distributed retry scheduler is enabled for the current runtime.
    /// </summary>
    public const string SchedulerEnabled = "scheduler-enabled";

    /// <summary>
    /// The shared bounded distributed retry scheduler is disabled for the current runtime.
    /// </summary>
    public const string SchedulerDisabled = "scheduler-disabled";

    /// <summary>
    /// The current runtime can schedule automatic retry on the current node.
    /// </summary>
    public const string CurrentNodeSchedulable = "current-node-schedulable";

    /// <summary>
    /// The current retry policy is still waiting for a cooldown window to elapse.
    /// </summary>
    public const string CooldownWindow = "cooldown-window";

    /// <summary>
    /// No further automatic retry scheduling is currently needed for the runtime.
    /// </summary>
    public const string NoFurtherRetryNeeded = "no-further-retry-needed";

    /// <summary>
    /// The bounded command history already records one automatic retry attempt.
    /// </summary>
    public const string AutomaticRetryAttemptRecorded = "automatic-retry-attempt-recorded";

    /// <summary>
    /// The runtime can evaluate automatic retry on a single node without lease coordination.
    /// </summary>
    public const string SingleNodeRuntime = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.SingleNodeRuntime;

    /// <summary>
    /// The runtime depends on cross-node lease coordination before automatic retry should execute.
    /// </summary>
    public const string LeaseCoordinatedRuntime = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.LeaseCoordinatedRuntime;

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.OwnerMatch;

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.OwnerMismatch;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently looks healthy for cross-node retry decisions.
    /// </summary>
    public const string DurableJournalHealthy = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.DurableJournalHealthy;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.RecoveredHistory;

    /// <summary>
    /// Automatic retry currently depends on in-memory command history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.InMemoryJournalOnly;

    /// <summary>
    /// Cross-node idempotency currently looks safe for automatic retry.
    /// </summary>
    public const string CrossNodeIdempotentSafe = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.CrossNodeIdempotentSafe;

    /// <summary>
    /// Cross-node idempotency currently remains risky for automatic retry.
    /// </summary>
    public const string CrossNodeIdempotencyRisk = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.CrossNodeIdempotencyRisk;

    /// <summary>
    /// Distributed retry orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories.OperatorOnly;
}
