namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-specific control-plane materializer answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories
{
    /// <summary>
    /// The runtime participates in the provider-specific control-plane materializer lane.
    /// </summary>
    public const string ProviderSpecificControlPlaneMaterializer = "provider-specific-control-plane-materializer";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.OperatorOnly;

    /// <summary>
    /// No active provider-specific control-plane materializer is currently available.
    /// </summary>
    public const string MaterializerUnavailable = "materializer-unavailable";

    /// <summary>
    /// The shared runtime currently has enough truth to select a provider-specific control-plane materializer.
    /// </summary>
    public const string MaterializerReady = "materializer-ready";

    /// <summary>
    /// One provider-specific control-plane materializer is currently selected.
    /// </summary>
    public const string MaterializerSelected = "materializer-selected";

    /// <summary>
    /// The selected provider-specific control-plane materializer is currently executing or driving the provider lane.
    /// </summary>
    public const string MaterializerExecuting = "materializer-executing";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through currently remains risky.
    /// </summary>
    public const string MaterializerRisk = "materializer-risk";

    /// <summary>
    /// Broader dependency-aware provisioning and mutation hardening is currently ready.
    /// </summary>
    public const string DependencyReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.DependencyReady;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently ready.
    /// </summary>
    public const string ProvisioningReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningReady;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently executing.
    /// </summary>
    public const string ProvisioningExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningExecuting;

    /// <summary>
    /// Broader provider-owned control-plane provisioning currently remains blocked.
    /// </summary>
    public const string ProvisioningBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningBlocked;

    /// <summary>
    /// Broader provider-owned control-plane provisioning currently remains risky.
    /// </summary>
    public const string ProvisioningRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningRisk;

    /// <summary>
    /// Broader provider-owned control-plane mutation is currently ready.
    /// </summary>
    public const string MutationReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationReady;

    /// <summary>
    /// Broader provider-owned control-plane reconcile is currently ready.
    /// </summary>
    public const string ReconcileReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ReconcileReady;

    /// <summary>
    /// Broader provider-owned control-plane mutation or reconcile is currently executing.
    /// </summary>
    public const string MutationExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationExecuting;

    /// <summary>
    /// Broader provider-owned control-plane mutation currently remains blocked.
    /// </summary>
    public const string MutationBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationBlocked;

    /// <summary>
    /// Broader provider-owned control-plane mutation currently remains risky.
    /// </summary>
    public const string MutationRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationRisk;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane is fully hardened.
    /// </summary>
    public const string ApplyAndReconcileHardened = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileHardened;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane is currently executing.
    /// </summary>
    public const string ApplyAndReconcileExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileExecuting;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane currently remains risky.
    /// </summary>
    public const string ApplyAndReconcileRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileRisk;

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
    /// The current node can use the selected provider-specific control-plane materializer safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet use the selected provider-specific control-plane materializer safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeBlocked;

    /// <summary>
    /// The runtime currently exposes one provider identifier for provider-specific materialization.
    /// </summary>
    public const string ProviderIdentityReady = "provider-identity-ready";

    /// <summary>
    /// The runtime does not currently expose one provider identifier for provider-specific materialization.
    /// </summary>
    public const string MissingProviderId = "missing-provider-id";

    /// <summary>
    /// The runtime currently exposes one materializer identifier for provider-specific materialization.
    /// </summary>
    public const string MaterializerIdentityReady = "materializer-identity-ready";

    /// <summary>
    /// The runtime does not currently expose one materializer identifier for provider-specific materialization.
    /// </summary>
    public const string MissingMaterializerId = "missing-materializer-id";

    /// <summary>
    /// The runtime currently exposes one transport kind for provider-specific materialization.
    /// </summary>
    public const string TransportIdentityReady = "transport-identity-ready";

    /// <summary>
    /// The runtime does not currently expose one transport kind for provider-specific materialization.
    /// </summary>
    public const string MissingTransportKind = "missing-transport-kind";

    /// <summary>
    /// The runtime currently exposes one provider-facing surface identifier for provider-specific materialization.
    /// </summary>
    public const string ProviderSurfaceReady = "provider-surface-ready";

    /// <summary>
    /// The runtime does not currently expose one provider-facing surface identifier for provider-specific materialization.
    /// </summary>
    public const string MissingProviderSurface = "missing-provider-surface";

    /// <summary>
    /// The runtime currently exposes one connector identity for provider-specific materialization.
    /// </summary>
    public const string ConnectorIdentityReady = "connector-identity-ready";

    /// <summary>
    /// The runtime does not currently expose one connector identity for provider-specific materialization.
    /// </summary>
    public const string MissingConnectorId = "missing-connector-id";

    /// <summary>
    /// The runtime currently exposes one provider worker identity.
    /// </summary>
    public const string WorkerIdentityVisible = "worker-identity-visible";

    /// <summary>
    /// The runtime does not currently expose one provider worker identity.
    /// </summary>
    public const string WorkerIdentityUnavailable = "worker-identity-unavailable";

    /// <summary>
    /// The current provider lane would still apply one or more shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.WouldApplyChanges;

    /// <summary>
    /// The current provider lane would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.NoChangesRequired;

    /// <summary>
    /// The current provider lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApprovalRequired;

    /// <summary>
    /// The current provider lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DestructiveOperation;

    /// <summary>
    /// The latest provider command remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider command translated into a provider-facing shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider command determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider command failed while Cephalon translated it.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandFailed;

    /// <summary>
    /// The runtime currently exposes an active reporter lease.
    /// </summary>
    public const string ReporterLeaseActive = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReporterLeaseActive;

    /// <summary>
    /// The runtime is missing one active reporter lease or currently reports degraded reporter ownership.
    /// </summary>
    public const string ReporterLeaseMissingOrStale = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReporterLeaseMissingOrStale;

    /// <summary>
    /// The runtime currently declares complete dependency identity metadata.
    /// </summary>
    public const string DeclaredDependencyIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DependencyIdentityReady;

    /// <summary>
    /// The runtime currently reports complete dependency identity metadata.
    /// </summary>
    public const string ReportedDependencyIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReportedDependencyIdentityReady;
}
