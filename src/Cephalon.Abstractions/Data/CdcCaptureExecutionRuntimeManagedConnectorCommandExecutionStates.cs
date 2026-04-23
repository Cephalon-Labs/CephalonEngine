namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector command-execution results.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates
{
    /// <summary>
    /// The requested command does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The requested command remains blocked before Cephalon can safely route it.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// The requested command still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// No matching provider execution adapter is currently available for the requested command.
    /// </summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// The requested command does not require an outbound provider command.
    /// </summary>
    public const string NoOp = "no-op";

    /// <summary>
    /// The requested command was translated into a provider-facing command shape.
    /// </summary>
    public const string Adapted = "adapted";

    /// <summary>
    /// The requested command could not be translated successfully.
    /// </summary>
    public const string Failed = "failed";
}
