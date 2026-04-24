namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned write-path execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned write-path execution lane.
    /// </summary>
    public const string ProviderOwnedWritePathExecution = "provider-owned-write-path-execution";

    /// <summary>
    /// Provider-owned write-path execution still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned write-path execution is currently ready on the shared lane.
    /// </summary>
    public const string ProviderExecutable = "provider-executable";

    /// <summary>
    /// Provider-owned write-path execution remains blocked.
    /// </summary>
    public const string ProviderBlocked = "provider-blocked";

    /// <summary>
    /// Provider-owned write-path execution already translated one provider-facing command shape.
    /// </summary>
    public const string ProviderOwnedExecuting = "provider-owned-executing";

    /// <summary>
    /// Provider-owned write-path execution no longer needs an additional provider command.
    /// </summary>
    public const string ProviderOwnedCompleted = "provider-owned-completed";

    /// <summary>
    /// Provider-owned write-path execution currently remains risky.
    /// </summary>
    public const string ProviderOwnedRisk = "provider-owned-risk";

    /// <summary>
    /// The current execution adapter is ready to translate provider-facing commands.
    /// </summary>
    public const string AdapterReady = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterReady;

    /// <summary>
    /// No matching provider execution adapter is currently available.
    /// </summary>
    public const string AdapterUnavailable = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.AdapterUnavailable;

    /// <summary>
    /// The current write-path still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalRequired;

    /// <summary>
    /// The current write-path still targets a destructive provider operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.DestructiveOperation;

    /// <summary>
    /// The current node can execute provider-owned write-path work safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet execute provider-owned write-path work safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningCategories.CurrentNodeBlocked;

    /// <summary>
    /// The latest provider-owned command translated into a provider-facing command shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionAdapted;

    /// <summary>
    /// The latest provider-owned command determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionNoOp;

    /// <summary>
    /// The latest provider-owned command remained blocked by shared runtime truth.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionBlocked;

    /// <summary>
    /// The latest provider-owned command remained operator-owned.
    /// </summary>
    public const string ProviderCommandOperatorOnly = "provider-command-operator-only";

    /// <summary>
    /// The latest provider-owned command could not resolve a provider execution adapter.
    /// </summary>
    public const string ProviderCommandUnavailable = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionUnavailable;

    /// <summary>
    /// The latest provider-owned command failed while Cephalon translated it.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.LatestExecutionFailed;

    /// <summary>
    /// The shared retry policy currently exposes a retry-ready provider lane.
    /// </summary>
    public const string RetryReady = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories.RetryReady;

    /// <summary>
    /// The shared retry policy currently remains inside one cooldown window.
    /// </summary>
    public const string CooldownWindow = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationCategories.CooldownWindow;
}
