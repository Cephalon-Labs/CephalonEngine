namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable severity identifiers used by typed cell traffic automation materialization conditions.
/// </summary>
public static class CellTrafficAutomationMaterializationConditionSeverities
{
    /// <summary>
    /// The condition is informational and does not currently signal an operator-facing problem.
    /// </summary>
    public const string Info = "info";

    /// <summary>
    /// The condition signals a warning that still needs operator attention.
    /// </summary>
    public const string Warning = "warning";

    /// <summary>
    /// The condition signals an error that blocks or invalidates the current materialization answer.
    /// </summary>
    public const string Error = "error";
}
