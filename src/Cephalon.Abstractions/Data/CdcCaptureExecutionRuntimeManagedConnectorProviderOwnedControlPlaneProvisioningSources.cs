namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider-owned control-plane provisioning answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningSources
{
    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from provider-owned control-plane mutation and reconcile truth.
    /// </summary>
    public const string ProviderOwnedControlPlaneMutationReconcile = "provider-owned-control-plane-mutation-reconcile";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from provider-owned control-plane ownership truth.
    /// </summary>
    public const string ProviderOwnedControlPlaneOwnership = "provider-owned-control-plane-ownership";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from provider execution-orchestration truth.
    /// </summary>
    public const string ProviderExecutionOrchestration = "provider-execution-orchestration";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from provider-owned write-path execution truth.
    /// </summary>
    public const string ProviderOwnedWritePathExecution = "provider-owned-write-path-execution";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from command-envelope truth.
    /// </summary>
    public const string CommandEnvelope = "command-envelope";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from command-issuance truth.
    /// </summary>
    public const string CommandIssuance = "command-issuance";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from command-retry truth.
    /// </summary>
    public const string CommandRetry = "command-retry";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from retry-execution policy truth.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from the latest command-execution outcome.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The provider-owned control-plane provisioning answer was derived primarily from retained command-journal evidence.
    /// </summary>
    public const string CommandJournal = "command-journal";

    /// <summary>
    /// The provider-owned control-plane provisioning answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
