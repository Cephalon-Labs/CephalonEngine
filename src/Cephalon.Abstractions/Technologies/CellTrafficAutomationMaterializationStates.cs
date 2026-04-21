namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable materialization states for cell traffic automation runtime answers.
/// </summary>
public static class CellTrafficAutomationMaterializationStates
{
    /// <summary>
    /// The automation targets a runtime, but no active materializer can apply it.
    /// </summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// The automation targets an active materializer that is expected to reconcile it.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The automation was reconciled successfully by the selected materializer.
    /// </summary>
    public const string Applied = "applied";

    /// <summary>
    /// The selected materializer last reported a failure while reconciling the automation.
    /// </summary>
    public const string Failed = "failed";
}
