namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningSources
{
    /// <summary>
    /// The broader provider-owned control-plane apply-and-reconcile execution answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedControlPlaneApplyAndReconcileExecution = "provider-owned-control-plane-apply-and-reconcile-execution";

    /// <summary>
    /// The broader provider-owned control-plane provisioning answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedControlPlaneProvisioning = "provider-owned-control-plane-provisioning";

    /// <summary>
    /// The broader provider-owned control-plane mutation and reconcile answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedControlPlaneMutationReconcile = "provider-owned-control-plane-mutation-reconcile";

    /// <summary>
    /// The broader provider-owned control-plane ownership answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedControlPlaneOwnership = "provider-owned-control-plane-ownership";

    /// <summary>
    /// The broader managed-connector governance answer supplied the decisive input.
    /// </summary>
    public const string ManagedConnectorGovernance = "managed-connector-governance";

    /// <summary>
    /// The broader managed-connector drift answer supplied the decisive input.
    /// </summary>
    public const string ManagedConnectorDrift = "managed-connector-drift";

    /// <summary>
    /// The latest recorded command-execution answer supplied the decisive input.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The shared command-journal answer supplied the decisive input.
    /// </summary>
    public const string CommandJournal = "command-journal";

    /// <summary>
    /// No decisive source answer was available.
    /// </summary>
    public const string Unknown = "unknown";
}
