namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector command-envelope answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates
{
    /// <summary>
    /// The execution runtime does not currently produce a managed-connector command envelope.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector still has one or more blockers before Cephalon can trust a runnable command envelope.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// Cephalon can describe the command envelope, but the write-path remains operator-owned.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Cephalon can describe the command envelope, but the write-path still requires approval before future engine execution.
    /// </summary>
    public const string ApprovalGated = "approval-gated";

    /// <summary>
    /// Cephalon can describe the command envelope and the shared execution lane is ready for later engine execution work.
    /// </summary>
    public const string EngineReady = "engine-ready";
}
