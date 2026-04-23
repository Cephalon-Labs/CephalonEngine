namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable action-plan state identifiers used by managed-connector execution-runtime answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates
{
    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector can stay in steady-state observe mode.
    /// </summary>
    public const string Observe = "observe";

    /// <summary>
    /// The managed connector is currently waiting for more runtime truth before deeper follow-through.
    /// </summary>
    public const string Waiting = "waiting";

    /// <summary>
    /// The managed connector currently requires operator action.
    /// </summary>
    public const string ActionRequired = "action-required";

    /// <summary>
    /// The managed connector is currently blocked by higher-priority remediation work.
    /// </summary>
    public const string Blocked = "blocked";
}
