namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider-owned control-plane apply-and-reconcile execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionSources
{
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
    /// The shared command-envelope answer supplied the decisive input.
    /// </summary>
    public const string CommandEnvelope = "command-envelope";

    /// <summary>
    /// The shared command-issuance answer supplied the decisive input.
    /// </summary>
    public const string CommandIssuance = "command-issuance";

    /// <summary>
    /// The shared command-retry answer supplied the decisive input.
    /// </summary>
    public const string CommandRetry = "command-retry";

    /// <summary>
    /// The shared retry-execution policy answer supplied the decisive input.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

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
