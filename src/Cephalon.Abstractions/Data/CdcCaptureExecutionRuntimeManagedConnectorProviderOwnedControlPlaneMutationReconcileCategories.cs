namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned control-plane mutation and reconcile answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned control-plane mutation and reconcile lane.
    /// </summary>
    public const string ProviderOwnedControlPlaneMutationReconcile = "provider-owned-control-plane-mutation-reconcile";

    /// <summary>
    /// Provider-owned control-plane mutation and reconcile still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OperatorOnly;

    /// <summary>
    /// Provider-owned control-plane mutation is currently ready.
    /// </summary>
    public const string MutationReady = "mutation-ready";

    /// <summary>
    /// Provider-owned control-plane reconcile is currently ready.
    /// </summary>
    public const string ReconcileReady = "reconcile-ready";

    /// <summary>
    /// Provider-owned control-plane mutation remains blocked.
    /// </summary>
    public const string MutationBlocked = "mutation-blocked";

    /// <summary>
    /// Provider-owned control-plane reconcile remains blocked.
    /// </summary>
    public const string ReconcileBlocked = "reconcile-blocked";

    /// <summary>
    /// Provider-owned control-plane mutation or reconcile is currently executing.
    /// </summary>
    public const string MutationExecuting = "mutation-executing";

    /// <summary>
    /// Provider-owned control-plane mutation or reconcile currently remains risky.
    /// </summary>
    public const string MutationRisk = "mutation-risk";

    /// <summary>
    /// The current target operation is one provider-owned mutation.
    /// </summary>
    public const string MutationOperation = "mutation-operation";

    /// <summary>
    /// The current target operation is one provider-owned reconcile.
    /// </summary>
    public const string ReconcileOperation = "reconcile-operation";

    /// <summary>
    /// No provider-owned mutation or reconcile operation is currently targeted.
    /// </summary>
    public const string NoTargetOperation = "no-target-operation";

    /// <summary>
    /// The current node can exercise bounded provider-owned control-plane mutation and reconcile safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet exercise bounded provider-owned control-plane mutation and reconcile safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.CurrentNodeBlocked;

    /// <summary>
    /// Broader provider-owned control-plane ownership is currently ready.
    /// </summary>
    public const string OwnershipReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipReady;

    /// <summary>
    /// Broader provider-owned control-plane ownership remains blocked.
    /// </summary>
    public const string OwnershipBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipBlocked;

    /// <summary>
    /// Broader provider-owned control-plane ownership is currently active.
    /// </summary>
    public const string OwnershipActive = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipActive;

    /// <summary>
    /// Broader provider-owned control-plane ownership currently remains partial.
    /// </summary>
    public const string OwnershipPartial = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipPartial;

    /// <summary>
    /// Broader provider-owned control-plane ownership currently remains risky.
    /// </summary>
    public const string OwnershipRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.OwnershipRisk;

    /// <summary>
    /// Broader provider execution orchestration is currently ready.
    /// </summary>
    public const string ProviderExecutionReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionReady;

    /// <summary>
    /// Broader provider execution orchestration remains blocked.
    /// </summary>
    public const string ProviderExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionBlocked;

    /// <summary>
    /// Broader provider execution orchestration is currently executing.
    /// </summary>
    public const string ProviderExecutionExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionExecuting;

    /// <summary>
    /// Broader provider execution orchestration no longer needs another shared step.
    /// </summary>
    public const string ProviderExecutionCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionCompleted;

    /// <summary>
    /// Broader provider execution orchestration currently remains risky.
    /// </summary>
    public const string ProviderExecutionRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutionRisk;

    /// <summary>
    /// Provider-owned write-path execution is currently ready.
    /// </summary>
    public const string ProviderExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderExecutable;

    /// <summary>
    /// Provider-owned write-path execution remains blocked.
    /// </summary>
    public const string ProviderBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderBlocked;

    /// <summary>
    /// Provider-owned write-path execution is currently active.
    /// </summary>
    public const string ProviderOwnedExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderOwnedExecuting;

    /// <summary>
    /// Provider-owned write-path execution no longer needs another provider command.
    /// </summary>
    public const string ProviderOwnedCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderOwnedCompleted;

    /// <summary>
    /// Provider-owned write-path execution currently remains risky.
    /// </summary>
    public const string ProviderOwnedRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderOwnedRisk;

    /// <summary>
    /// The current command envelope is engine-ready for mutation or reconcile.
    /// </summary>
    public const string CommandEnvelopeReady = "command-envelope-ready";

    /// <summary>
    /// The current command envelope remains blocked.
    /// </summary>
    public const string CommandEnvelopeBlocked = "command-envelope-blocked";

    /// <summary>
    /// The current shared command issuance lane accepted one mutation or reconcile command.
    /// </summary>
    public const string CommandIssuanceAccepted = "command-issuance-accepted";

    /// <summary>
    /// The current shared command issuance lane already issued one mutation or reconcile command.
    /// </summary>
    public const string CommandIssuanceIssued = "command-issuance-issued";

    /// <summary>
    /// The current retry posture still allows one eligible mutation or reconcile retry.
    /// </summary>
    public const string RetryEligible = "retry-eligible";

    /// <summary>
    /// The current retry posture remains blocked.
    /// </summary>
    public const string RetryBlocked = "retry-blocked";

    /// <summary>
    /// The current provider control-plane lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ApprovalRequired;

    /// <summary>
    /// The current provider control-plane lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.DestructiveOperation;

    /// <summary>
    /// The current provider control-plane mutation or reconcile would still apply one or more shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = "would-apply-changes";

    /// <summary>
    /// The current provider control-plane mutation or reconcile would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = "no-changes-required";

    /// <summary>
    /// The command journal currently exposes retained evidence for provider-owned mutation and reconcile.
    /// </summary>
    public const string CommandJournalEvidence = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.CommandJournalEvidence;

    /// <summary>
    /// No command-journal evidence has been recorded yet.
    /// </summary>
    public const string NoRecordedCommand = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.NoRecordedCommand;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.RecoveredHistory;

    /// <summary>
    /// Provider-owned control-plane mutation and reconcile currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.InMemoryJournalOnly;

    /// <summary>
    /// The latest provider execution translated into a provider-facing command shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider execution determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider execution remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider execution remained operator-owned.
    /// </summary>
    public const string ProviderCommandOperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandOperatorOnly;

    /// <summary>
    /// The latest provider execution could not resolve a provider adapter.
    /// </summary>
    public const string ProviderCommandUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandUnavailable;

    /// <summary>
    /// The latest provider execution failed while Cephalon translated provider command shape.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipCategories.ProviderCommandFailed;
}
