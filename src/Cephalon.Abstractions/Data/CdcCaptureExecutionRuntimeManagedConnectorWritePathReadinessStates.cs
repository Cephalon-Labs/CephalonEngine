namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector write-path readiness state identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates
{
    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector currently stays in observe-only mode, so write-path readiness is deferred.
    /// </summary>
    public const string Deferred = "deferred";

    /// <summary>
    /// The managed connector is not yet ready for future write-path follow-through.
    /// </summary>
    public const string NotReady = "not-ready";

    /// <summary>
    /// The managed connector currently has enough runtime truth to support future write-path follow-through.
    /// </summary>
    public const string Ready = "ready";

    /// <summary>
    /// The managed connector is currently blocked by runtime remediation before write-path readiness can be considered.
    /// </summary>
    public const string Blocked = "blocked";
}
