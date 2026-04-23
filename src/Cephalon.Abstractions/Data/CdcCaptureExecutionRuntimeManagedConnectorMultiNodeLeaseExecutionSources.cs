namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector broader multi-node lease-execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionSources
{
    /// <summary>
    /// The multi-node lease-execution answer was derived primarily from automatic-retry coordination truth.
    /// </summary>
    public const string AutomaticRetryCoordination = "automatic-retry-coordination";

    /// <summary>
    /// The multi-node lease-execution answer was derived primarily from distributed retry lease truth.
    /// </summary>
    public const string DistributedRetryLease = "distributed-retry-lease";

    /// <summary>
    /// The multi-node lease-execution answer was derived primarily from cross-node idempotency-hardening truth.
    /// </summary>
    public const string CrossNodeIdempotencyHardening = "cross-node-idempotency-hardening";

    /// <summary>
    /// The multi-node lease-execution answer was derived primarily from distributed retry orchestration truth.
    /// </summary>
    public const string DistributedRetryOrchestration = "distributed-retry-orchestration";

    /// <summary>
    /// The multi-node lease-execution answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
