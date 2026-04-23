namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector execution-intent state identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates
{
    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector currently stays in observe-only mode, so Cephalon does not intend to execute connector-management follow-through.
    /// </summary>
    public const string Deferred = "deferred";

    /// <summary>
    /// The managed connector cannot currently progress to a trustworthy execution intent because prerequisite runtime truth is incomplete or blocked.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// The managed connector currently requires operator-owned follow-through because the declared control plane is not yet engine-owned.
    /// </summary>
    public const string OperatorAction = "operator-action";

    /// <summary>
    /// The managed connector currently has a future engine-execution candidate, but the next step would still require an approval gate.
    /// </summary>
    public const string RequiresApproval = "requires-approval";

    /// <summary>
    /// The managed connector currently has a future engine-execution candidate that would not require additional shared write-path changes.
    /// </summary>
    public const string ReadyToExecute = "ready-to-execute";
}
