namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider-owned write-path execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionSources
{
    /// <summary>
    /// The provider-owned write-path execution answer was derived primarily from execution-adapter truth.
    /// </summary>
    public const string ExecutionAdapter = "execution-adapter";

    /// <summary>
    /// The provider-owned write-path execution answer was derived primarily from the latest command-execution outcome.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The provider-owned write-path execution answer was derived primarily from retry-execution policy truth.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

    /// <summary>
    /// The provider-owned write-path execution answer was derived primarily from automatic background retry execution truth.
    /// </summary>
    public const string AutomaticRetryExecution = "automatic-retry-execution";

    /// <summary>
    /// The provider-owned write-path execution answer was derived primarily from scheduler recovery and execution-hardening truth.
    /// </summary>
    public const string SchedulerRecoveryExecutionHardening = "scheduler-recovery-execution-hardening";

    /// <summary>
    /// The provider-owned write-path execution answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
