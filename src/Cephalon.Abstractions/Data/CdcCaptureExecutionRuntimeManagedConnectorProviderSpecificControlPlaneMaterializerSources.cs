namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider-specific control-plane materializer answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerSources
{
    /// <summary>
    /// The broader provider-owned control-plane dependency-aware provisioning and mutation hardening answer supplied the decisive input.
    /// </summary>
    public const string ProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening = "provider-owned-control-plane-dependency-aware-provisioning-and-mutation-hardening";

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
    /// The shared provider execution-adapter answer supplied the decisive input.
    /// </summary>
    public const string ExecutionAdapter = "execution-adapter";

    /// <summary>
    /// The latest command-execution outcome supplied the decisive input.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The retained command-journal evidence supplied the decisive input.
    /// </summary>
    public const string CommandJournal = "command-journal";

    /// <summary>
    /// Runtime metadata supplied the decisive input.
    /// </summary>
    public const string Metadata = "metadata";

    /// <summary>
    /// The provider-specific control-plane materializer answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
