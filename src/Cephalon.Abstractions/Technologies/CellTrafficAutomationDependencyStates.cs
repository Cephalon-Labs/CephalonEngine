namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Defines the stable dependency states for cell traffic automation materialization answers.
/// </summary>
public static class CellTrafficAutomationDependencyStates
{
    /// <summary>
    /// The selected materializer depends on control-plane resources that are currently available.
    /// </summary>
    public const string Satisfied = "satisfied";

    /// <summary>
    /// The selected materializer depends on control-plane resources that are currently missing.
    /// </summary>
    public const string Missing = "dependency-missing";

    /// <summary>
    /// The dependency posture could not be determined from the current runtime truth.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Multiple required materialization dimensions currently disagree about dependency posture.
    /// </summary>
    public const string Mixed = "mixed";
}
