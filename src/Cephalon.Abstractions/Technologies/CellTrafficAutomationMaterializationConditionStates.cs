namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable state identifiers used by typed cell traffic automation materialization conditions.
/// </summary>
public static class CellTrafficAutomationMaterializationConditionStates
{
    /// <summary>
    /// The condition is satisfied.
    /// </summary>
    public const string Met = "met";

    /// <summary>
    /// The condition is not satisfied.
    /// </summary>
    public const string Unmet = "unmet";

    /// <summary>
    /// The condition is expected but not yet reconciled.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The runtime cannot currently determine whether the condition is satisfied.
    /// </summary>
    public const string Unknown = "unknown";
}
