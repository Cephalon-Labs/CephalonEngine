namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector automatic background retry coordination answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources
{
    /// <summary>
    /// The automatic background retry coordination answer was derived primarily from automatic retry posture.
    /// </summary>
    public const string AutomaticRetryExecution = "automatic-retry-execution";

    /// <summary>
    /// The automatic background retry coordination answer was derived primarily from reporter-coordination truth.
    /// </summary>
    public const string ReporterCoordination = "reporter-coordination";

    /// <summary>
    /// The automatic background retry coordination answer was derived primarily from the host-owned coordination owner id.
    /// </summary>
    public const string CoordinationOwner = "coordination-owner";

    /// <summary>
    /// The automatic background retry coordination answer was derived primarily from execution-ownership semantics.
    /// </summary>
    public const string ExecutionOwnership = "execution-ownership";

    /// <summary>
    /// The automatic background retry coordination answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
