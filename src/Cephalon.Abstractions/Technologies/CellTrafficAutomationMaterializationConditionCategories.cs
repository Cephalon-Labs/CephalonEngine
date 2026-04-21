namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable category identifiers used by typed cell traffic automation materialization conditions.
/// </summary>
public static class CellTrafficAutomationMaterializationConditionCategories
{
    /// <summary>
    /// A condition that describes whether the materialized control-plane or edge object is ready to serve traffic.
    /// </summary>
    public const string Readiness = "readiness";

    /// <summary>
    /// A condition that describes traffic dependency posture such as missing gateways, middleware, or backend services.
    /// </summary>
    public const string Dependency = "dependency";

    /// <summary>
    /// A condition that describes ownership posture for a materialized resource.
    /// </summary>
    public const string Ownership = "ownership";

    /// <summary>
    /// A condition that describes drift posture between authored Cephalon intent and the observed control-plane state.
    /// </summary>
    public const string Drift = "drift";

    /// <summary>
    /// A condition that describes the active lifecycle or reconciliation action for the materialized resource.
    /// </summary>
    public const string Lifecycle = "lifecycle";

    /// <summary>
    /// A condition that describes observation posture such as stale, unavailable, or error-driven reportability.
    /// </summary>
    public const string Observation = "observation";
}
