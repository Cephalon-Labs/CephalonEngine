namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned control-plane ownership answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned control-plane ownership lane.
    /// </summary>
    public const string ProviderOwnedControlPlaneOwnership = "provider-owned-control-plane-ownership";

    /// <summary>
    /// Provider-owned control-plane ownership still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OperatorOnly;

    /// <summary>
    /// Provider-owned control-plane ownership is currently ready on the shared lane.
    /// </summary>
    public const string OwnershipReady = "ownership-ready";

    /// <summary>
    /// Provider-owned control-plane ownership remains blocked.
    /// </summary>
    public const string OwnershipBlocked = "ownership-blocked";

    /// <summary>
    /// Provider-owned control-plane ownership is currently active on one bounded provider-facing step.
    /// </summary>
    public const string OwnershipActive = "ownership-active";

    /// <summary>
    /// Provider-owned control-plane ownership is currently partial on the shared lane.
    /// </summary>
    public const string OwnershipPartial = "ownership-partial";

    /// <summary>
    /// Provider-owned control-plane ownership currently remains risky.
    /// </summary>
    public const string OwnershipRisk = "ownership-risk";

    /// <summary>
    /// The current node can exercise bounded provider-owned control-plane ownership safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.CurrentNodeOrchestratable;

    /// <summary>
    /// The current node cannot yet exercise bounded provider-owned control-plane ownership safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.CurrentNodeBlocked;

    /// <summary>
    /// Broader provider execution orchestration is currently ready.
    /// </summary>
    public const string ProviderExecutionReady = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationReady;

    /// <summary>
    /// Broader provider execution orchestration remains blocked.
    /// </summary>
    public const string ProviderExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationBlocked;

    /// <summary>
    /// Broader provider execution orchestration is currently executing.
    /// </summary>
    public const string ProviderExecutionExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationExecuting;

    /// <summary>
    /// Broader provider execution orchestration no longer needs another shared step.
    /// </summary>
    public const string ProviderExecutionCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationCompleted;

    /// <summary>
    /// Broader provider execution orchestration currently remains risky.
    /// </summary>
    public const string ProviderExecutionRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.OrchestrationRisk;

    /// <summary>
    /// Provider-owned write-path execution is currently ready.
    /// </summary>
    public const string ProviderExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderExecutable;

    /// <summary>
    /// Provider-owned write-path execution remains blocked.
    /// </summary>
    public const string ProviderBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderBlocked;

    /// <summary>
    /// Provider-owned write-path execution is currently active.
    /// </summary>
    public const string ProviderOwnedExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderOwnedExecuting;

    /// <summary>
    /// Provider-owned write-path execution no longer needs another provider command.
    /// </summary>
    public const string ProviderOwnedCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderOwnedCompleted;

    /// <summary>
    /// Provider-owned write-path execution currently remains risky.
    /// </summary>
    public const string ProviderOwnedRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderOwnedRisk;

    /// <summary>
    /// The durable shared scheduler currently keeps one bounded retry scheduled.
    /// </summary>
    public const string SchedulerScheduled = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.SchedulerScheduled;

    /// <summary>
    /// The durable shared scheduler currently does not need another scheduled step.
    /// </summary>
    public const string SchedulerUnscheduled = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.SchedulerUnscheduled;

    /// <summary>
    /// The durable shared scheduler currently still needs durable recovery hardening.
    /// </summary>
    public const string RecoveryNeeded = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.RecoveryNeeded;

    /// <summary>
    /// The durable shared scheduler currently remains conflicted.
    /// </summary>
    public const string SchedulerConflicted = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.SchedulerConflicted;

    /// <summary>
    /// Scheduler recovery is ready for safe control-plane ownership.
    /// </summary>
    public const string RecoveryReady = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.RecoveryReady;

    /// <summary>
    /// Scheduler recovery remains blocked by missing durable evidence.
    /// </summary>
    public const string RecoveryBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.RecoveryBlocked;

    /// <summary>
    /// Scheduler execution truth currently looks hardened enough for control-plane ownership.
    /// </summary>
    public const string ExecutionHardened = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ExecutionHardened;

    /// <summary>
    /// Scheduler execution truth currently remains risky.
    /// </summary>
    public const string ExecutionRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ExecutionRisk;

    /// <summary>
    /// The command journal currently exposes retained evidence for provider-owned control-plane ownership.
    /// </summary>
    public const string CommandJournalEvidence = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.CommandJournalEvidence;

    /// <summary>
    /// No command-journal evidence has been recorded yet.
    /// </summary>
    public const string NoRecordedCommand = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.NoRecordedCommand;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.RecoveredHistory;

    /// <summary>
    /// Provider-owned control-plane ownership currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.InMemoryJournalOnly;

    /// <summary>
    /// The current provider control-plane lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ApprovalRequired;

    /// <summary>
    /// The current provider control-plane lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.DestructiveOperation;

    /// <summary>
    /// The latest provider execution translated into a provider-facing command shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider execution determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider execution remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider execution remained operator-owned.
    /// </summary>
    public const string ProviderCommandOperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandOperatorOnly;

    /// <summary>
    /// The latest provider execution could not resolve a provider adapter.
    /// </summary>
    public const string ProviderCommandUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandUnavailable;

    /// <summary>
    /// The latest provider execution failed while Cephalon translated provider command shape.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationCategories.ProviderCommandFailed;
}
