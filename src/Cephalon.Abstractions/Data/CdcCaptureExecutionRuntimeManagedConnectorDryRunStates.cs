namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector dry-run state identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDryRunStates
{
    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector currently stays in observe-only mode, so dry-run write-path previews remain deferred.
    /// </summary>
    public const string Deferred = "deferred";

    /// <summary>
    /// The managed connector cannot currently progress to a dry-run answer because prerequisite runtime truth is incomplete or blocked.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// The managed connector currently reports no shared write-path changes for the intended management operation.
    /// </summary>
    public const string NoOp = "no-op";

    /// <summary>
    /// The managed connector currently reports one or more shared write-path changes for the intended management operation.
    /// </summary>
    public const string WouldChange = "would-change";
}
