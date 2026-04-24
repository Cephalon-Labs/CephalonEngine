namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider-owned control-plane dependency-aware provisioning and mutation hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningSources
{
    /// <summary>
    /// The broader provider-owned control-plane dependency-aware apply-and-reconcile hardening answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening = "provider-owned-control-plane-dependency-aware-apply-and-reconcile-hardening";

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
    /// The broader provider execution-orchestration answer supplied the decisive input.
    /// </summary>
    public const string ProviderExecutionOrchestration = "provider-execution-orchestration";

    /// <summary>
    /// The broader provider-owned write-path execution answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedWritePathExecution = "provider-owned-write-path-execution";

    /// <summary>
    /// Managed-connector governance truth supplied the decisive input.
    /// </summary>
    public const string ManagedConnectorGovernance = "managed-connector-governance";

    /// <summary>
    /// Managed-connector desired-versus-observed drift truth supplied the decisive input.
    /// </summary>
    public const string ManagedConnectorDrift = "managed-connector-drift";

    /// <summary>
    /// The retained command-journal evidence supplied the decisive input.
    /// </summary>
    public const string CommandJournal = "command-journal";

    /// <summary>
    /// The latest command-execution outcome supplied the decisive input.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The dependency-aware provisioning and mutation hardening answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
