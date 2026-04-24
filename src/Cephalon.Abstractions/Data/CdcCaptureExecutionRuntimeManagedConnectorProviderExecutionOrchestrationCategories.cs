namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider execution-orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories
{
    /// <summary>
    /// The runtime participates in the provider execution-orchestration lane.
    /// </summary>
    public const string ProviderExecutionOrchestration = "provider-execution-orchestration";

    /// <summary>
    /// Provider execution orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.OperatorOnly;

    /// <summary>
    /// Provider execution orchestration is currently ready on the shared lane.
    /// </summary>
    public const string OrchestrationReady = "orchestration-ready";

    /// <summary>
    /// Provider execution orchestration remains blocked.
    /// </summary>
    public const string OrchestrationBlocked = "orchestration-blocked";

    /// <summary>
    /// Provider execution orchestration is currently executing one provider-facing orchestration step.
    /// </summary>
    public const string OrchestrationExecuting = "orchestration-executing";

    /// <summary>
    /// Provider execution orchestration no longer needs another provider-facing orchestration step.
    /// </summary>
    public const string OrchestrationCompleted = "orchestration-completed";

    /// <summary>
    /// Provider execution orchestration currently remains risky.
    /// </summary>
    public const string OrchestrationRisk = "orchestration-risk";

    /// <summary>
    /// The current node can orchestrate provider execution safely.
    /// </summary>
    public const string CurrentNodeOrchestratable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet orchestrate provider execution safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.CurrentNodeBlocked;

    /// <summary>
    /// Provider-owned write-path execution is currently ready.
    /// </summary>
    public const string ProviderExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderExecutable;

    /// <summary>
    /// Provider-owned write-path execution remains blocked.
    /// </summary>
    public const string ProviderBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderBlocked;

    /// <summary>
    /// Provider-owned write-path execution already translated one provider-facing command shape.
    /// </summary>
    public const string ProviderOwnedExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderOwnedExecuting;

    /// <summary>
    /// Provider-owned write-path execution no longer needs another provider command.
    /// </summary>
    public const string ProviderOwnedCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderOwnedCompleted;

    /// <summary>
    /// Provider-owned write-path execution currently remains risky.
    /// </summary>
    public const string ProviderOwnedRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderOwnedRisk;

    /// <summary>
    /// The durable shared scheduler currently keeps one bounded retry scheduled.
    /// </summary>
    public const string SchedulerScheduled = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.Scheduled;

    /// <summary>
    /// The durable shared scheduler currently does not need another scheduled step.
    /// </summary>
    public const string SchedulerUnscheduled = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.Unscheduled;

    /// <summary>
    /// The durable shared scheduler currently still needs durable recovery hardening.
    /// </summary>
    public const string RecoveryNeeded = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.RecoveryNeeded;

    /// <summary>
    /// The durable shared scheduler currently remains conflicted.
    /// </summary>
    public const string SchedulerConflicted = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.SchedulerConflicted;

    /// <summary>
    /// Scheduler recovery is ready for safe orchestration.
    /// </summary>
    public const string RecoveryReady = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.RecoveryReady;

    /// <summary>
    /// Scheduler recovery remains blocked by missing durable evidence.
    /// </summary>
    public const string RecoveryBlocked = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.RecoveryBlocked;

    /// <summary>
    /// Scheduler execution truth currently looks hardened enough for orchestration.
    /// </summary>
    public const string ExecutionHardened = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.ExecutionHardened;

    /// <summary>
    /// Scheduler execution truth currently remains risky.
    /// </summary>
    public const string ExecutionRisk = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.ExecutionRisk;

    /// <summary>
    /// The command journal currently exposes retained evidence for provider execution orchestration.
    /// </summary>
    public const string CommandJournalEvidence = "command-journal-evidence";

    /// <summary>
    /// No command-journal evidence has been recorded yet.
    /// </summary>
    public const string NoRecordedCommand = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.NoRecordedCommand;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.RecoveredHistory;

    /// <summary>
    /// Provider execution orchestration currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.InMemoryJournalOnly;

    /// <summary>
    /// The current provider execution lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ApprovalRequired;

    /// <summary>
    /// The current provider execution lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.DestructiveOperation;

    /// <summary>
    /// The latest provider execution translated into a provider-facing command shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider execution determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider execution remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider execution remained operator-owned.
    /// </summary>
    public const string ProviderCommandOperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandOperatorOnly;

    /// <summary>
    /// The latest provider execution could not resolve a provider adapter.
    /// </summary>
    public const string ProviderCommandUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandUnavailable;

    /// <summary>
    /// The latest provider execution failed while Cephalon translated provider command shape.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories.ProviderCommandFailed;
}
