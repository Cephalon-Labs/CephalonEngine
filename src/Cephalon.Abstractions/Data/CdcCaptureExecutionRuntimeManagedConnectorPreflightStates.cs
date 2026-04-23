namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector preflight state identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorPreflightStates
{
    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector currently stays in observe-only mode, so connector-management preflight is deferred.
    /// </summary>
    public const string Deferred = "deferred";

    /// <summary>
    /// The managed connector is not yet ready for Cephalon to preflight the currently intended management operation.
    /// </summary>
    public const string NotReady = "not-ready";

    /// <summary>
    /// The managed connector currently satisfies the shared baseline Cephalon would use to preflight the intended management operation.
    /// </summary>
    public const string Ready = "ready";

    /// <summary>
    /// The managed connector is currently blocked by runtime remediation before connector-management preflight can proceed.
    /// </summary>
    public const string Blocked = "blocked";
}
