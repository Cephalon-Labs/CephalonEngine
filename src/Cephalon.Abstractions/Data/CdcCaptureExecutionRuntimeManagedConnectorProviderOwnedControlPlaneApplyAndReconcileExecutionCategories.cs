namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned control-plane apply-and-reconcile execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned control-plane apply-and-reconcile execution lane.
    /// </summary>
    public const string ProviderOwnedControlPlaneApplyAndReconcileExecution = "provider-owned-control-plane-apply-and-reconcile-execution";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.OperatorOnly;

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution is currently ready.
    /// </summary>
    public const string ApplyAndReconcileReady = "apply-and-reconcile-ready";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution remains blocked.
    /// </summary>
    public const string ApplyAndReconcileBlocked = "apply-and-reconcile-blocked";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution is currently exercising one bounded execution step.
    /// </summary>
    public const string ApplyAndReconcileExecuting = "apply-and-reconcile-executing";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution no longer needs another shared execution step.
    /// </summary>
    public const string ApplyAndReconcileCompleted = "apply-and-reconcile-completed";

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution currently remains risky.
    /// </summary>
    public const string ApplyAndReconcileRisk = "apply-and-reconcile-risk";

    /// <summary>
    /// The current target operation is one provider-owned mutation.
    /// </summary>
    public const string MutationOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationOperation;

    /// <summary>
    /// The current target operation is one provider-owned reconcile.
    /// </summary>
    public const string ReconcileOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ReconcileOperation;

    /// <summary>
    /// No provider-owned mutation or reconcile operation is currently targeted.
    /// </summary>
    public const string NoTargetOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.NoTargetOperation;

    /// <summary>
    /// The current node can exercise bounded provider-owned control-plane apply-and-reconcile execution safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet exercise bounded provider-owned control-plane apply-and-reconcile execution safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CurrentNodeBlocked;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently ready.
    /// </summary>
    public const string ProvisioningReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningReady;

    /// <summary>
    /// Broader provider-owned control-plane provisioning remains blocked.
    /// </summary>
    public const string ProvisioningBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningBlocked;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently executing.
    /// </summary>
    public const string ProvisioningExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningExecuting;

    /// <summary>
    /// Broader provider-owned control-plane provisioning currently remains partial.
    /// </summary>
    public const string ProvisioningPartial = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningPartial;

    /// <summary>
    /// Broader provider-owned control-plane provisioning currently remains risky.
    /// </summary>
    public const string ProvisioningRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningRisk;

    /// <summary>
    /// Broader provider-owned control-plane ownership is currently ready.
    /// </summary>
    public const string OwnershipReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.OwnershipReady;

    /// <summary>
    /// Broader provider-owned control-plane ownership remains blocked.
    /// </summary>
    public const string OwnershipBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.OwnershipBlocked;

    /// <summary>
    /// Broader provider-owned control-plane ownership is currently active.
    /// </summary>
    public const string OwnershipActive = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.OwnershipActive;

    /// <summary>
    /// Broader provider-owned control-plane ownership currently remains partial.
    /// </summary>
    public const string OwnershipPartial = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.OwnershipPartial;

    /// <summary>
    /// Broader provider-owned control-plane ownership currently remains risky.
    /// </summary>
    public const string OwnershipRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.OwnershipRisk;

    /// <summary>
    /// Broader provider-owned control-plane mutation is currently ready.
    /// </summary>
    public const string MutationReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationReady;

    /// <summary>
    /// Broader provider-owned control-plane reconcile is currently ready.
    /// </summary>
    public const string ReconcileReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ReconcileReady;

    /// <summary>
    /// Broader provider-owned control-plane mutation remains blocked.
    /// </summary>
    public const string MutationBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationBlocked;

    /// <summary>
    /// Broader provider-owned control-plane reconcile remains blocked.
    /// </summary>
    public const string ReconcileBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ReconcileBlocked;

    /// <summary>
    /// Broader provider-owned control-plane mutation or reconcile is currently executing.
    /// </summary>
    public const string MutationExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationExecuting;

    /// <summary>
    /// Broader provider-owned control-plane mutation or reconcile currently remains risky.
    /// </summary>
    public const string MutationRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.MutationRisk;

    /// <summary>
    /// Broader provider execution orchestration is currently ready.
    /// </summary>
    public const string ProviderExecutionReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderExecutionReady;

    /// <summary>
    /// Broader provider execution orchestration remains blocked.
    /// </summary>
    public const string ProviderExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderExecutionBlocked;

    /// <summary>
    /// Broader provider execution orchestration is currently executing.
    /// </summary>
    public const string ProviderExecutionExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderExecutionExecuting;

    /// <summary>
    /// Broader provider execution orchestration no longer needs another shared step.
    /// </summary>
    public const string ProviderExecutionCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderExecutionCompleted;

    /// <summary>
    /// Broader provider execution orchestration currently remains risky.
    /// </summary>
    public const string ProviderExecutionRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderExecutionRisk;

    /// <summary>
    /// Provider-owned write-path execution is currently ready.
    /// </summary>
    public const string ProviderExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderExecutable;

    /// <summary>
    /// Provider-owned write-path execution remains blocked.
    /// </summary>
    public const string ProviderBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderBlocked;

    /// <summary>
    /// Provider-owned write-path execution is currently active.
    /// </summary>
    public const string ProviderOwnedExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderOwnedExecuting;

    /// <summary>
    /// Provider-owned write-path execution no longer needs another provider command.
    /// </summary>
    public const string ProviderOwnedCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderOwnedCompleted;

    /// <summary>
    /// Provider-owned write-path execution currently remains risky.
    /// </summary>
    public const string ProviderOwnedRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderOwnedRisk;

    /// <summary>
    /// The current command envelope is engine-ready for provider-owned apply-and-reconcile execution.
    /// </summary>
    public const string CommandEnvelopeReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CommandEnvelopeReady;

    /// <summary>
    /// The current command envelope remains blocked.
    /// </summary>
    public const string CommandEnvelopeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CommandEnvelopeBlocked;

    /// <summary>
    /// The current shared command issuance lane accepted one provider-owned apply-and-reconcile command.
    /// </summary>
    public const string CommandIssuanceAccepted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CommandIssuanceAccepted;

    /// <summary>
    /// The current shared command issuance lane already issued one provider-owned apply-and-reconcile command.
    /// </summary>
    public const string CommandIssuanceIssued = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CommandIssuanceIssued;

    /// <summary>
    /// The current retry posture still allows one eligible provider-owned apply-and-reconcile retry.
    /// </summary>
    public const string RetryEligible = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.RetryEligible;

    /// <summary>
    /// The current retry posture remains blocked.
    /// </summary>
    public const string RetryBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.RetryBlocked;

    /// <summary>
    /// The current provider control-plane apply-and-reconcile lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ApprovalRequired;

    /// <summary>
    /// The current provider control-plane apply-and-reconcile lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.DestructiveOperation;

    /// <summary>
    /// The current provider control-plane apply-and-reconcile lane would still apply one or more shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.WouldApplyChanges;

    /// <summary>
    /// The current provider control-plane apply-and-reconcile lane would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.NoChangesRequired;

    /// <summary>
    /// The command journal currently exposes retained evidence for provider-owned apply-and-reconcile execution.
    /// </summary>
    public const string CommandJournalEvidence = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.CommandJournalEvidence;

    /// <summary>
    /// No command-journal evidence has been recorded yet.
    /// </summary>
    public const string NoRecordedCommand = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.NoRecordedCommand;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.RecoveredHistory;

    /// <summary>
    /// Provider-owned control-plane apply-and-reconcile execution currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.InMemoryJournalOnly;

    /// <summary>
    /// The latest provider execution translated into a provider-facing command shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider execution determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider execution remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider execution remained operator-owned.
    /// </summary>
    public const string ProviderCommandOperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandOperatorOnly;

    /// <summary>
    /// The latest provider execution could not resolve a provider adapter.
    /// </summary>
    public const string ProviderCommandUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandUnavailable;

    /// <summary>
    /// The latest provider execution failed while Cephalon translated provider command shape.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProviderCommandFailed;
}
