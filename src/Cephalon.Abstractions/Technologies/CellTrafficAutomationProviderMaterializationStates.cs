namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable provider-materialization states for cell traffic automation runtime answers.
/// </summary>
public static class CellTrafficAutomationProviderMaterializationStates
{
    /// <summary>
    /// The automation targets a provider but no active materializer can apply it.
    /// </summary>
    public const string Unavailable = CellTrafficAutomationMaterializationStates.Unavailable;

    /// <summary>
    /// The automation targets a provider and an active materializer is expected to reconcile it.
    /// </summary>
    public const string Pending = CellTrafficAutomationMaterializationStates.Pending;

    /// <summary>
    /// The automation was reconciled successfully by the selected provider materializer.
    /// </summary>
    public const string Applied = CellTrafficAutomationMaterializationStates.Applied;

    /// <summary>
    /// The selected provider materializer last reported a failure while reconciling the automation.
    /// </summary>
    public const string Failed = CellTrafficAutomationMaterializationStates.Failed;
}
