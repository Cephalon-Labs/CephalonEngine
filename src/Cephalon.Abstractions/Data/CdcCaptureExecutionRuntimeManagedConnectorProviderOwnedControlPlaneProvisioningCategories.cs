namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned control-plane provisioning answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned control-plane provisioning lane.
    /// </summary>
    public const string ProviderOwnedControlPlaneProvisioning = "provider-owned-control-plane-provisioning";

    /// <summary>
    /// Provider-owned control-plane provisioning still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.OperatorOnly;

    /// <summary>
    /// Provider-owned control-plane provisioning is currently ready.
    /// </summary>
    public const string ProvisioningReady = "provisioning-ready";

    /// <summary>
    /// Provider-owned control-plane provisioning remains blocked.
    /// </summary>
    public const string ProvisioningBlocked = "provisioning-blocked";

    /// <summary>
    /// Provider-owned control-plane provisioning is currently executing.
    /// </summary>
    public const string ProvisioningExecuting = "provisioning-executing";

    /// <summary>
    /// Provider-owned control-plane provisioning is currently partial on the shared lane.
    /// </summary>
    public const string ProvisioningPartial = "provisioning-partial";

    /// <summary>
    /// Provider-owned control-plane provisioning currently remains risky.
    /// </summary>
    public const string ProvisioningRisk = "provisioning-risk";

    /// <summary>
    /// The current target operation is one provider-owned mutation.
    /// </summary>
    public const string MutationOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationOperation;

    /// <summary>
    /// The current target operation is one provider-owned reconcile.
    /// </summary>
    public const string ReconcileOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ReconcileOperation;

    /// <summary>
    /// No provider-owned mutation or reconcile operation is currently targeted.
    /// </summary>
    public const string NoTargetOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.NoTargetOperation;

    /// <summary>
    /// The current node can exercise bounded provider-owned control-plane provisioning safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet exercise bounded provider-owned control-plane provisioning safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CurrentNodeBlocked;

    /// <summary>
    /// Broader provider-owned control-plane ownership is currently ready.
    /// </summary>
    public const string OwnershipReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.OwnershipReady;

    /// <summary>
    /// Broader provider-owned control-plane ownership remains blocked.
    /// </summary>
    public const string OwnershipBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.OwnershipBlocked;

    /// <summary>
    /// Broader provider-owned control-plane ownership is currently active.
    /// </summary>
    public const string OwnershipActive = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.OwnershipActive;

    /// <summary>
    /// Broader provider-owned control-plane ownership currently remains partial.
    /// </summary>
    public const string OwnershipPartial = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.OwnershipPartial;

    /// <summary>
    /// Broader provider-owned control-plane ownership currently remains risky.
    /// </summary>
    public const string OwnershipRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.OwnershipRisk;

    /// <summary>
    /// Broader provider-owned control-plane mutation is currently ready.
    /// </summary>
    public const string MutationReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationReady;

    /// <summary>
    /// Broader provider-owned control-plane reconcile is currently ready.
    /// </summary>
    public const string ReconcileReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ReconcileReady;

    /// <summary>
    /// Broader provider-owned control-plane mutation remains blocked.
    /// </summary>
    public const string MutationBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationBlocked;

    /// <summary>
    /// Broader provider-owned control-plane reconcile remains blocked.
    /// </summary>
    public const string ReconcileBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ReconcileBlocked;

    /// <summary>
    /// Broader provider-owned control-plane mutation or reconcile is currently executing.
    /// </summary>
    public const string MutationExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationExecuting;

    /// <summary>
    /// Broader provider-owned control-plane mutation or reconcile currently remains risky.
    /// </summary>
    public const string MutationRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationRisk;

    /// <summary>
    /// Broader provider execution orchestration is currently ready.
    /// </summary>
    public const string ProviderExecutionReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderExecutionReady;

    /// <summary>
    /// Broader provider execution orchestration remains blocked.
    /// </summary>
    public const string ProviderExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderExecutionBlocked;

    /// <summary>
    /// Broader provider execution orchestration is currently executing.
    /// </summary>
    public const string ProviderExecutionExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderExecutionExecuting;

    /// <summary>
    /// Broader provider execution orchestration no longer needs another shared step.
    /// </summary>
    public const string ProviderExecutionCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderExecutionCompleted;

    /// <summary>
    /// Broader provider execution orchestration currently remains risky.
    /// </summary>
    public const string ProviderExecutionRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderExecutionRisk;

    /// <summary>
    /// Provider-owned write-path execution is currently ready.
    /// </summary>
    public const string ProviderExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderExecutable;

    /// <summary>
    /// Provider-owned write-path execution remains blocked.
    /// </summary>
    public const string ProviderBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderBlocked;

    /// <summary>
    /// Provider-owned write-path execution is currently active.
    /// </summary>
    public const string ProviderOwnedExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderOwnedExecuting;

    /// <summary>
    /// Provider-owned write-path execution no longer needs another provider command.
    /// </summary>
    public const string ProviderOwnedCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderOwnedCompleted;

    /// <summary>
    /// Provider-owned write-path execution currently remains risky.
    /// </summary>
    public const string ProviderOwnedRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderOwnedRisk;

    /// <summary>
    /// The current command envelope is engine-ready for provider-owned provisioning.
    /// </summary>
    public const string CommandEnvelopeReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CommandEnvelopeReady;

    /// <summary>
    /// The current command envelope remains blocked.
    /// </summary>
    public const string CommandEnvelopeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CommandEnvelopeBlocked;

    /// <summary>
    /// The current shared command issuance lane accepted one provider-owned provisioning command.
    /// </summary>
    public const string CommandIssuanceAccepted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CommandIssuanceAccepted;

    /// <summary>
    /// The current shared command issuance lane already issued one provider-owned provisioning command.
    /// </summary>
    public const string CommandIssuanceIssued = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CommandIssuanceIssued;

    /// <summary>
    /// The current retry posture still allows one eligible provider-owned provisioning retry.
    /// </summary>
    public const string RetryEligible = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.RetryEligible;

    /// <summary>
    /// The current retry posture remains blocked.
    /// </summary>
    public const string RetryBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.RetryBlocked;

    /// <summary>
    /// The current provider control-plane provisioning lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ApprovalRequired;

    /// <summary>
    /// The current provider control-plane provisioning lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.DestructiveOperation;

    /// <summary>
    /// The current provider control-plane provisioning lane would still apply one or more shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.WouldApplyChanges;

    /// <summary>
    /// The current provider control-plane provisioning lane would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.NoChangesRequired;

    /// <summary>
    /// The command journal currently exposes retained evidence for provider-owned provisioning.
    /// </summary>
    public const string CommandJournalEvidence = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.CommandJournalEvidence;

    /// <summary>
    /// No command-journal evidence has been recorded yet.
    /// </summary>
    public const string NoRecordedCommand = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.NoRecordedCommand;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.RecoveredHistory;

    /// <summary>
    /// Provider-owned control-plane provisioning currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.InMemoryJournalOnly;

    /// <summary>
    /// The latest provider execution translated into a provider-facing command shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider execution determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider execution remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider execution remained operator-owned.
    /// </summary>
    public const string ProviderCommandOperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderCommandOperatorOnly;

    /// <summary>
    /// The latest provider execution could not resolve a provider adapter.
    /// </summary>
    public const string ProviderCommandUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderCommandUnavailable;

    /// <summary>
    /// The latest provider execution failed while Cephalon translated provider command shape.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ProviderCommandFailed;
}
