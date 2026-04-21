namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable dimension identifiers used by typed cell traffic automation materialization conditions.
/// </summary>
public static class CellTrafficAutomationMaterializationConditionDimensions
{
    /// <summary>
    /// The provider-managed control-plane materialization dimension.
    /// </summary>
    public const string Provider = "provider";

    /// <summary>
    /// The edge-managed traffic-runtime materialization dimension.
    /// </summary>
    public const string Edge = "edge";
}
